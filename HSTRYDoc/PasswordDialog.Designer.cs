// PasswordDialog.Designer.cs
namespace HSTRYDoc
{
    partial class PasswordDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblPrompt = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblConfirm = new System.Windows.Forms.Label();
            this.txtConfirm = new System.Windows.Forms.TextBox();
            this.chkShow = new System.Windows.Forms.CheckBox();
            this.lblCapsLock = new System.Windows.Forms.Label();
            this.lblHint = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblPrompt
            // 
            this.lblPrompt.AutoSize = true;
            this.lblPrompt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPrompt.Location = new System.Drawing.Point(3, 0);
            this.lblPrompt.Margin = new System.Windows.Forms.Padding(3, 0, 3, 4);
            this.lblPrompt.Name = "lblPrompt";
            this.lblPrompt.Size = new System.Drawing.Size(378, 16);
            this.lblPrompt.TabIndex = 0;
            this.lblPrompt.Text = "Enter password:";
            // 
            // txtPassword
            // 
            this.txtPassword.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtPassword.Location = new System.Drawing.Point(3, 20);
            this.txtPassword.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(378, 23);
            this.txtPassword.TabIndex = 1;
            this.txtPassword.UseSystemPasswordChar = true;
            // 
            // lblConfirm
            // 
            this.lblConfirm.AutoSize = true;
            this.lblConfirm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblConfirm.Location = new System.Drawing.Point(3, 53);
            this.lblConfirm.Margin = new System.Windows.Forms.Padding(3, 0, 3, 4);
            this.lblConfirm.Name = "lblConfirm";
            this.lblConfirm.Size = new System.Drawing.Size(378, 16);
            this.lblConfirm.TabIndex = 2;
            this.lblConfirm.Text = "Confirm password:";
            // 
            // txtConfirm
            // 
            this.txtConfirm.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtConfirm.Location = new System.Drawing.Point(3, 73);
            this.txtConfirm.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this.txtConfirm.Name = "txtConfirm";
            this.txtConfirm.Size = new System.Drawing.Size(378, 23);
            this.txtConfirm.TabIndex = 3;
            this.txtConfirm.UseSystemPasswordChar = true;
            // 
            // chkShow
            // 
            this.chkShow.AutoSize = true;
            this.chkShow.Dock = System.Windows.Forms.DockStyle.Left;
            this.chkShow.Location = new System.Drawing.Point(3, 106);
            this.chkShow.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this.chkShow.Name = "chkShow";
            this.chkShow.Size = new System.Drawing.Size(122, 19);
            this.chkShow.TabIndex = 4;
            this.chkShow.Text = "Show password";
            this.chkShow.UseVisualStyleBackColor = true;
            // 
            // lblCapsLock
            // 
            this.lblCapsLock.AutoSize = true;
            this.lblCapsLock.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblCapsLock.ForeColor = System.Drawing.Color.DarkRed;
            this.lblCapsLock.Location = new System.Drawing.Point(291, 106);
            this.lblCapsLock.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this.lblCapsLock.Name = "lblCapsLock";
            this.lblCapsLock.Size = new System.Drawing.Size(90, 19);
            this.lblCapsLock.TabIndex = 5;
            this.lblCapsLock.Text = "Caps Lock is ON";
            this.lblCapsLock.Visible = false;
            // 
            // lblHint
            // 
            this.lblHint.AutoSize = true;
            this.lblHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHint.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblHint.Location = new System.Drawing.Point(3, 131);
            this.lblHint.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(378, 15);
            this.lblHint.TabIndex = 6;
            this.lblHint.Text = "Password is required to unlock the key.";
            // 
            // btnOk
            // 
            this.btnOk.AutoSize = true;
            this.btnOk.Location = new System.Drawing.Point(221, 3);
            this.btnOk.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);
            this.btnOk.MinimumSize = new System.Drawing.Size(80, 0);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(80, 25);
            this.btnOk.TabIndex = 0;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.AutoSize = true;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(307, 3);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.btnCancel.MinimumSize = new System.Drawing.Size(80, 0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 25);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.lblPrompt, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.txtPassword, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblConfirm, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.txtConfirm, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.chkShow, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.lblHint, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.buttonPanel, 0, 6);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 12);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 7;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(384, 231);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // buttonPanel
            // 
            this.buttonPanel.AutoSize = true;
            this.buttonPanel.Controls.Add(this.btnOk);
            this.buttonPanel.Controls.Add(this.btnCancel);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonPanel.Location = new System.Drawing.Point(0, 193);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(0);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.buttonPanel.Size = new System.Drawing.Size(384, 38);
            this.buttonPanel.TabIndex = 7;
            // 
            // PasswordDialog
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(408, 255);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.lblCapsLock);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PasswordDialog";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Password";
            // CapsLock label placement
            this.lblCapsLock.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCapsLock.Location = new System.Drawing.Point(290, 118);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.buttonPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblPrompt;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblConfirm;
        private System.Windows.Forms.TextBox txtConfirm;
        private System.Windows.Forms.CheckBox chkShow;
        private System.Windows.Forms.Label lblCapsLock;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel buttonPanel;
    }
}
