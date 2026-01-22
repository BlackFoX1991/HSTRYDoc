namespace HSTRYDoc
{
    partial class ContainerSearchResultsDialog
    {
        private System.ComponentModel.IContainer components = null;

        private ListView lvResults;
        private ColumnHeader colBlock;
        private ColumnHeader colSnippet;
        private Button btnOpen;
        private Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            lvResults = new ListView();
            colBlock = new ColumnHeader();
            colSnippet = new ColumnHeader();
            btnOpen = new Button();
            btnClose = new Button();

            SuspendLayout();

            Text = "Search results";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(760, 420);
            StartPosition = FormStartPosition.CenterParent;

            lvResults.Location = new Point(12, 12);
            lvResults.Size = new Size(736, 360);
            lvResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lvResults.View = View.Details;
            lvResults.FullRowSelect = true;
            lvResults.GridLines = true;

            colBlock.Text = "Block";
            colBlock.Width = 180;
            colSnippet.Text = "Snippet";
            colSnippet.Width = 530;

            lvResults.Columns.AddRange(new[] { colBlock, colSnippet });

            btnOpen.Location = new Point(582, 382);
            btnOpen.Size = new Size(80, 27);
            btnOpen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOpen.Text = "Open";

            btnClose.Location = new Point(668, 382);
            btnClose.Size = new Size(80, 27);
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.Text = "Close";

            Controls.Add(lvResults);
            Controls.Add(btnOpen);
            Controls.Add(btnClose);

            ResumeLayout(false);
        }
    }
}
