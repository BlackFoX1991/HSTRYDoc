namespace HSTRYDoc
{
    partial class Chooser
    {
        private System.ComponentModel.IContainer components = null;

        private Button btnNew;
        private Button btnOpen;
        private Button btnExit;
        private Label lblTitle;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Chooser));
            btnNew = new Button();
            btnOpen = new Button();
            btnExit = new Button();
            lblTitle = new Label();
            SuspendLayout();
            // 
            // btnNew
            // 
            btnNew.DialogResult = DialogResult.Yes;
            btnNew.Location = new Point(12, 70);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(100, 32);
            btnNew.TabIndex = 1;
            btnNew.Text = "New";
            // 
            // btnOpen
            // 
            btnOpen.DialogResult = DialogResult.No;
            btnOpen.Location = new Point(128, 70);
            btnOpen.Name = "btnOpen";
            btnOpen.Size = new Size(100, 32);
            btnOpen.TabIndex = 2;
            btnOpen.Text = "Open";
            // 
            // btnExit
            // 
            btnExit.DialogResult = DialogResult.Cancel;
            btnExit.Location = new Point(248, 70);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(100, 32);
            btnExit.TabIndex = 3;
            btnExit.Text = "Exit";
            // 
            // lblTitle
            // 
            lblTitle.Location = new Point(12, 12);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(336, 40);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "What would you like to do?";
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // Chooser
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(360, 109);
            Controls.Add(lblTitle);
            Controls.Add(btnNew);
            Controls.Add(btnOpen);
            Controls.Add(btnExit);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Chooser";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "HstryDocu Start...";
            ResumeLayout(false);
        }
    }
}
