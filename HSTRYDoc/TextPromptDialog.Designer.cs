namespace HSTRYDoc
{
    partial class TextPromptDialog
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblInfo;
        private Label lblText;
        private TextBox txtInput;
        private Button btnOk;
        private Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TextPromptDialog));
            lblInfo = new Label();
            lblText = new Label();
            txtInput = new TextBox();
            btnOk = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // lblInfo
            // 
            lblInfo.Location = new Point(12, 12);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(436, 36);
            lblInfo.TabIndex = 0;
            lblInfo.Text = "Info";
            // 
            // lblText
            // 
            lblText.AutoSize = true;
            lblText.Location = new Point(12, 62);
            lblText.Name = "lblText";
            lblText.Size = new Size(31, 15);
            lblText.TabIndex = 1;
            lblText.Text = "Text:";
            // 
            // txtInput
            // 
            txtInput.Location = new Point(60, 59);
            txtInput.Name = "txtInput";
            txtInput.Size = new Size(388, 23);
            txtInput.TabIndex = 2;
            // 
            // btnOk
            // 
            btnOk.Location = new Point(282, 118);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(80, 27);
            btnOk.TabIndex = 3;
            btnOk.Text = "OK";
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(368, 118);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(80, 27);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            // 
            // TextPromptDialog
            // 
            ClientSize = new Size(460, 160);
            Controls.Add(lblInfo);
            Controls.Add(lblText);
            Controls.Add(txtInput);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "TextPromptDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
