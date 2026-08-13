using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HSTRYDoc
{
    public partial class KeySourceDialog : Form
    {
        private readonly string _initialDriveRoot;
        private readonly string _initialPrivateKeyFileName;
        private readonly bool _requirePrivateKey;

        public string? SelectedDriveRoot { get; private set; }
        public string? SelectedPrivateKeyFileName { get; private set; }
        public string? SelectedPrivateKeyPath { get; private set; }

        public KeySourceDialog(string initialDriveRoot, string? initialPrivateKeyFileName = null, bool requirePrivateKey = true)
        {
            InitializeComponent();

            _initialDriveRoot = KeyStorage.NormalizeDriveRoot(initialDriveRoot);
            _initialPrivateKeyFileName = Path.GetFileName(initialPrivateKeyFileName ?? string.Empty);
            _requirePrivateKey = requirePrivateKey;

            lstDrives.SelectedIndexChanged += (_, __) => RefreshKeysForSelectedDrive();
            lstKeys.SelectedIndexChanged += (_, __) => UpdateUi();
            btnRescan.Click += (_, __) => ScanDrives();
            btnOk.Click += (_, __) => OnOk();
            btnCancel.Click += (_, __) => DialogResult = DialogResult.Cancel;

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void KeySourceDialog_Load(object? sender, EventArgs e)
        {
            lblIntro.Text = _requirePrivateKey
                ? "Choose a drive and then select a private key from its HSTRY_KEY folder."
                : "Choose the drive where the HSTRY_KEY folder should be used.";

            ScanDrives();
        }

        private void ScanDrives()
        {
            string? previous = SelectedDriveRoot;

            lstDrives.BeginUpdate();
            try
            {
                lstDrives.Items.Clear();
                foreach (string root in KeyStorage.FindAvailableDriveRoots())
                    lstDrives.Items.Add(root);
            }
            finally
            {
                lstDrives.EndUpdate();
            }

            SelectDrive(previous);
            RefreshKeysForSelectedDrive();
        }

        private void SelectDrive(string? preferredRoot)
        {
            string preferred = KeyStorage.NormalizeDriveRoot(preferredRoot ?? _initialDriveRoot);
            if (!string.IsNullOrWhiteSpace(preferred))
            {
                for (int i = 0; i < lstDrives.Items.Count; i++)
                {
                    if (string.Equals(lstDrives.Items[i] as string, preferred, StringComparison.OrdinalIgnoreCase))
                    {
                        lstDrives.SelectedIndex = i;
                        return;
                    }
                }
            }

            if (lstDrives.Items.Count > 0)
                lstDrives.SelectedIndex = 0;
        }

        private void RefreshKeysForSelectedDrive()
        {
            SelectedDriveRoot = lstDrives.SelectedItem as string;
            string[] keys = string.IsNullOrWhiteSpace(SelectedDriveRoot)
                ? Array.Empty<string>()
                : KeyStorage.FindPrivateKeysInKeyFolder(SelectedDriveRoot);

            lstKeys.BeginUpdate();
            try
            {
                lstKeys.Items.Clear();
                foreach (string fileName in KeyStorage.GetPrivateKeyFileNames(keys))
                    lstKeys.Items.Add(fileName);
            }
            finally
            {
                lstKeys.EndUpdate();
            }

            SelectKey();
            UpdateUi();
        }

        private void SelectKey()
        {
            if (lstKeys.Items.Count == 0)
                return;

            if (!string.IsNullOrWhiteSpace(_initialPrivateKeyFileName))
            {
                for (int i = 0; i < lstKeys.Items.Count; i++)
                {
                    if (string.Equals(lstKeys.Items[i] as string, _initialPrivateKeyFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        lstKeys.SelectedIndex = i;
                        return;
                    }
                }
            }

            lstKeys.SelectedIndex = 0;
        }

        private void UpdateUi()
        {
            SelectedDriveRoot = lstDrives.SelectedItem as string;
            SelectedPrivateKeyFileName = lstKeys.SelectedItem as string;

            string folder = string.IsNullOrWhiteSpace(SelectedDriveRoot)
                ? string.Empty
                : KeyStorage.GetKeyFolderForDrive(SelectedDriveRoot);

            lblKeyFolder.Text = string.IsNullOrWhiteSpace(folder) ? "<no drive selected>" : folder;

            SelectedPrivateKeyPath = (!string.IsNullOrWhiteSpace(SelectedDriveRoot) &&
                                      !string.IsNullOrWhiteSpace(SelectedPrivateKeyFileName))
                ? KeyStorage.GetPrivateKeyPath(SelectedDriveRoot, SelectedPrivateKeyFileName)
                : null;

            bool hasDrive = !string.IsNullOrWhiteSpace(SelectedDriveRoot);
            bool hasKey = !string.IsNullOrWhiteSpace(SelectedPrivateKeyPath) && File.Exists(SelectedPrivateKeyPath);
            btnOk.Enabled = hasDrive && (!_requirePrivateKey || hasKey);

            if (!hasDrive)
                lblHint.Text = "No ready drive is available.";
            else if (_requirePrivateKey && lstKeys.Items.Count == 0)
                lblHint.Text = "No .hstrypriv key was found in this drive's HSTRY_KEY folder.";
            else if (_requirePrivateKey && !hasKey)
                lblHint.Text = "Select a private key.";
            else if (!_requirePrivateKey && !Directory.Exists(folder))
                lblHint.Text = "The HSTRY_KEY folder will be created when needed.";
            else
                lblHint.Text = "Click OK to continue.";
        }

        private void OnOk()
        {
            UpdateUi();

            if (string.IsNullOrWhiteSpace(SelectedDriveRoot))
            {
                MessageBox.Show(this, "No drive was selected.", "Private key",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_requirePrivateKey &&
                (string.IsNullOrWhiteSpace(SelectedPrivateKeyPath) || !File.Exists(SelectedPrivateKeyPath)))
            {
                MessageBox.Show(this, "No valid private key was selected.", "Private key",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
