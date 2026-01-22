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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PasswordDialog));
            lblInfo = new Label();
            lblPassword = new Label();
            lblConfirm = new Label();
            txtPassword = new TextBox();
            txtConfirm = new TextBox();
            btnOk = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // lblInfo
            // 
            lblInfo.Location = new Point(12, 12);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(396, 36);
            lblInfo.TabIndex = 0;
            lblInfo.Text = "Info";
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Location = new Point(12, 60);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(60, 15);
            lblPassword.TabIndex = 1;
            lblPassword.Text = "Password:";
            // 
            // lblConfirm
            // 
            lblConfirm.AutoSize = true;
            lblConfirm.Location = new Point(12, 92);
            lblConfirm.Name = "lblConfirm";
            lblConfirm.Size = new Size(54, 15);
            lblConfirm.TabIndex = 3;
            lblConfirm.Text = "Confirm:";
            // 
            // txtPassword
            // 
            txtPassword.Location = new Point(100, 57);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(308, 23);
            txtPassword.TabIndex = 2;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // txtConfirm
            // 
            txtConfirm.Location = new Point(100, 89);
            txtConfirm.Name = "txtConfirm";
            txtConfirm.Size = new Size(308, 23);
            txtConfirm.TabIndex = 4;
            txtConfirm.UseSystemPasswordChar = true;
            // 
            // btnOk
            // 
            btnOk.Location = new Point(242, 140);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(80, 27);
            btnOk.TabIndex = 5;
            btnOk.Text = "OK";
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(328, 140);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(80, 27);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "Cancel";
            // 
            // PasswordDialog
            // 
            ClientSize = new Size(420, 185);
            Controls.Add(lblInfo);
            Controls.Add(lblPassword);
            Controls.Add(txtPassword);
            Controls.Add(lblConfirm);
            Controls.Add(txtConfirm);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PasswordDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
