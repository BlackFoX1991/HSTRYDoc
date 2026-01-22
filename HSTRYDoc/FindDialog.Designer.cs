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
            components = new System.ComponentModel.Container();

            txtQuery = new TextBox();
            chkMatchCase = new CheckBox();
            chkWholeWord = new CheckBox();
            chkWrap = new CheckBox();
            btnFindNext = new Button();
            btnClose = new Button();
            lblQuery = new Label();

            SuspendLayout();

            Text = "Search";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(420, 170);

            lblQuery.AutoSize = true;
            lblQuery.Location = new Point(12, 15);
            lblQuery.Text = "Find:";

            txtQuery.Location = new Point(60, 12);
            txtQuery.Size = new Size(348, 23);
            txtQuery.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            chkMatchCase.AutoSize = true;
            chkMatchCase.Location = new Point(60, 48);
            chkMatchCase.Text = "Match case";

            chkWholeWord.AutoSize = true;
            chkWholeWord.Location = new Point(60, 74);
            chkWholeWord.Text = "Whole word";

            chkWrap.AutoSize = true;
            chkWrap.Location = new Point(60, 100);
            chkWrap.Text = "Wrap";
            chkWrap.Checked = true;

            btnFindNext.Location = new Point(232, 130);
            btnFindNext.Size = new Size(85, 27);
            btnFindNext.Text = "Find next";

            btnClose.Location = new Point(323, 130);
            btnClose.Size = new Size(85, 27);
            btnClose.Text = "Close";

            Controls.Add(lblQuery);
            Controls.Add(txtQuery);
            Controls.Add(chkMatchCase);
            Controls.Add(chkWholeWord);
            Controls.Add(chkWrap);
            Controls.Add(btnFindNext);
            Controls.Add(btnClose);

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
