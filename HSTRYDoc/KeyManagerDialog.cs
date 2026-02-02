// KeyManagerDialog.cs (V6 ECC: recipients + per-block access control)
// - Uses ECDH private key for opening/unwrapping + owner block operations
// - Uses ECDSA signing private key for header changes (add/remove recipients)
// - Key files:
//   - ECDH private: *.hstrypriv
//   - ECDH public:  *.hstrypub
//   - ECDSA signing private: *.hstrysigpriv (derived from *.hstrypriv)
//   - ECDSA signing public:  *.hstrysigpub

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

            lvwRecipients.AllowDrop = true;
            lvwRecipients.DragEnter += LvwRecipients_DragEnter;
            lvwRecipients.DragDrop += LvwRecipients_DragDrop;

            lvwRecipients.SelectedIndexChanged += (_, __) => RefreshBlocksList();

            lvwBlocks.SelectedIndexChanged += (_, __) => UpdateMyRecipientStatusAndPermissions();
            lvwBlocks.ItemSelectionChanged += (_, __) => UpdateMyRecipientStatusAndPermissions();

            RefreshRecipientsList();

            if (!string.IsNullOrWhiteSpace(SelectedEcdhPrivateKeyPath) && File.Exists(SelectedEcdhPrivateKeyPath))
                TryLoadMyEcdhPrivateKey(SelectedEcdhPrivateKeyPath!, showErrors: false);
            else
                UpdateMyKeyUi(null, null);

            UpdateMyRecipientStatusAndPermissions();
            RefreshBlocksList();
        }

        private static string DeriveSigningPrivateKeyPath(string ecdhPrivPath)
            => Path.ChangeExtension(ecdhPrivPath, ".hstrysigpriv");

        private void SetUiBusy(bool busy)
        {
            grpMyKeys.Enabled = !busy;
            grpRecipients.Enabled = !busy;
            grpBlockAccess.Enabled = !busy;

            btnOk.Enabled = !busy;
            btnCancel.Enabled = !busy;
        }

        private void LvwRecipients_DragEnter(object? sender, DragEventArgs e)
        {
            if (!_isOwnerKeyLoaded || _container.Version != HSTRYContainer.CurrentVersion)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        }

        private void LvwRecipients_DragDrop(object? sender, DragEventArgs e)
        {
            if (!_isOwnerKeyLoaded || _mySigningPrivateKey == null)
                return;

            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this,
                    "This container is not V6.",
                    "Recipients",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
                return;

            int added = 0;

            foreach (var f in files)
            {
                if (!File.Exists(f)) continue;
                if (!string.Equals(Path.GetExtension(f), ".hstrypub", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    using var pub = HSTRYContainer.EcdhKeyFiles.LoadPublicKeySpki(f);
                    _container.AddRecipient(_mySigningPrivateKey, pub);
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

            TryLoadMyEcdhPrivateKey(ofd.FileName, showErrors: true);
            RefreshBlocksList();
            UpdateMyRecipientStatusAndPermissions();
        }

        private void TryLoadMyEcdhPrivateKey(string path, bool showErrors)
        {
            try
            {
                _myEcdhPrivateKey?.Dispose();
                _myEcdhPrivateKey = HSTRYContainer.EcdhKeyFiles.LoadPrivateKeyPkcs8(path);

                byte[] keyId = HSTRYContainer.EcdhKeyFiles.ComputeKeyIdFromPublicKey(_myEcdhPrivateKey);
                _myKeyIdHex = Convert.ToHexString(keyId);

                SelectedEcdhPrivateKeyPath = path;

                _isOwnerKeyLoaded = IsOwnerEcdhPrivateKey(_myEcdhPrivateKey);

                // Try load signing key automatically if present (owner features require it)
                TryLoadSigningKeyForEcdhPath(path, showErrors: showErrors && _isOwnerKeyLoaded);

                UpdateMyKeyUi(path, _myKeyIdHex);
            }
            catch (Exception ex)
            {
                _myEcdhPrivateKey?.Dispose();
                _myEcdhPrivateKey = null;
                _mySigningPrivateKey?.Dispose();
                _mySigningPrivateKey = null;

                _myKeyIdHex = null;
                _isOwnerKeyLoaded = false;

                UpdateMyKeyUi(path, "");

                if (showErrors)
                    MessageBox.Show(this, ex.Message, "Load private key", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TryLoadSigningKeyForEcdhPath(string ecdhPrivPath, bool showErrors)
        {
            try
            {
                _mySigningPrivateKey?.Dispose();
                _mySigningPrivateKey = null;

                string signPath = DeriveSigningPrivateKeyPath(ecdhPrivPath);
                if (!File.Exists(signPath))
                {
                    if (showErrors)
                    {
                        MessageBox.Show(this,
                            "Owner signing key is missing.\n\nExpected file:\n" + signPath,
                            "Owner signing key",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                    return;
                }

                _mySigningPrivateKey = HSTRYContainer.EcdsaKeyFiles.LoadPrivateKeyPkcs8(signPath);
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
            btnTransferOwnership.Enabled = _myEcdhPrivateKey != null; // refined later
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

            // Optional signing set (only needed if you want this recipient to become owner later)
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

            try
            {
                using var ecdh = HSTRYContainer.EcdhKeyFiles.CreateNewKeyPair();
                using var ecdsa = HSTRYContainer.EcdsaKeyFiles.CreateNewKeyPair();

                HSTRYContainer.EcdhKeyFiles.SavePrivateKeyPkcs8(ecdhPrivPath, ecdh);
                HSTRYContainer.EcdhKeyFiles.SavePublicKeySpki(ecdhPubPath, ecdh);

                HSTRYContainer.EcdsaKeyFiles.SavePrivateKeyPkcs8(signPrivPath, ecdsa);
                HSTRYContainer.EcdsaKeyFiles.SavePublicKeySpki(signPubPath, ecdsa);

                var res = MessageBox.Show(
                    this,
                    "Key set created.\n\nDo you want to add the new ECDH public key as a recipient now?",
                    "Create key set",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);

                if (res == DialogResult.Yes)
                {
                    if (_myEcdhPrivateKey == null || !_isOwnerKeyLoaded || _mySigningPrivateKey == null)
                    {
                        MessageBox.Show(this,
                            "Load the owner ECDH private key and ensure the owner signing key exists (*.hstrysigpriv) to modify recipients.",
                            "Create key set",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                    else
                    {
                        using var pub = HSTRYContainer.EcdhKeyFiles.LoadPublicKeySpki(ecdhPubPath);
                        _container.AddRecipient(_mySigningPrivateKey, pub);
                        ContainerChanged = true;

                        RefreshRecipientsList();
                        RefreshBlocksList();
                        UpdateMyRecipientStatusAndPermissions();

                        MessageBox.Show(this,
                            "Recipient added.\n\nNote: New recipients have NO block access by default.",
                            "Create key set",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }

                MessageBox.Show(this,
                    "Key set created successfully.\n\nYou can share the .hstrypub file with other users.",
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
            if (_myEcdhPrivateKey == null)
            {
                MessageBox.Show(this, "No private key is loaded.", "Export public key",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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
                HSTRYContainer.EcdhKeyFiles.SavePublicKeySpki(sfd.FileName, _myEcdhPrivateKey);
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
            if (_myEcdhPrivateKey == null)
            {
                MessageBox.Show(this, "No private key is loaded.", "Transfer ownership",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!_isOwnerKeyLoaded || _mySigningPrivateKey == null)
            {
                MessageBox.Show(this,
                    "Load the current owner ECDH private key and ensure the owner signing key exists (*.hstrysigpriv).",
                    "Transfer ownership",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

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

            bool overwrite = File.Exists(newEcdhPrivPath) || File.Exists(newEcdhPubPath) || File.Exists(newSignPrivPath) || File.Exists(newSignPubPath);
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

                            HSTRYContainer.EcdhKeyFiles.SavePrivateKeyPkcs8(newEcdhPrivPath, newEcdh);
                            HSTRYContainer.EcdhKeyFiles.SavePublicKeySpki(newEcdhPubPath, newEcdh);

                            HSTRYContainer.EcdsaKeyFiles.SavePrivateKeyPkcs8(newSignPrivPath, newSig);
                            HSTRYContainer.EcdsaKeyFiles.SavePublicKeySpki(newSignPubPath, newSig);

                            using var newEcdhReload = HSTRYContainer.EcdhKeyFiles.LoadPrivateKeyPkcs8(newEcdhPrivPath);
                            using var newSigReload = HSTRYContainer.EcdsaKeyFiles.LoadPrivateKeyPkcs8(newSignPrivPath);

                            _container.TransferOwnership(
                                currentOwnerSigningPrivateKey: _mySigningPrivateKey,
                                currentOwnerEcdhPrivateKey: _myEcdhPrivateKey!,
                                newOwnerSigningPrivateKey: newSigReload,
                                newOwnerEcdhPrivateKey: newEcdhReload,
                                progress: progress,
                                token: token);

                            ContainerChanged = true;
                        }, token);
                    });

                // Reload dialog state to new owner key (optional UX)
                TryLoadMyEcdhPrivateKey(newEcdhPrivPath, showErrors: true);

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
            if (_myEcdhPrivateKey == null || string.IsNullOrWhiteSpace(_myKeyIdHex))
            {
                MessageBox.Show(this, "No private key is loaded.", "Add myself",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!_isOwnerKeyLoaded || _mySigningPrivateKey == null)
            {
                MessageBox.Show(this,
                    "Load the owner ECDH private key and ensure the owner signing key exists (*.hstrysigpriv) to modify recipients.",
                    "Add myself",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            bool included = _container.Recipients.Any(r => Convert.ToHexString(r.KeyId)
                .Equals(_myKeyIdHex, StringComparison.OrdinalIgnoreCase));

            if (included)
            {
                UpdateMyRecipientStatusAndPermissions();
                return;
            }

            try
            {
                using var pub = CreatePublicOnlyFromPrivate(_myEcdhPrivateKey);
                _container.AddRecipient(_mySigningPrivateKey, pub);

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
            if (_mySigningPrivateKey == null || !_isOwnerKeyLoaded)
            {
                MessageBox.Show(this,
                    "Load the owner ECDH private key and ensure the owner signing key exists (*.hstrysigpriv) to modify recipients.",
                    "Add recipient",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using var ofd = new OpenFileDialog
            {
                Filter = "HSTRY Public Key (*.hstrypub)|*.hstrypub|All files (*.*)|*.*",
                CheckFileExists = true,
                Title = "Select recipient public key"
            };

            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                using var pub = HSTRYContainer.EcdhKeyFiles.LoadPublicKeySpki(ofd.FileName);
                _container.AddRecipient(_mySigningPrivateKey, pub);

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

            if (_mySigningPrivateKey == null || !_isOwnerKeyLoaded)
            {
                MessageBox.Show(this,
                    "Load the owner ECDH private key and ensure the owner signing key exists (*.hstrysigpriv) to modify recipients.",
                    "Remove recipient",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string keyIdHex = lvwRecipients.SelectedItems[0].Text;

            var res = MessageBox.Show(
                this,
                "Remove the selected recipient from this container?\n\nThis does NOT automatically revoke existing block access.\nUse 'Revoke all' if you want to remove all block rights for this recipient.",
                "Remove recipient",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (res != DialogResult.Yes)
                return;

            try
            {
                if (_container.RemoveRecipientByKeyIdHex(_mySigningPrivateKey, keyIdHex))
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
                    string len = (r.WrappedDek?.Length ?? 0).ToString(CultureInfo.InvariantCulture);
                    string spkiLen = (r.PublicKeySpki?.Length ?? 0).ToString(CultureInfo.InvariantCulture);

                    var it = new ListViewItem(keyIdHex);
                    it.SubItems.Add(alg);
                    it.SubItems.Add(len);
                    it.SubItems.Add(spkiLen);

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

            bool isV6 = _container.Version == HSTRYContainer.CurrentVersion;

            bool canModify = hasEcdh && _isOwnerKeyLoaded && isV6 && _mySigningPrivateKey != null;

            btnAddRecipient.Enabled = canModify;
            btnRemoveRecipient.Enabled = canModify;
            btnAddMyself.Enabled = canModify && !included;

            btnTransferOwnership.Enabled = canModify; // ownership also needs both keys

            bool hasRecipientSelection = lvwRecipients.SelectedItems.Count > 0;
            bool hasBlockSelection = lvwBlocks.SelectedItems.Count > 0;
            bool hasAnyBlocks = _container.Blocks.Count > 0;

            grpBlockAccess.Enabled = isV6;

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

            if (!isV6)
            {
                lblMyRecipientStatus.Text = "Key loaded. This container is not V6.";
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

                if (included)
                {
                    lblMyRecipientStatus.Text = "Owner key loaded. Your key is included as a recipient.";
                    lblMyRecipientStatus.ForeColor = Color.DarkGreen;
                }
                else
                {
                    lblMyRecipientStatus.Text = "Owner key loaded, but your key is NOT a recipient. You can add yourself.";
                    lblMyRecipientStatus.ForeColor = Color.DarkRed;
                }
            }
            else
            {
                if (included)
                {
                    lblMyRecipientStatus.Text = "Recipient key loaded (read-only). You are not the owner.";
                    lblMyRecipientStatus.ForeColor = Color.DarkGoldenrod;
                }
                else
                {
                    lblMyRecipientStatus.Text = "Key loaded, but it is not a recipient and not the owner.";
                    lblMyRecipientStatus.ForeColor = Color.DarkRed;
                }
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
                    lblBlockAccessHint.Text = "Block access control requires V6.";
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
        // Selected-only operations (sync)
        // ============================================================
        private void UiGrantReadSelectedBlock()
        {
            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this, "Block access control requires V6.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_myEcdhPrivateKey == null || !_isOwnerKeyLoaded)
            {
                MessageBox.Show(this, "Load the owner ECDH private key to edit block rights.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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

            if (idx < 0 || idx >= _container.Blocks.Count)
                return;

            try
            {
                var slot = _container.Blocks[idx].KeySlots.FirstOrDefault(s => s.KeyId.SequenceEqual(rec.KeyId));
                if (slot != null && (slot.Rights & BlockRights.Read) != 0)
                    return;

                using var recPub = ECDiffieHellman.Create();
                recPub.ImportSubjectPublicKeyInfo(rec.PublicKeySpki, out _);

                _container.GrantBlockAccess(_myEcdhPrivateKey, idx, recPub, BlockRights.Read, replaceExisting: true);

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
                MessageBox.Show(this, "Block access control requires V6.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_myEcdhPrivateKey == null || !_isOwnerKeyLoaded)
            {
                MessageBox.Show(this, "Load the owner ECDH private key to edit block rights.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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

                    _container.RevokeBlockAccess(_myEcdhPrivateKey, idx, keyIdHex);
                }

                ContainerChanged = true;
                RefreshBlocksList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Block access", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============================================================
        // Bulk operations (async + reporterDiag)
        // ============================================================
        private async Task UiGrantReadAllBlocksAsync()
        {
            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this, "Block access control requires V6.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_myEcdhPrivateKey == null || !_isOwnerKeyLoaded)
            {
                MessageBox.Show(this, "Load the owner ECDH private key to edit block rights.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
                            _container.GrantReadAllBlocks(_myEcdhPrivateKey, recPub, progress, token);
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
                MessageBox.Show(this, "Block access control requires V6.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_myEcdhPrivateKey == null || !_isOwnerKeyLoaded)
            {
                MessageBox.Show(this, "Load the owner ECDH private key to edit block rights.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
                            _container.GrantWriteAllBlocks(_myEcdhPrivateKey, recPub, progress, token);
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
                MessageBox.Show(this, "Block access control requires V6.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_myEcdhPrivateKey == null || !_isOwnerKeyLoaded)
            {
                MessageBox.Show(this, "Load the owner ECDH private key to edit block rights.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
                            _container.RevokeAllBlocks(_myEcdhPrivateKey, keyIdCopy, progress, token);
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
        }
    }
}
