namespace HSTRYDoc
{
    partial class PasswordDialog
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblInfo;
        private Label lblPassword;
        private Label lblConfirm;
        private TextBox txtPassword;
        private TextBox txtConfirm;
        private Button btnOk;
        private Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            lblInfo = new Label();
            lblPassword = new Label();
            lblConfirm = new Label();
            txtPassword = new TextBox();
            txtConfirm = new TextBox();
            btnOk = new Button();
            btnCancel = new Button();

            SuspendLayout();

            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 185);

            lblInfo.AutoSize = false;
            lblInfo.Location = new Point(12, 12);
            lblInfo.Size = new Size(396, 36);
            lblInfo.Text = "Info";

            lblPassword.AutoSize = true;
            lblPassword.Location = new Point(12, 60);
            lblPassword.Text = "Password:";

            txtPassword.Location = new Point(100, 57);
            txtPassword.Size = new Size(308, 23);
            txtPassword.UseSystemPasswordChar = true;

            lblConfirm.AutoSize = true;
            lblConfirm.Location = new Point(12, 92);
            lblConfirm.Text = "Confirm:";

            txtConfirm.Location = new Point(100, 89);
            txtConfirm.Size = new Size(308, 23);
            txtConfirm.UseSystemPasswordChar = true;

            btnOk.Location = new Point(242, 140);
            btnOk.Size = new Size(80, 27);
            btnOk.Text = "OK";

            btnCancel.Location = new Point(328, 140);
            btnCancel.Size = new Size(80, 27);
            btnCancel.Text = "Cancel";

            Controls.Add(lblInfo);
            Controls.Add(lblPassword);
            Controls.Add(txtPassword);
            Controls.Add(lblConfirm);
            Controls.Add(txtConfirm);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
