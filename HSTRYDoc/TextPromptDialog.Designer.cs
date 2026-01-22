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
            components = new System.ComponentModel.Container();

            lblInfo = new Label();
            lblText = new Label();
            txtInput = new TextBox();
            btnOk = new Button();
            btnCancel = new Button();

            SuspendLayout();

            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 160);

            lblInfo.AutoSize = false;
            lblInfo.Location = new Point(12, 12);
            lblInfo.Size = new Size(436, 36);
            lblInfo.Text = "Info";

            lblText.AutoSize = true;
            lblText.Location = new Point(12, 62);
            lblText.Text = "Text:";

            txtInput.Location = new Point(60, 59);
            txtInput.Size = new Size(388, 23);

            btnOk.Location = new Point(282, 118);
            btnOk.Size = new Size(80, 27);
            btnOk.Text = "OK";

            btnCancel.Location = new Point(368, 118);
            btnCancel.Size = new Size(80, 27);
            btnCancel.Text = "Cancel";

            Controls.Add(lblInfo);
            Controls.Add(lblText);
            Controls.Add(txtInput);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
