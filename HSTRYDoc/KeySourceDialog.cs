using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HSTRYDoc
{
    public partial class KeySourceDialog : Form
    {
        private readonly string _defaultKeyPath;

        public string? SelectedPrivateKeyPath { get; private set; }

        public KeySourceDialog(string defaultKeyPath)
        {
            InitializeComponent();

            _defaultKeyPath = defaultKeyPath ?? string.Empty;

            // Wire events
            radioDefault.CheckedChanged += (_, __) => UpdateUi();
            radioUsb.CheckedChanged += (_, __) => UpdateUi();
            radioManual.CheckedChanged += (_, __) => UpdateUi();

            lstUsbKeys.SelectedIndexChanged += (_, __) => UpdateUi();

            btnRescan.Click += (_, __) => ScanUsbKeys();
            btnBrowse.Click += (_, __) => BrowseManualKey();

            txtManualPath.TextChanged += (_, __) => UpdateUi();

            btnOk.Click += (_, __) => OnOk();
            btnCancel.Click += (_, __) => DialogResult = DialogResult.Cancel;

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void KeySourceDialog_Load(object? sender, EventArgs e)
        {
            lblDefaultPath.Text = _defaultKeyPath;

            ScanUsbKeys();

            // Default to "Default" if available; otherwise USB if available; else Manual
            if (File.Exists(_defaultKeyPath))
            {
                radioDefault.Checked = true;
            }
            else if (lstUsbKeys.Items.Count > 0)
            {
                radioUsb.Checked = true;
                lstUsbKeys.SelectedIndex = 0;
            }
            else
            {
                radioManual.Checked = true;
            }

            UpdateUi();
        }

        private void ScanUsbKeys()
        {
            lstUsbKeys.BeginUpdate();
            try
            {
                lstUsbKeys.Items.Clear();

                foreach (var p in FindUsbPrivateKeys())
                    lstUsbKeys.Items.Add(p);

                if (lstUsbKeys.Items.Count > 0)
                    lstUsbKeys.SelectedIndex = 0;
            }
            finally
            {
                lstUsbKeys.EndUpdate();
            }

            UpdateUi();
        }

        private static List<string> FindUsbPrivateKeys()
        {
            var result = new List<string>();

            DriveInfo[] drives;
            try { drives = DriveInfo.GetDrives(); }
            catch { return result; }

            foreach (var d in drives)
            {
                // You asked "optional USB/HSTRY_KEY" – include only removable by default.
                if (d.DriveType != DriveType.Removable) continue;
                if (!d.IsReady) continue;

                string dir = Path.Combine(d.RootDirectory.FullName, "HSTRY_KEY");
                if (!Directory.Exists(dir)) continue;

                try
                {
                    // Encrypted private keys are still *.hstrypriv
                    result.AddRange(Directory.GetFiles(dir, "*.hstrypriv", SearchOption.TopDirectoryOnly));
                }
                catch
                {
                    // ignore unreadable USB drives/folders
                }
            }

            return result
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void BrowseManualKey()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "HSTRY Private Key (*.hstrypriv)|*.hstrypriv|All files (*.*)|*.*",
                CheckFileExists = true,
                Title = "Select private key"
            };

            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            txtManualPath.Text = ofd.FileName;
            radioManual.Checked = true;
            UpdateUi();
        }

        private void UpdateUi()
        {
            bool defExists = !string.IsNullOrWhiteSpace(_defaultKeyPath) && File.Exists(_defaultKeyPath);
            lblDefaultStatus.Text = defExists ? "Status: Found" : "Status: Not found";

            bool usbHasItems = lstUsbKeys.Items.Count > 0;
            lblUsbStatus.Text = usbHasItems ? $"Status: {lstUsbKeys.Items.Count} key(s) found" : "Status: No keys found";

            pnlUsb.Enabled = radioUsb.Checked;
            pnlManual.Enabled = radioManual.Checked;

            string? selected = null;

            if (radioDefault.Checked)
            {
                selected = defExists ? _defaultKeyPath : null;
            }
            else if (radioUsb.Checked)
            {
                selected = lstUsbKeys.SelectedItem as string;
                if (string.IsNullOrWhiteSpace(selected) || !File.Exists(selected))
                    selected = null;
            }
            else if (radioManual.Checked)
            {
                string p = (txtManualPath.Text ?? string.Empty).Trim();
                selected = (!string.IsNullOrWhiteSpace(p) && File.Exists(p)) ? p : null;
            }

            SelectedPrivateKeyPath = selected;

            btnOk.Enabled = SelectedPrivateKeyPath != null;

            if (radioDefault.Checked && !defExists)
                lblHint.Text = "The default key is missing. Choose USB or Manual.";
            else if (radioUsb.Checked && !usbHasItems)
                lblHint.Text = "No keys found on USB. Insert a USB drive with folder HSTRY_KEY and click Rescan.";
            else if (radioManual.Checked && SelectedPrivateKeyPath == null)
                lblHint.Text = "Select a .hstrypriv file.";
            else
                lblHint.Text = "Click OK to continue.";
        }

        private void OnOk()
        {
            if (SelectedPrivateKeyPath == null)
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
