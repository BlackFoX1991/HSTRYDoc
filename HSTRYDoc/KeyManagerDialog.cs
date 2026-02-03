// KeyManagerDialog.cs (V7 ECC: recipients + per-block access control)
// - Uses ECDH private key for opening/unwrapping + owner block operations
// - Uses ECDSA signing private key for header changes (add/remove recipients)
// - Key files:
//   - ECDH private: *.hstrypriv (encrypted)
//   - ECDH public:  *.hstrypub
//   - ECDSA signing private: *.hstrysigpriv (derived from *.hstrypriv, encrypted)
//   - ECDSA signing public:  *.hstrysigpub
//
// V7 addition:
// - RecipientEntry may carry optional recipient SigningPublicKeySpki (from *.hstrysigpub)
// - Drag&Drop can pair .hstrypub with matching .hstrysigpub (same basename, same folder)
// - User is asked once per drop whether to include sigpubs when available

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace HSTRYDoc
{
    public partial class KeyManagerDialog : Form
    {
        private string? _sessionKeyPassword;

        private readonly HSTRYContainer _container;
        private readonly string _containerPath;

        private ECDiffieHellman? _myEcdhPrivateKey;
        private ECDsa? _mySigningPrivateKey;

        private string? _myKeyIdHex;     // KeyId = SHA256(ECDH SPKI)
        private bool _isOwnerKeyLoaded;  // owner means: ECDH matches container owner ECDH

        public string? SelectedEcdhPrivateKeyPath { get; private set; }
        public bool ContainerChanged { get; private set; }

        public KeyManagerDialog(HSTRYContainer container, string containerPath, string? currentEcdhPrivateKeyPath)
        {
            InitializeComponent();

            _container = container ?? throw new ArgumentNullException(nameof(container));
            _containerPath = containerPath ?? string.Empty;

            SelectedEcdhPrivateKeyPath = currentEcdhPrivateKeyPath;

            // Wire UI
            btnBrowsePriv.Click += (_, __) => UiBrowsePrivateKey();
            btnCreateKeyPair.Click += (_, __) => UiCreateKeyPairForSharing();
            btnExportPublic.Click += (_, __) => UiExportPublicKey();
            btnTransferOwnership.Click += async (_, __) => await UiTransferOwnershipAsync();

            btnAddMyself.Click += (_, __) => UiAddMyselfAsRecipient();
            btnAddRecipient.Click += (_, __) => UiAddRecipient();
            btnRemoveRecipient.Click += (_, __) => UiRemoveSelectedRecipient();
            btnCopyKeyId.Click += (_, __) => UiCopySelectedKeyId();

            btnGrantRead.Click += (_, __) => UiGrantReadSelectedBlock();
            btnRevokeAccess.Click += (_, __) => UiRevokeSelectedBlocks();

            btnGrantReadAll.Click += async (_, __) => await UiGrantReadAllBlocksAsync();
            btnGrantWrite.Click += async (_, __) => await UiGrantWriteAllBlocksAsync();
            btnRevokeAll.Click += async (_, __) => await UiRevokeAllBlocksAsync();

            btnOk.Click += (_, __) => { DialogResult = DialogResult.OK; Close(); };
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            // Drag & drop (V7): .hstrypub + optional .hstrysigpub
            lvwRecipients.AllowDrop = true;
            lvwRecipients.DragEnter += LvwRecipients_DragEnter;
            lvwRecipients.DragDrop += LvwRecipients_DragDrop;

            lvwRecipients.SelectedIndexChanged += (_, __) => RefreshBlocksList();
            lvwBlocks.SelectedIndexChanged += (_, __) => UpdateMyRecipientStatusAndPermissions();
            lvwBlocks.ItemSelectionChanged += (_, __) => UpdateMyRecipientStatusAndPermissions();

            // Do NOT auto-prompt password on dialog open; load lazily on action.
            UpdateMyKeyUi(SelectedEcdhPrivateKeyPath, null);
            RefreshRecipientsList();
            RefreshBlocksList();
            UpdateMyRecipientStatusAndPermissions();
        }

        private static string DeriveSigningPrivateKeyPath(string ecdhPrivPath)
            => Path.ChangeExtension(ecdhPrivPath, ".hstrysigpriv");

        private static string DeriveSigningPublicKeyPathFromEcdhPublic(string ecdhPubPath)
            => Path.ChangeExtension(ecdhPubPath, ".hstrysigpub");

        private void SetUiBusy(bool busy)
        {
            grpMyKeys.Enabled = !busy;
            grpRecipients.Enabled = !busy;
            grpBlockAccess.Enabled = !busy;

            btnOk.Enabled = !busy;
            btnCancel.Enabled = !busy;
        }

        private void ClearLoadedKeys()
        {
            _myEcdhPrivateKey?.Dispose();
            _myEcdhPrivateKey = null;

            _mySigningPrivateKey?.Dispose();
            _mySigningPrivateKey = null;

            _myKeyIdHex = null;
            _isOwnerKeyLoaded = false;

            UpdateMyKeyUi(SelectedEcdhPrivateKeyPath, null);
        }

        private bool EnsureKeysLoadedForAction(bool requireOwner, bool requireSigningKey, bool showErrors)
        {
            if (_myEcdhPrivateKey != null)
            {
                if (requireOwner && !_isOwnerKeyLoaded)
                {
                    if (showErrors)
                        MessageBox.Show(this, "Load the OWNER ECDH private key for this operation.", "Key required",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (requireSigningKey && _isOwnerKeyLoaded && _mySigningPrivateKey == null)
                {
                    TryLoadSigningKeyForEcdhPath(SelectedEcdhPrivateKeyPath ?? "", showErrors: showErrors);
                    if (_mySigningPrivateKey == null)
                        return false;
                }

                return true;
            }

            if (string.IsNullOrWhiteSpace(SelectedEcdhPrivateKeyPath) || !File.Exists(SelectedEcdhPrivateKeyPath))
            {
                if (showErrors)
                    MessageBox.Show(this, "No private key file selected.", "Key required",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (!EnsureSessionPasswordPrompt(out string pw))
                return false;

            try
            {
                _myEcdhPrivateKey = LoadEcdhPrivateKeyWithPassword(SelectedEcdhPrivateKeyPath!, pw);

                byte[] keyId = HSTRYContainer.EcdhKeyFiles.ComputeKeyIdFromPublicKey(_myEcdhPrivateKey);
                _myKeyIdHex = Convert.ToHexString(keyId);

                _isOwnerKeyLoaded = IsOwnerEcdhPrivateKey(_myEcdhPrivateKey);

                if (_isOwnerKeyLoaded && requireSigningKey)
                    TryLoadSigningKeyForEcdhPath(SelectedEcdhPrivateKeyPath!, showErrors: showErrors);

                UpdateMyKeyUi(SelectedEcdhPrivateKeyPath, _myKeyIdHex);

                if (requireOwner && !_isOwnerKeyLoaded)
                {
                    if (showErrors)
                        MessageBox.Show(this, "Load the OWNER ECDH private key for this operation.", "Key required",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (requireSigningKey && _isOwnerKeyLoaded && _mySigningPrivateKey == null)
                    return false;

                return true;
            }
            catch (CryptographicException)
            {
                _sessionKeyPassword = null;
                ClearLoadedKeys();

                if (showErrors)
                    MessageBox.Show(this, "Wrong password or corrupted key file.", "Load key",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }
        }

        // ============================================================
        // Password helpers (dialog-session cached)
        // ============================================================
        private bool EnsureSessionPasswordPrompt(out string password)
        {
            password = _sessionKeyPassword ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(_sessionKeyPassword))
                return true;

            string? pw = PasswordDialog.ShowPassword(
                this,
                "Unlock key",
                "Enter password:",
                PasswordDialog.PasswordDialogMode.Prompt);

            if (pw == null) return false;
            if (string.IsNullOrWhiteSpace(pw)) return false;

            _sessionKeyPassword = pw;
            password = pw;
            return true;
        }

        private bool EnsureNewPasswordSet(out string password)
        {
            password = string.Empty;

            string? pw = PasswordDialog.ShowPassword(
                this,
                "Set password",
                "Set a password for this private key set:",
                PasswordDialog.PasswordDialogMode.SetNew);

            if (pw == null) return false;
            if (string.IsNullOrWhiteSpace(pw)) return false;

            // IMPORTANT: Do NOT overwrite _sessionKeyPassword here!
            password = pw;
            return true;
        }

        private ECDiffieHellman LoadEcdhPrivateKeyWithPassword(string ecdhPrivPath, string password)
        {
            return HSTRYContainer.EcdhKeyFiles.LoadPrivateKeyPkcs8Encrypted(ecdhPrivPath, password);
        }

        private ECDsa LoadEcdsaSigningKeyWithPassword(string ecdhPrivPath, string password)
        {
            string signPath = DeriveSigningPrivateKeyPath(ecdhPrivPath);
            if (!File.Exists(signPath))
                throw new FileNotFoundException("Owner signing key is missing.", signPath);

            return HSTRYContainer.EcdsaKeyFiles.LoadPrivateKeyPkcs8Encrypted(signPath, password);
        }

        // ============================================================
        // Drag & drop pairing (.hstrypub + optional .hstrysigpub)
        // ============================================================
        private void LvwRecipients_DragEnter(object? sender, DragEventArgs e)
        {
            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void LvwRecipients_DragDrop(object? sender, DragEventArgs e)
        {
            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this,
                    "This container is not V7.",
                    "Recipients",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!EnsureKeysLoadedForAction(requireOwner: true, requireSigningKey: true, showErrors: true))
                return;

            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
                return;

            // Collect pub candidates
            var pubPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sigPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var f in files)
            {
                if (!File.Exists(f)) continue;

                string ext = Path.GetExtension(f);
                if (ext.Equals(".hstrypub", StringComparison.OrdinalIgnoreCase))
                    pubPaths.Add(f);
                else if (ext.Equals(".hstrysigpub", StringComparison.OrdinalIgnoreCase))
                    sigPaths.Add(f);
            }

            // If only sigpub was dropped, try infer .hstrypub (same basename)
            foreach (var sig in sigPaths.ToArray())
            {
                string inferredPub = Path.ChangeExtension(sig, ".hstrypub");
                if (File.Exists(inferredPub))
                    pubPaths.Add(inferredPub);
            }

            if (pubPaths.Count == 0)
                return;

            // Determine which pubs have a matching sigpub available (either dropped or exists beside)
            var pairs = new List<(string pub, string? sig)>();
            foreach (var pub in pubPaths)
            {
                string expectedSig = DeriveSigningPublicKeyPathFromEcdhPublic(pub);
                string? sig = null;

                if (sigPaths.Contains(expectedSig) || File.Exists(expectedSig))
                    sig = expectedSig;

                pairs.Add((pub, sig));
            }

            bool anySigAvailable = pairs.Any(x => !string.IsNullOrWhiteSpace(x.sig) && File.Exists(x.sig!));

            bool includeSig = false;
            if (anySigAvailable)
            {
                var ask = MessageBox.Show(
                    this,
                    "Signing public keys (*.hstrysigpub) were found for one or more recipients.\n\nInclude them when adding recipients?",
                    "Recipients",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);

                includeSig = (ask == DialogResult.Yes);
            }

            int added = 0;

            foreach (var (pubPath, sigPath) in pairs)
            {
                try
                {
                    using var pub = HSTRYContainer.EcdhKeyFiles.LoadPublicKeySpki(pubPath);

                    byte[]? sigSpki = null;
                    if (includeSig && !string.IsNullOrWhiteSpace(sigPath) && File.Exists(sigPath!))
                    {
                        using var sigPub = HSTRYContainer.EcdsaKeyFiles.LoadPublicKeySpki(sigPath!);
                        sigSpki = sigPub.ExportSubjectPublicKeyInfo();
                    }

                    _container.AddRecipient(_mySigningPrivateKey!, pub, sigSpki);
                    added++;
                    ContainerChanged = true;
                }
                catch
                {
                    // ignore per-file failures
                }
            }

            if (added > 0)
            {
                RefreshRecipientsList();
                RefreshBlocksList();
                UpdateMyRecipientStatusAndPermissions();

                MessageBox.Show(this,
                    "Recipients added.\n\nNote: New recipients have NO block access by default.",
                    "Recipients",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        // ============================================================
        // Key file selection (lazy load)
        // ============================================================
        private void UiBrowsePrivateKey()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "HSTRY ECDH Private Key (*.hstrypriv)|*.hstrypriv|All files (*.*)|*.*",
                CheckFileExists = true,
                Title = "Select ECDH private key"
            };

            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            if (!string.Equals(SelectedEcdhPrivateKeyPath ?? "", ofd.FileName, StringComparison.OrdinalIgnoreCase))
                _sessionKeyPassword = null;

            SelectedEcdhPrivateKeyPath = ofd.FileName;

            // Lazy: do not load immediately
            ClearLoadedKeys();
            RefreshBlocksList();
            UpdateMyRecipientStatusAndPermissions();
        }

        private void TryLoadSigningKeyForEcdhPath(string ecdhPrivPath, bool showErrors)
        {
            try
            {
                _mySigningPrivateKey?.Dispose();
                _mySigningPrivateKey = null;

                if (!EnsureSessionPasswordPrompt(out string pw))
                    return;

                _mySigningPrivateKey = LoadEcdsaSigningKeyWithPassword(ecdhPrivPath, pw);
            }
            catch (CryptographicException)
            {
                _sessionKeyPassword = null;
                _mySigningPrivateKey?.Dispose();
                _mySigningPrivateKey = null;

                if (showErrors)
                    MessageBox.Show(this, "Wrong password or corrupted signing key file.", "Load signing key",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                _mySigningPrivateKey?.Dispose();
                _mySigningPrivateKey = null;

                if (showErrors)
                    MessageBox.Show(this, ex.Message, "Load signing key", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsOwnerEcdhPrivateKey(ECDiffieHellman priv)
        {
            try
            {
                byte[] spki = priv.ExportSubjectPublicKeyInfo();
                return spki.SequenceEqual(_container.OwnerEcdhPublicKeySpki);
            }
            catch
            {
                return false;
            }
        }

        private void UpdateMyKeyUi(string? path, string? keyIdHex)
        {
            txtPrivateKeyPath.Text = path ?? string.Empty;
            txtMyKeyId.Text = keyIdHex ?? string.Empty;

            btnExportPublic.Enabled = _myEcdhPrivateKey != null;
            btnTransferOwnership.Enabled = _myEcdhPrivateKey != null;
        }

        // ============================================================
        // Create new key set (recipient)
        // ============================================================
        private void UiCreateKeyPairForSharing()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "HSTRY ECDH Private Key (*.hstrypriv)|*.hstrypriv|All files (*.*)|*.*",
                DefaultExt = "hstrypriv",
                AddExtension = true,
                FileName = "recipient.hstrypriv",
                Title = "Save new ECDH private key"
            };

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;

            string ecdhPrivPath = sfd.FileName;
            string ecdhPubPath = Path.ChangeExtension(ecdhPrivPath, ".hstrypub");
            string signPrivPath = DeriveSigningPrivateKeyPath(ecdhPrivPath);
            string signPubPath = Path.ChangeExtension(signPrivPath, ".hstrysigpub");

            bool overwrite = File.Exists(ecdhPrivPath) || File.Exists(ecdhPubPath) || File.Exists(signPrivPath) || File.Exists(signPubPath);
            if (overwrite)
            {
                var res = MessageBox.Show(
                    this,
                    "Key files already exist. Overwrite them?",
                    "Create key set",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (res != DialogResult.Yes)
                    return;
            }

            if (!EnsureNewPasswordSet(out string pwNew))
                return;

            try
            {
                using var ecdh = HSTRYContainer.EcdhKeyFiles.CreateNewKeyPair();
                using var ecdsa = HSTRYContainer.EcdsaKeyFiles.CreateNewKeyPair();

                // Private: encrypted
                HSTRYContainer.EcdhKeyFiles.SavePrivateKeyPkcs8Encrypted(ecdhPrivPath, ecdh, pwNew);
                HSTRYContainer.EcdsaKeyFiles.SavePrivateKeyPkcs8Encrypted(signPrivPath, ecdsa, pwNew);

                // Public: plaintext
                HSTRYContainer.EcdhKeyFiles.SavePublicKeySpki(ecdhPubPath, ecdh);
                HSTRYContainer.EcdsaKeyFiles.SavePublicKeySpki(signPubPath, ecdsa);

                MessageBox.Show(this,
                    "Key set created successfully.\n\nShare the .hstrypub (and optionally .hstrysigpub) with other users.",
                    "Create key set",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Create key set", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UiExportPublicKey()
        {
            if (!EnsureKeysLoadedForAction(requireOwner: false, requireSigningKey: false, showErrors: true))
                return;

            string defaultPub = !string.IsNullOrWhiteSpace(SelectedEcdhPrivateKeyPath)
                ? Path.ChangeExtension(SelectedEcdhPrivateKeyPath, ".hstrypub")
                : Path.ChangeExtension(_containerPath, ".hstrypub");

            using var sfd = new SaveFileDialog
            {
                Filter = "HSTRY Public Key (*.hstrypub)|*.hstrypub|All files (*.*)|*.*",
                DefaultExt = "hstrypub",
                AddExtension = true,
                FileName = Path.GetFileName(defaultPub),
                Title = "Export public key"
            };

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                HSTRYContainer.EcdhKeyFiles.SavePublicKeySpki(sfd.FileName, _myEcdhPrivateKey!);
                MessageBox.Show(this, "Public key exported successfully.", "Export public key",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Export public key", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============================================================
        // Transfer ownership (async, needs owner ECDH + owner ECDSA)
        // ============================================================
        private async Task UiTransferOwnershipAsync()
        {
            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this, "This container is not V7.", "Transfer ownership",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!EnsureKeysLoadedForAction(requireOwner: true, requireSigningKey: true, showErrors: true))
                return;

            var confirm = MessageBox.Show(
                this,
                "This will transfer container ownership to a NEW key set (ECDH + ECDSA).\n\nContinue?",
                "Transfer ownership",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes)
                return;

            using var sfd = new SaveFileDialog
            {
                Filter = "HSTRY ECDH Private Key (*.hstrypriv)|*.hstrypriv|All files (*.*)|*.*",
                DefaultExt = "hstrypriv",
                AddExtension = true,
                FileName = "new_owner.hstrypriv",
                Title = "Save new owner ECDH private key"
            };

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;

            string newEcdhPrivPath = sfd.FileName;
            string newEcdhPubPath = Path.ChangeExtension(newEcdhPrivPath, ".hstrypub");
            string newSignPrivPath = DeriveSigningPrivateKeyPath(newEcdhPrivPath);
            string newSignPubPath = Path.ChangeExtension(newSignPrivPath, ".hstrysigpub");

            bool overwrite = File.Exists(newEcdhPrivPath) || File.Exists(newEcdhPubPath) ||
                             File.Exists(newSignPrivPath) || File.Exists(newSignPubPath);

            if (overwrite)
            {
                var res = MessageBox.Show(
                    this,
                    "Owner key files already exist. Overwrite them?",
                    "Transfer ownership",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (res != DialogResult.Yes)
                    return;
            }

            if (!EnsureNewPasswordSet(out string pwNewOwner))
                return;

            SetUiBusy(true);
            try
            {
                await reporterDiag.RunAsync(
                    owner: this,
                    title: "Transfer ownership",
                    work: async (progress, token) =>
                    {
                        progress.Report(new UiProgress { Message = "Transferring ownership…", Indeterminate = true });

                        await Task.Run(() =>
                        {
                            using var newEcdh = HSTRYContainer.EcdhKeyFiles.CreateNewKeyPair();
                            using var newSig = HSTRYContainer.EcdsaKeyFiles.CreateNewKeyPair();

                            // Private: encrypted
                            HSTRYContainer.EcdhKeyFiles.SavePrivateKeyPkcs8Encrypted(newEcdhPrivPath, newEcdh, pwNewOwner);
                            HSTRYContainer.EcdsaKeyFiles.SavePrivateKeyPkcs8Encrypted(newSignPrivPath, newSig, pwNewOwner);

                            // Public: plaintext
                            HSTRYContainer.EcdhKeyFiles.SavePublicKeySpki(newEcdhPubPath, newEcdh);
                            HSTRYContainer.EcdsaKeyFiles.SavePublicKeySpki(newSignPubPath, newSig);

                            _container.TransferOwnership(
                                currentOwnerSigningPrivateKey: _mySigningPrivateKey!,
                                currentOwnerEcdhPrivateKey: _myEcdhPrivateKey!,
                                newOwnerSigningPrivateKey: newSig,
                                newOwnerEcdhPrivateKey: newEcdh,
                                progress: progress,
                                token: token);

                            ContainerChanged = true;
                        }, token);
                    });

                SelectedEcdhPrivateKeyPath = newEcdhPrivPath;
                _sessionKeyPassword = pwNewOwner;
                ClearLoadedKeys();

                RefreshRecipientsList();
                RefreshBlocksList();
                UpdateMyRecipientStatusAndPermissions();

                MessageBox.Show(this,
                    "Ownership transferred successfully.\n\nThe new owner key set has been saved to disk.",
                    "Transfer ownership",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(this, "Operation cancelled.", "Transfer ownership",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Transfer ownership", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUiBusy(false);
                UpdateMyRecipientStatusAndPermissions();
            }
        }

        // ============================================================
        // Recipient operations (owner-only; requires signing key)
        // ============================================================
        private void UiAddMyselfAsRecipient()
        {
            if (!EnsureKeysLoadedForAction(requireOwner: true, requireSigningKey: true, showErrors: true))
                return;

            if (string.IsNullOrWhiteSpace(_myKeyIdHex))
                return;

            bool included = _container.Recipients.Any(r =>
                Convert.ToHexString(r.KeyId).Equals(_myKeyIdHex!, StringComparison.OrdinalIgnoreCase));

            if (included)
            {
                UpdateMyRecipientStatusAndPermissions();
                return;
            }

            try
            {
                using var pub = CreatePublicOnlyFromPrivate(_myEcdhPrivateKey!);
                _container.AddRecipient(_mySigningPrivateKey!, pub, recipientSigningPublicKeySpki: null);

                ContainerChanged = true;
                RefreshRecipientsList();
                RefreshBlocksList();
                UpdateMyRecipientStatusAndPermissions();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Add myself", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static ECDiffieHellman CreatePublicOnlyFromPrivate(ECDiffieHellman priv)
        {
            byte[] spki = priv.ExportSubjectPublicKeyInfo();
            var e = ECDiffieHellman.Create();
            e.ImportSubjectPublicKeyInfo(spki, out _);
            return e;
        }

        private void UiAddRecipient()
        {
            if (!EnsureKeysLoadedForAction(requireOwner: true, requireSigningKey: true, showErrors: true))
                return;

            using var ofd = new OpenFileDialog
            {
                Filter = "HSTRY Public Key (*.hstrypub)|*.hstrypub|All files (*.*)|*.*",
                CheckFileExists = true,
                Title = "Select recipient public key"
            };

            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            string pubPath = ofd.FileName;
            string sigPath = DeriveSigningPublicKeyPathFromEcdhPublic(pubPath);

            byte[]? sigSpki = null;

            if (File.Exists(sigPath))
            {
                var ask = MessageBox.Show(
                    this,
                    "A matching signing public key (*.hstrysigpub) was found.\n\nInclude it for this recipient?",
                    "Add recipient",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);

                if (ask == DialogResult.Yes)
                {
                    try
                    {
                        using var sigPub = HSTRYContainer.EcdsaKeyFiles.LoadPublicKeySpki(sigPath);
                        sigSpki = sigPub.ExportSubjectPublicKeyInfo();
                    }
                    catch
                    {
                        sigSpki = null;
                    }
                }
            }

            try
            {
                using var pub = HSTRYContainer.EcdhKeyFiles.LoadPublicKeySpki(pubPath);
                _container.AddRecipient(_mySigningPrivateKey!, pub, sigSpki);

                ContainerChanged = true;
                RefreshRecipientsList();
                RefreshBlocksList();
                UpdateMyRecipientStatusAndPermissions();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Add recipient", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UiRemoveSelectedRecipient()
        {
            if (lvwRecipients.SelectedItems.Count == 0)
                return;

            if (!EnsureKeysLoadedForAction(requireOwner: true, requireSigningKey: true, showErrors: true))
                return;

            string keyIdHex = lvwRecipients.SelectedItems[0].Text;

            var res = MessageBox.Show(
                this,
                "Remove the selected recipient from this container?\n\nThis does NOT automatically revoke existing block access.\nUse 'Revoke all' to remove all block rights for this recipient.",
                "Remove recipient",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (res != DialogResult.Yes)
                return;

            try
            {
                if (_container.RemoveRecipientByKeyIdHex(_mySigningPrivateKey!, keyIdHex))
                {
                    ContainerChanged = true;
                    RefreshRecipientsList();
                    RefreshBlocksList();
                    UpdateMyRecipientStatusAndPermissions();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Remove recipient", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UiCopySelectedKeyId()
        {
            if (lvwRecipients.SelectedItems.Count == 0)
                return;

            string keyIdHex = lvwRecipients.SelectedItems[0].Text;
            try { Clipboard.SetText(keyIdHex); } catch { }
        }

        private void RefreshRecipientsList()
        {
            lvwRecipients.BeginUpdate();
            try
            {
                lvwRecipients.Items.Clear();

                foreach (var r in _container.Recipients)
                {
                    string keyIdHex = Convert.ToHexString(r.KeyId);
                    string alg = r.Alg == 1 ? "ECDH-HKDF-SHA256-AESGCM" : $"Alg-{r.Alg}";
                    string wrappedLen = (r.WrappedDek?.Length ?? 0).ToString(CultureInfo.InvariantCulture);
                    string ecdhLen = (r.PublicKeySpki?.Length ?? 0).ToString(CultureInfo.InvariantCulture);
                    string sigLen = (r.SigningPublicKeySpki?.Length ?? 0).ToString(CultureInfo.InvariantCulture);

                    var it = new ListViewItem(keyIdHex);
                    it.SubItems.Add(alg);
                    it.SubItems.Add(wrappedLen);
                    it.SubItems.Add(ecdhLen);
                    it.SubItems.Add(sigLen);

                    it.Tag = keyIdHex;
                    lvwRecipients.Items.Add(it);
                }
            }
            finally
            {
                lvwRecipients.EndUpdate();
            }
        }

        private void UpdateMyRecipientStatusAndPermissions()
        {
            bool hasEcdh = _myEcdhPrivateKey != null && !string.IsNullOrWhiteSpace(_myKeyIdHex);
            bool included = hasEcdh && _container.Recipients.Any(r =>
                Convert.ToHexString(r.KeyId).Equals(_myKeyIdHex!, StringComparison.OrdinalIgnoreCase));

            bool isV7 = _container.Version == HSTRYContainer.CurrentVersion;
            bool canModify = hasEcdh && _isOwnerKeyLoaded && isV7 && _mySigningPrivateKey != null;

            btnAddRecipient.Enabled = canModify;
            btnRemoveRecipient.Enabled = canModify;
            btnAddMyself.Enabled = canModify && !included;
            btnTransferOwnership.Enabled = canModify;

            bool hasRecipientSelection = lvwRecipients.SelectedItems.Count > 0;
            bool hasBlockSelection = lvwBlocks.SelectedItems.Count > 0;
            bool hasAnyBlocks = _container.Blocks.Count > 0;

            grpBlockAccess.Enabled = isV7;

            btnGrantRead.Enabled = canModify && hasRecipientSelection && hasBlockSelection;
            btnRevokeAccess.Enabled = canModify && hasRecipientSelection && hasBlockSelection;

            btnGrantWrite.Enabled = canModify && hasRecipientSelection && hasAnyBlocks;
            btnGrantReadAll.Enabled = canModify && hasRecipientSelection && hasAnyBlocks;
            btnRevokeAll.Enabled = canModify && hasRecipientSelection && hasAnyBlocks;

            if (!hasEcdh)
            {
                lblMyRecipientStatus.Text = "No private key loaded";
                lblMyRecipientStatus.ForeColor = SystemColors.ControlText;
                return;
            }

            if (!isV7)
            {
                lblMyRecipientStatus.Text = "Key loaded. This container is not V7.";
                lblMyRecipientStatus.ForeColor = Color.DarkRed;
                return;
            }

            if (_isOwnerKeyLoaded)
            {
                if (_mySigningPrivateKey == null)
                {
                    lblMyRecipientStatus.Text = "Owner ECDH key loaded, but owner signing key is missing (*.hstrysigpriv).";
                    lblMyRecipientStatus.ForeColor = Color.DarkRed;
                    return;
                }

                lblMyRecipientStatus.Text = included
                    ? "Owner key loaded. Your key is included as a recipient."
                    : "Owner key loaded, but your key is NOT a recipient. You can add yourself.";

                lblMyRecipientStatus.ForeColor = included ? Color.DarkGreen : Color.DarkRed;
            }
            else
            {
                lblMyRecipientStatus.Text = included
                    ? "Recipient key loaded (read-only). You are not the owner."
                    : "Key loaded, but it is not a recipient and not the owner.";

                lblMyRecipientStatus.ForeColor = included ? Color.DarkGoldenrod : Color.DarkRed;
            }
        }

        // ============================================================
        // Block list UI
        // ============================================================
        private RecipientEntry? GetSelectedRecipient()
        {
            if (lvwRecipients.SelectedItems.Count == 0)
                return null;

            string keyIdHex = lvwRecipients.SelectedItems[0].Text;
            byte[] keyId;
            try { keyId = Convert.FromHexString(keyIdHex); }
            catch { return null; }

            return _container.Recipients.FirstOrDefault(r => r.KeyId.SequenceEqual(keyId));
        }

        private List<int> GetSelectedBlockIndices()
        {
            return lvwBlocks.SelectedItems
                .Cast<ListViewItem>()
                .Select(it => int.TryParse(it.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx) ? idx : -1)
                .Where(idx => idx >= 0)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();
        }

        private void RefreshBlocksList()
        {
            lvwBlocks.BeginUpdate();
            try
            {
                lvwBlocks.Items.Clear();

                if (_container.Version != HSTRYContainer.CurrentVersion)
                {
                    lblBlockAccessHint.Text = "Block access control requires V7.";
                    return;
                }

                var rec = GetSelectedRecipient();
                if (rec == null)
                {
                    lblBlockAccessHint.Text = "Select a recipient above to view and edit block rights.";
                    return;
                }

                lblBlockAccessHint.Text = $"Selected recipient: {Convert.ToHexString(rec.KeyId)}";

                for (int i = 0; i < _container.Blocks.Count; i++)
                {
                    var b = _container.Blocks[i];
                    string title = string.IsNullOrWhiteSpace(b.Title) ? "(no title)" : b.Title;

                    var slot = b.KeySlots.FirstOrDefault(s => s.KeyId.SequenceEqual(rec.KeyId));
                    string rightsText = slot == null ? "None" : RightsToText(slot.Rights);

                    var it = new ListViewItem(i.ToString(CultureInfo.InvariantCulture));
                    it.SubItems.Add(title);
                    it.SubItems.Add(rightsText);
                    lvwBlocks.Items.Add(it);
                }
            }
            finally
            {
                lvwBlocks.EndUpdate();
            }

            UpdateMyRecipientStatusAndPermissions();
        }

        private static string RightsToText(BlockRights r)
        {
            if ((r & BlockRights.Read) == 0 && (r & BlockRights.Write) == 0) return "None";
            if ((r & BlockRights.Read) != 0 && (r & BlockRights.Write) == 0) return "Read";
            if ((r & BlockRights.Read) != 0 && (r & BlockRights.Write) != 0) return "Read+Write";
            if ((r & BlockRights.Write) != 0) return "Write";
            return "None";
        }

        // ============================================================
        // Block operations
        // ============================================================
        private void UiGrantReadSelectedBlock()
        {
            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this, "Block access control requires V7.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!EnsureKeysLoadedForAction(requireOwner: true, requireSigningKey: false, showErrors: true))
                return;

            var rec = GetSelectedRecipient();
            if (rec == null) return;

            if (rec.PublicKeySpki == null || rec.PublicKeySpki.Length == 0)
            {
                MessageBox.Show(this, "Recipient public key is missing in the container (SPKI).", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (lvwBlocks.SelectedItems.Count == 0) return;
            if (!int.TryParse(lvwBlocks.SelectedItems[0].Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx))
                return;

            if ((uint)idx >= (uint)_container.Blocks.Count)
                return;

            try
            {
                using var recPub = ECDiffieHellman.Create();
                recPub.ImportSubjectPublicKeyInfo(rec.PublicKeySpki, out _);

                _container.GrantBlockAccess(_myEcdhPrivateKey!, idx, recPub, BlockRights.Read, replaceExisting: true);

                ContainerChanged = true;
                RefreshBlocksList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Block access", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UiRevokeSelectedBlocks()
        {
            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this, "Block access control requires V7.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!EnsureKeysLoadedForAction(requireOwner: true, requireSigningKey: false, showErrors: true))
                return;

            var rec = GetSelectedRecipient();
            if (rec == null) return;

            var indices = GetSelectedBlockIndices();
            if (indices.Count == 0) return;

            string keyIdHex = Convert.ToHexString(rec.KeyId);

            try
            {
                foreach (int idx in indices)
                {
                    if (!_container.Blocks[idx].KeySlots.Any(s => s.KeyId.SequenceEqual(rec.KeyId)))
                        continue;

                    _container.RevokeBlockAccess(_myEcdhPrivateKey!, idx, keyIdHex);
                }

                ContainerChanged = true;
                RefreshBlocksList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Block access", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UiGrantReadAllBlocksAsync()
        {
            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this, "Block access control requires V7.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!EnsureKeysLoadedForAction(requireOwner: true, requireSigningKey: false, showErrors: true))
                return;

            if (_container.Blocks.Count == 0)
                return;

            var rec = GetSelectedRecipient();
            if (rec == null) return;

            if (rec.PublicKeySpki == null || rec.PublicKeySpki.Length == 0)
            {
                MessageBox.Show(this, "Recipient public key is missing in the container (SPKI).", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var confirm = MessageBox.Show(
                this,
                "Grant READ access on ALL blocks for the selected recipient?\n\nThis may take some time for large containers.",
                "Grant read (all)",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes)
                return;

            SetUiBusy(true);
            try
            {
                byte[] recSpki = rec.PublicKeySpki.ToArray();

                await reporterDiag.RunAsync(
                    owner: this,
                    title: "Grant read (all)",
                    work: async (progress, token) =>
                    {
                        using var recPub = ECDiffieHellman.Create();
                        recPub.ImportSubjectPublicKeyInfo(recSpki, out _);

                        await Task.Run(() =>
                        {
                            _container.GrantReadAllBlocks(_myEcdhPrivateKey!, recPub, progress, token);
                        }, token);
                    });

                ContainerChanged = true;
                RefreshBlocksList();
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(this, "Operation cancelled.", "Grant read (all)",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Grant read (all)",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUiBusy(false);
                UpdateMyRecipientStatusAndPermissions();
            }
        }

        private async Task UiGrantWriteAllBlocksAsync()
        {
            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this, "Block access control requires V7.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!EnsureKeysLoadedForAction(requireOwner: true, requireSigningKey: false, showErrors: true))
                return;

            if (_container.Blocks.Count == 0)
                return;

            var rec = GetSelectedRecipient();
            if (rec == null) return;

            if (rec.PublicKeySpki == null || rec.PublicKeySpki.Length == 0)
            {
                MessageBox.Show(this, "Recipient public key is missing in the container (SPKI).", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var confirm = MessageBox.Show(
                this,
                "Grant READ+WRITE access on ALL blocks for the selected recipient?\n\nThis may take some time for large containers.\n\nContinue?",
                "Grant write (all)",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes)
                return;

            SetUiBusy(true);
            try
            {
                byte[] recSpki = rec.PublicKeySpki.ToArray();

                await reporterDiag.RunAsync(
                    owner: this,
                    title: "Grant write (all)",
                    work: async (progress, token) =>
                    {
                        using var recPub = ECDiffieHellman.Create();
                        recPub.ImportSubjectPublicKeyInfo(recSpki, out _);

                        await Task.Run(() =>
                        {
                            _container.GrantWriteAllBlocks(_myEcdhPrivateKey!, recPub, progress, token);
                        }, token);
                    });

                ContainerChanged = true;
                RefreshBlocksList();
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(this, "Operation cancelled.", "Grant write (all)",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Grant write (all)",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUiBusy(false);
                UpdateMyRecipientStatusAndPermissions();
            }
        }

        private async Task UiRevokeAllBlocksAsync()
        {
            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this, "Block access control requires V7.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!EnsureKeysLoadedForAction(requireOwner: true, requireSigningKey: false, showErrors: true))
                return;

            if (_container.Blocks.Count == 0)
                return;

            var rec = GetSelectedRecipient();
            if (rec == null) return;

            var confirm = MessageBox.Show(
                this,
                "Revoke ALL block access for the selected recipient?\n\nThis may take some time for large containers.",
                "Revoke all",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes)
                return;

            SetUiBusy(true);
            try
            {
                byte[] keyIdCopy = rec.KeyId.ToArray();

                await reporterDiag.RunAsync(
                    owner: this,
                    title: "Revoke all",
                    work: async (progress, token) =>
                    {
                        await Task.Run(() =>
                        {
                            _container.RevokeAllBlocks(_myEcdhPrivateKey!, keyIdCopy, progress, token);
                        }, token);
                    });

                ContainerChanged = true;
                RefreshBlocksList();
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(this, "Operation cancelled.", "Revoke all",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Revoke all",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUiBusy(false);
                UpdateMyRecipientStatusAndPermissions();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            _myEcdhPrivateKey?.Dispose();
            _myEcdhPrivateKey = null;

            _mySigningPrivateKey?.Dispose();
            _mySigningPrivateKey = null;

            _sessionKeyPassword = null;
        }
    }
}
