namespace HSTRYDoc
{
    public partial class PasswordDialog : Form
    {
        public string Password => txtPassword.Text ?? string.Empty;

        public PasswordDialog(string title, string info, bool requireConfirm)
        {
            InitializeComponent();

            Text = title;
            lblInfo.Text = info;

            lblConfirm.Visible = requireConfirm;
            txtConfirm.Visible = requireConfirm;

            btnOk.Click += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MessageBox.Show(this, "Passwort darf nicht leer sein.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                if (requireConfirm && !string.Equals(txtPassword.Text, txtConfirm.Text, StringComparison.Ordinal))
                {
                    MessageBox.Show(this, "Passwörter stimmen nicht überein.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            };

            btnCancel.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }
}
