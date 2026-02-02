// PasswordDialog.cs
using System;
using System.Windows.Forms;

namespace HSTRYDoc
{
    /// <summary>
    /// Simple password dialog for WinForms.
    /// UI strings are English.
    /// </summary>
    public partial class PasswordDialog : Form
    {
        public enum PasswordDialogMode
        {
            Prompt,     // ask for an existing password
            SetNew      // ask for new password + confirmation
        }

        public string Password { get; private set; } = string.Empty;

        private readonly PasswordDialogMode _mode;

        public PasswordDialog(
            string title = "Password",
            string prompt = "Enter password:",
            PasswordDialogMode mode = PasswordDialogMode.Prompt)
        {
            InitializeComponent();

            Text = string.IsNullOrWhiteSpace(title) ? "Password" : title;
            lblPrompt.Text = string.IsNullOrWhiteSpace(prompt) ? "Enter password:" : prompt;

            _mode = mode;
            ApplyMode(mode);

            btnOk.Click += (_, __) => TryAccept();
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            chkShow.CheckedChanged += (_, __) => ApplyPasswordMask();
            txtPassword.TextChanged += (_, __) => UpdateCapsLockHint();
            txtConfirm.TextChanged += (_, __) => UpdateCapsLockHint();

            txtPassword.KeyDown += Txt_KeyDown;
            txtConfirm.KeyDown += Txt_KeyDown;

            Shown += (_, __) =>
            {
                UpdateCapsLockHint();
                txtPassword.Focus();
            };

            FormClosing += (_, __) =>
            {
                // If not accepted, do not keep password in memory longer than needed.
                if (DialogResult != DialogResult.OK)
                    ClearSensitiveFields();
            };
        }

        private void ApplyMode(PasswordDialogMode mode)
        {
            bool confirm = mode == PasswordDialogMode.SetNew;

            lblConfirm.Visible = confirm;
            txtConfirm.Visible = confirm;

            if (!confirm)
                txtConfirm.Text = string.Empty;

            btnOk.Text = confirm ? "Set" : "OK";
            btnCancel.Text = "Cancel";

            lblHint.Text = confirm
                ? "Use a strong password. Keep it safe."
                : "Password is required to unlock the key.";

            ApplyPasswordMask();
        }

        private void Txt_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                TryAccept();
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void ApplyPasswordMask()
        {
            bool show = chkShow.Checked;
            txtPassword.UseSystemPasswordChar = !show;
            txtConfirm.UseSystemPasswordChar = !show;
        }

        private void UpdateCapsLockHint()
        {
            bool caps = Control.IsKeyLocked(Keys.CapsLock);
            lblCapsLock.Visible = caps;
            lblCapsLock.Text = caps ? "Caps Lock is ON" : string.Empty;
        }

        private void TryAccept()
        {
            string p1 = txtPassword.Text ?? string.Empty;
            string p2 = txtConfirm.Text ?? string.Empty;

            if (string.IsNullOrEmpty(p1))
            {
                MessageBox.Show(this, "Password must not be empty.", "Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            if (_mode == PasswordDialogMode.SetNew)
            {
                if (!string.Equals(p1, p2, StringComparison.Ordinal))
                {
                    MessageBox.Show(this, "Passwords do not match.", "Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtConfirm.Focus();
                    txtConfirm.SelectAll();
                    return;
                }

                // Soft warning for very short passwords
                if (p1.Length < 8)
                {
                    var res = MessageBox.Show(
                        this,
                        "This password is shorter than 8 characters.\n\nUse it anyway?",
                        "Password",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);

                    if (res != DialogResult.Yes)
                    {
                        txtPassword.Focus();
                        txtPassword.SelectAll();
                        return;
                    }
                }
            }

            Password = p1;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ClearSensitiveFields()
        {
            Password = string.Empty;
            txtPassword.Text = string.Empty;
            txtConfirm.Text = string.Empty;
        }

        // Convenience helper: shows the dialog and returns password if OK, else null.
        public static string? ShowPassword(
            IWin32Window owner,
            string title,
            string prompt,
            PasswordDialogMode mode = PasswordDialogMode.Prompt)
        {
            using var dlg = new PasswordDialog(title, prompt, mode);
            return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.Password : null;
        }
    }
}
