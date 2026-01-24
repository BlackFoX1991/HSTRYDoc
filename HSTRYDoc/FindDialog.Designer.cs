namespace HSTRYDoc
{
    partial class FindDialog
    {
        private System.ComponentModel.IContainer components = null;

        private TextBox txtQuery;
        private CheckBox chkMatchCase;
        private CheckBox chkWholeWord;
        private CheckBox chkWrap;
        private Button btnFindNext;
        private Button btnClose;
        private Label lblQuery;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FindDialog));
            txtQuery = new TextBox();
            chkMatchCase = new CheckBox();
            chkWholeWord = new CheckBox();
            chkWrap = new CheckBox();
            btnFindNext = new Button();
            btnClose = new Button();
            lblQuery = new Label();
            SuspendLayout();
            // 
            // txtQuery
            // 
            txtQuery.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtQuery.Location = new Point(60, 12);
            txtQuery.Name = "txtQuery";
            txtQuery.Size = new Size(348, 27);
            txtQuery.TabIndex = 1;
            // 
            // chkMatchCase
            // 
            chkMatchCase.AutoSize = true;
            chkMatchCase.Location = new Point(60, 48);
            chkMatchCase.Name = "chkMatchCase";
            chkMatchCase.Size = new Size(105, 24);
            chkMatchCase.TabIndex = 2;
            chkMatchCase.Text = "Match case";
            // 
            // chkWholeWord
            // 
            chkWholeWord.AutoSize = true;
            chkWholeWord.Location = new Point(60, 74);
            chkWholeWord.Name = "chkWholeWord";
            chkWholeWord.Size = new Size(112, 24);
            chkWholeWord.TabIndex = 3;
            chkWholeWord.Text = "Whole word";
            // 
            // chkWrap
            // 
            chkWrap.AutoSize = true;
            chkWrap.Checked = true;
            chkWrap.CheckState = CheckState.Checked;
            chkWrap.Location = new Point(60, 100);
            chkWrap.Name = "chkWrap";
            chkWrap.Size = new Size(67, 24);
            chkWrap.TabIndex = 4;
            chkWrap.Text = "Wrap";
            // 
            // btnFindNext
            // 
            btnFindNext.Location = new Point(232, 130);
            btnFindNext.Name = "btnFindNext";
            btnFindNext.Size = new Size(85, 27);
            btnFindNext.TabIndex = 5;
            btnFindNext.Text = "Find next";
            // 
            // btnClose
            // 
            btnClose.Location = new Point(323, 130);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(85, 27);
            btnClose.TabIndex = 6;
            btnClose.Text = "Close";
            // 
            // lblQuery
            // 
            lblQuery.AutoSize = true;
            lblQuery.Location = new Point(12, 15);
            lblQuery.Name = "lblQuery";
            lblQuery.Size = new Size(40, 20);
            lblQuery.TabIndex = 0;
            lblQuery.Text = "Find:";
            // 
            // FindDialog
            // 
            ClientSize = new Size(420, 170);
            Controls.Add(lblQuery);
            Controls.Add(txtQuery);
            Controls.Add(chkMatchCase);
            Controls.Add(chkWholeWord);
            Controls.Add(chkWrap);
            Controls.Add(btnFindNext);
            Controls.Add(btnClose);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FindDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Search";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
