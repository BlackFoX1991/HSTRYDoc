namespace HSTRYDoc
{
    partial class ContainerSearchResultsDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.ListView lvResults;
        private System.Windows.Forms.ColumnHeader colBlock;
        private System.Windows.Forms.ColumnHeader colPosition;
        private System.Windows.Forms.ColumnHeader colSnippet;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ContainerSearchResultsDialog));
            lvResults = new ListView();
            colBlock = new ColumnHeader();
            colPosition = new ColumnHeader();
            colSnippet = new ColumnHeader();
            btnOpen = new Button();
            btnClose = new Button();
            SuspendLayout();
            // 
            // lvResults
            // 
            lvResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lvResults.Columns.AddRange(new ColumnHeader[] { colBlock, colPosition, colSnippet });
            lvResults.FullRowSelect = true;
            lvResults.GridLines = true;
            lvResults.Location = new Point(12, 12);
            lvResults.Name = "lvResults";
            lvResults.Size = new Size(736, 360);
            lvResults.TabIndex = 0;
            lvResults.UseCompatibleStateImageBehavior = false;
            lvResults.View = View.Details;
            // 
            // colBlock
            // 
            colBlock.Text = "Block";
            colBlock.Width = 180;
            // 
            // colPosition
            // 
            colPosition.Text = "Pos";
            colPosition.Width = 70;
            // 
            // colSnippet
            // 
            colSnippet.Text = "Snippet";
            colSnippet.Width = 460;
            // 
            // btnOpen
            // 
            btnOpen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOpen.Location = new Point(582, 382);
            btnOpen.Name = "btnOpen";
            btnOpen.Size = new Size(80, 27);
            btnOpen.TabIndex = 1;
            btnOpen.Text = "Open";
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.Location = new Point(668, 382);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(80, 27);
            btnClose.TabIndex = 2;
            btnClose.Text = "Close";
            // 
            // ContainerSearchResultsDialog
            // 
            ClientSize = new Size(760, 420);
            Controls.Add(lvResults);
            Controls.Add(btnOpen);
            Controls.Add(btnClose);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ContainerSearchResultsDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Search results";
            ResumeLayout(false);
        }
    }
}
