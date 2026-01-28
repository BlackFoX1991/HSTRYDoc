// KeyManagerDialog.cs (V4 Option A: per-block access control + ASYNC bulk ops)
// Behavior:
// - Grant write (all): applies Read+Write to ALL blocks (async + reporterDiag)
// - Grant read (selected): applies Read to the currently selected block (only if not already granted)
// - Grant read (all): applies Read to ALL blocks (only if not already granted) (async + reporterDiag)
// - Revoke (selected): revokes access on selected blocks
// - Revoke all: revokes access on ALL blocks (async + reporterDiag)

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

        private RSA? _myPrivateKey;
        private string? _myKeyIdHex;
        private bool _isOwnerKeyLoaded;

        public string? SelectedPrivateKeyPath { get; private set; }
        public bool ContainerChanged { get; private set; }

        public KeyManagerDialog(HSTRYContainer container, string containerPath, string? currentPrivateKeyPath)
        {
            InitializeComponent();

            _container = container ?? throw new ArgumentNullException(nameof(container));
            _containerPath = containerPath ?? string.Empty;

            SelectedPrivateKeyPath = currentPrivateKeyPath;

            btnBrowsePriv.Click += (_, __) => UiBrowsePrivateKey();
            btnCreateKeyPair.Click += (_, __) => UiCreateKeyPairForSharing();
            btnExportPublic.Click += (_, __) => UiExportPublicKey();
            btnTransferOwnership.Click += (_, __) => UiTransferOwnership();

            btnAddMyself.Click += (_, __) => UiAddMyselfAsRecipient();
            btnAddRecipient.Click += (_, __) => UiAddRecipient();
            btnRemoveRecipient.Click += (_, __) => UiRemoveSelectedRecipient();
            btnCopyKeyId.Click += (_, __) => UiCopySelectedKeyId();

            // Block access
            btnGrantRead.Click += (_, __) => UiGrantReadSelectedBlock();                 // selected only (sync)
            btnRevokeAccess.Click += (_, __) => UiRevokeSelectedBlocks();                // selected only (sync)

            // Bulk (async + reporterDiag)
            btnGrantReadAll.Click += async (_, __) => await UiGrantReadAllBlocksAsync();
            btnGrantWrite.Click += async (_, __) => await UiGrantWriteAllBlocksAsync(); // Read+Write for ALL
            btnRevokeAll.Click += async (_, __) => await UiRevokeAllBlocksAsync();

            btnOk.Click += (_, __) => { DialogResult = DialogResult.OK; Close(); };
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            // Drag & drop public keys onto recipients list
            lvwRecipients.AllowDrop = true;
            lvwRecipients.DragEnter += LvwRecipients_DragEnter;
            lvwRecipients.DragDrop += LvwRecipients_DragDrop;

            // Refresh blocks view when changing selected recipient
            lvwRecipients.SelectedIndexChanged += (_, __) => RefreshBlocksList();

            // Update enabled state when selecting blocks
            lvwBlocks.SelectedIndexChanged += (_, __) => UpdateMyRecipientStatusAndPermissions();
            lvwBlocks.ItemSelectionChanged += (_, __) => UpdateMyRecipientStatusAndPermissions();

            RefreshRecipientsList();

            if (!string.IsNullOrWhiteSpace(SelectedPrivateKeyPath) && File.Exists(SelectedPrivateKeyPath))
                TryLoadMyPrivateKey(SelectedPrivateKeyPath!, showErrors: false);
            else
                UpdateMyKeyUi(null, null);

            UpdateMyRecipientStatusAndPermissions();
            RefreshBlocksList();
        }

        private void SetUiBusy(bool busy)
        {
            // Keep cancel/ok behavior simple: while busy, prevent operations
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
            if (!_isOwnerKeyLoaded || _myPrivateKey == null)
                return;

            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this,
                    "This container is not V4. Upgrade it to V4 before editing recipients or block rights.",
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
                    using var pub = HSTRYContainer.RsaKeyFiles.LoadPublicKeySpki(f);
                    _container.AddRecipient(_myPrivateKey, pub);
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
                    "Recipients added.\n\nNote: New recipients have NO block access by default in V4.\nSelect the recipient and use the Block access tools below.",
                    "Recipients",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void UiBrowsePrivateKey()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "HSTRY Private Key (*.hstrypriv)|*.hstrypriv|All files (*.*)|*.*",
                CheckFileExists = true,
                Title = "Select private key"
            };

            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            TryLoadMyPrivateKey(ofd.FileName, showErrors: true);
            RefreshBlocksList();
            UpdateMyRecipientStatusAndPermissions();
        }

        private void TryLoadMyPrivateKey(string path, bool showErrors)
        {
            try
            {
                _myPrivateKey?.Dispose();
                _myPrivateKey = HSTRYContainer.RsaKeyFiles.LoadPrivateKeyPkcs8(path);

                byte[] keyId = HSTRYContainer.RsaKeyFiles.ComputeKeyIdFromPublicKey(_myPrivateKey);
                _myKeyIdHex = Convert.ToHexString(keyId);

                SelectedPrivateKeyPath = path;

                _isOwnerKeyLoaded = IsOwnerPrivateKey(_myPrivateKey);

                UpdateMyKeyUi(path, _myKeyIdHex);
            }
            catch (Exception ex)
            {
                _myPrivateKey?.Dispose();
                _myPrivateKey = null;
                _myKeyIdHex = null;
                _isOwnerKeyLoaded = false;

                UpdateMyKeyUi(path, "");

                if (showErrors)
                    MessageBox.Show(this, ex.Message, "Load private key", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsOwnerPrivateKey(RSA priv)
        {
            try
            {
                byte[] spki = priv.ExportSubjectPublicKeyInfo();
                return spki.SequenceEqual(_container.OwnerPublicKeySpki);
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

            btnExportPublic.Enabled = _myPrivateKey != null;
            btnTransferOwnership.Enabled = _myPrivateKey != null; // refined in UpdateMyRecipientStatusAndPermissions
        }

        // ============================================================
        // Create new keypair
        // ============================================================
        private void UiCreateKeyPairForSharing()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "HSTRY Private Key (*.hstrypriv)|*.hstrypriv|All files (*.*)|*.*",
                DefaultExt = "hstrypriv",
                AddExtension = true,
                FileName = "recipient.hstrypriv",
                Title = "Save new private key"
            };

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;

            string privPath = sfd.FileName;
            string pubPath = Path.ChangeExtension(privPath, ".hstrypub");

            bool overwrite = File.Exists(privPath) || File.Exists(pubPath);
            if (overwrite)
            {
                var res = MessageBox.Show(
                    this,
                    "Key files already exist. Overwrite them?",
                    "Create key pair",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (res != DialogResult.Yes)
                    return;
            }

            try
            {
                using var rsa = HSTRYContainer.RsaKeyFiles.CreateNewKeyPair(3072);
                HSTRYContainer.RsaKeyFiles.SavePrivateKeyPkcs8(privPath, rsa);
                HSTRYContainer.RsaKeyFiles.SavePublicKeySpki(pubPath, rsa);

                var res = MessageBox.Show(
                    this,
                    "Key pair created.\n\nDo you want to add the new public key as a recipient now?",
                    "Create key pair",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);

                if (res == DialogResult.Yes)
                {
                    if (_myPrivateKey == null || !_isOwnerKeyLoaded)
                    {
                        MessageBox.Show(this, "Load the owner private key to modify recipients.", "Create key pair",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else if (_container.Version != HSTRYContainer.CurrentVersion)
                    {
                        MessageBox.Show(this, "This container is not V4. Upgrade it to V4 before editing recipients.", "Create key pair",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        using var pub = HSTRYContainer.RsaKeyFiles.LoadPublicKeySpki(pubPath);
                        _container.AddRecipient(_myPrivateKey, pub);
                        ContainerChanged = true;
                        RefreshRecipientsList();
                        RefreshBlocksList();
                        UpdateMyRecipientStatusAndPermissions();

                        MessageBox.Show(this,
                            "Recipient added.\n\nNote: New recipients have NO block access by default in V4.",
                            "Create key pair",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }

                MessageBox.Show(this,
                    "Key pair created successfully.\n\nYou can share the .hstrypub file with other users.",
                    "Create key pair",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Create key pair", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UiExportPublicKey()
        {
            if (_myPrivateKey == null)
            {
                MessageBox.Show(this, "No private key is loaded.", "Export public key",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string defaultPub = !string.IsNullOrWhiteSpace(SelectedPrivateKeyPath)
                ? Path.ChangeExtension(SelectedPrivateKeyPath, ".hstrypub")
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
                HSTRYContainer.RsaKeyFiles.SavePublicKeySpki(sfd.FileName, _myPrivateKey);
                MessageBox.Show(this, "Public key exported successfully.", "Export public key",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Export public key", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UiTransferOwnership()
        {
            if (_myPrivateKey == null)
            {
                MessageBox.Show(this, "No private key is loaded.", "Transfer ownership",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!_isOwnerKeyLoaded)
            {
                MessageBox.Show(this, "You must load the current owner private key to transfer ownership.", "Transfer ownership",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this,
                    "This container is not V4. Upgrade it to V4 before transferring ownership.",
                    "Transfer ownership",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                this,
                "This will transfer container ownership to a NEW key pair.\n\nContinue?",
                "Transfer ownership",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes)
                return;

            using var sfd = new SaveFileDialog
            {
                Filter = "HSTRY Private Key (*.hstrypriv)|*.hstrypriv|All files (*.*)|*.*",
                DefaultExt = "hstrypriv",
                AddExtension = true,
                FileName = "new_owner.hstrypriv",
                Title = "Save new owner private key"
            };

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;

            string newPrivPath = sfd.FileName;
            string newPubPath = Path.ChangeExtension(newPrivPath, ".hstrypub");

            bool overwrite = File.Exists(newPrivPath) || File.Exists(newPubPath);
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

            try
            {
                using var newOwner = HSTRYContainer.RsaKeyFiles.CreateNewKeyPair(3072);

                HSTRYContainer.RsaKeyFiles.SavePrivateKeyPkcs8(newPrivPath, newOwner);
                HSTRYContainer.RsaKeyFiles.SavePublicKeySpki(newPubPath, newOwner);

                _container.TransferOwnership(_myPrivateKey, newOwner, ensureNewOwnerIsRecipient: true);
                ContainerChanged = true;

                TryLoadMyPrivateKey(newPrivPath, showErrors: true);
                RefreshRecipientsList();
                RefreshBlocksList();
                UpdateMyRecipientStatusAndPermissions();

                MessageBox.Show(this,
                    "Ownership transferred successfully.\n\nThe new owner key pair has been saved to disk.",
                    "Transfer ownership",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Transfer ownership", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============================================================
        // Recipient operations (owner-only)
        // ============================================================

        private void UiAddMyselfAsRecipient()
        {
            if (_myPrivateKey == null || string.IsNullOrWhiteSpace(_myKeyIdHex))
            {
                MessageBox.Show(this, "No private key is loaded.", "Add myself",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!_isOwnerKeyLoaded)
            {
                MessageBox.Show(this, "You must load the owner private key to modify recipients.", "Add myself",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this,
                    "This container is not V4. Upgrade it to V4 before editing recipients or block rights.",
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
                using var pub = CreatePublicOnlyFromPrivate(_myPrivateKey);
                _container.AddRecipient(_myPrivateKey, pub);

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

        private static RSA CreatePublicOnlyFromPrivate(RSA priv)
        {
            byte[] spki = priv.ExportSubjectPublicKeyInfo();
            var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(spki, out _);
            return rsa;
        }

        private void UiAddRecipient()
        {
            if (_myPrivateKey == null || !_isOwnerKeyLoaded)
            {
                MessageBox.Show(this, "You must load the owner private key to modify recipients.", "Add recipient",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this,
                    "This container is not V4. Upgrade it to V4 before editing recipients or block rights.",
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
                using var pub = HSTRYContainer.RsaKeyFiles.LoadPublicKeySpki(ofd.FileName);
                _container.AddRecipient(_myPrivateKey, pub);

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

            if (_myPrivateKey == null || !_isOwnerKeyLoaded)
            {
                MessageBox.Show(this, "You must load the owner private key to modify recipients.", "Remove recipient",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_container.Version != HSTRYContainer.CurrentVersion)
            {
                MessageBox.Show(this,
                    "This container is not V4. Upgrade it to V4 before editing recipients or block rights.",
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
                if (_container.RemoveRecipientByKeyIdHex(_myPrivateKey, keyIdHex))
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
            try { Clipboard.SetText(keyIdHex); } catch { /* ignore */ }
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
                    string alg = r.Alg == 1 ? "RSA-OAEP-SHA256" : $"Alg-{r.Alg}";
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
            bool hasKey = _myPrivateKey != null && !string.IsNullOrWhiteSpace(_myKeyIdHex);

            bool included = hasKey && _container.Recipients.Any(r =>
                Convert.ToHexString(r.KeyId).Equals(_myKeyIdHex!, StringComparison.OrdinalIgnoreCase));

            bool isV4 = _container.Version == HSTRYContainer.CurrentVersion;
            bool canModify = hasKey && _isOwnerKeyLoaded && isV4;

            btnAddRecipient.Enabled = canModify;
            btnRemoveRecipient.Enabled = canModify;
            btnAddMyself.Enabled = canModify && !included;
            btnTransferOwnership.Enabled = canModify;

            bool hasRecipientSelection = lvwRecipients.SelectedItems.Count > 0;
            bool hasBlockSelection = lvwBlocks.SelectedItems.Count > 0;
            bool hasAnyBlocks = _container.Blocks.Count > 0;

            grpBlockAccess.Enabled = isV4;

            // Selected-only buttons
            btnGrantRead.Enabled = canModify && hasRecipientSelection && hasBlockSelection;
            btnRevokeAccess.Enabled = canModify && hasRecipientSelection && hasBlockSelection;

            // All-block buttons
            btnGrantWrite.Enabled = canModify && hasRecipientSelection && hasAnyBlocks;
            btnGrantReadAll.Enabled = canModify && hasRecipientSelection && hasAnyBlocks;
            btnRevokeAll.Enabled = canModify && hasRecipientSelection && hasAnyBlocks;

            if (!hasKey)
            {
                lblMyRecipientStatus.Text = "No private key loaded";
                lblMyRecipientStatus.ForeColor = SystemColors.ControlText;
                return;
            }

            if (!isV4)
            {
                lblMyRecipientStatus.Text = "Key loaded. This container is not V4. Upgrade to V4 to edit recipients or block rights.";
                lblMyRecipientStatus.ForeColor = Color.DarkRed;
                return;
            }

            if (_isOwnerKeyLoaded)
            {
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
                    lblBlockAccessHint.Text = "Block access control requires V4.";
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
                MessageBox.Show(this, "Block access control requires V4.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_myPrivateKey == null || !_isOwnerKeyLoaded)
            {
                MessageBox.Show(this, "Load the owner private key to edit block rights.", "Block access",
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

                using var rsaPub = RSA.Create();
                rsaPub.ImportSubjectPublicKeyInfo(rec.PublicKeySpki, out _);

                _container.GrantBlockAccess(_myPrivateKey, idx, rsaPub, BlockRights.Read, replaceExisting: true);

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
                MessageBox.Show(this, "Block access control requires V4.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_myPrivateKey == null || !_isOwnerKeyLoaded)
            {
                MessageBox.Show(this, "Load the owner private key to edit block rights.", "Block access",
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

                    _container.RevokeBlockAccess(_myPrivateKey, idx, keyIdHex);
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
                MessageBox.Show(this, "Block access control requires V4.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_myPrivateKey == null || !_isOwnerKeyLoaded)
            {
                MessageBox.Show(this, "Load the owner private key to edit block rights.", "Block access",
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
                byte[] recSpki = rec.PublicKeySpki.ToArray(); // stable for async

                await reporterDiag.RunAsync(
                    owner: this,
                    title: "Grant read (all)",
                    work: async (progress, token) =>
                    {
                        using var rsaPub = RSA.Create();
                        rsaPub.ImportSubjectPublicKeyInfo(recSpki, out _);

                        await Task.Run(() =>
                        {
                            // requires HSTRYContainer.GrantReadAllBlocks(...)
                            _container.GrantReadAllBlocks(_myPrivateKey, rsaPub, progress, token);
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
                MessageBox.Show(this, "Block access control requires V4.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_myPrivateKey == null || !_isOwnerKeyLoaded)
            {
                MessageBox.Show(this, "Load the owner private key to edit block rights.", "Block access",
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
                        using var rsaPub = RSA.Create();
                        rsaPub.ImportSubjectPublicKeyInfo(recSpki, out _);

                        await Task.Run(() =>
                        {
                            // requires HSTRYContainer.GrantWriteAllBlocks(...)
                            _container.GrantWriteAllBlocks(_myPrivateKey, rsaPub, progress, token);
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
                MessageBox.Show(this, "Block access control requires V4.", "Block access",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_myPrivateKey == null || !_isOwnerKeyLoaded)
            {
                MessageBox.Show(this, "Load the owner private key to edit block rights.", "Block access",
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
                            // requires HSTRYContainer.RevokeAllBlocks(...)
                            _container.RevokeAllBlocks(_myPrivateKey, keyIdCopy, progress, token);
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
            _myPrivateKey?.Dispose();
            _myPrivateKey = null;
        }
    }
}
