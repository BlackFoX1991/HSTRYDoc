namespace HSTRYDoc
{
    partial class Chooser
    {
        private System.ComponentModel.IContainer components = null;

        private Button btnNew;
        private Button btnOpen;
        private Button btnExit;

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
            panel1 = new Panel();
            panel2 = new Panel();
            lvwRecent = new ListView();
            colFilename = new ColumnHeader();
            colFilepath = new ColumnHeader();
            colUsed = new ColumnHeader();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // btnNew
            // 
            btnNew.DialogResult = DialogResult.Yes;
            btnNew.Dock = DockStyle.Right;
            btnNew.Location = new Point(552, 10);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(100, 33);
            btnNew.TabIndex = 1;
            btnNew.Text = "New...";
            // 
            // btnOpen
            // 
            btnOpen.DialogResult = DialogResult.No;
            btnOpen.Dock = DockStyle.Right;
            btnOpen.Location = new Point(452, 10);
            btnOpen.Name = "btnOpen";
            btnOpen.Size = new Size(100, 33);
            btnOpen.TabIndex = 2;
            btnOpen.Text = "Open...";
            // 
            // btnExit
            // 
            btnExit.DialogResult = DialogResult.Cancel;
            btnExit.Dock = DockStyle.Right;
            btnExit.Location = new Point(352, 10);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(100, 33);
            btnExit.TabIndex = 3;
            btnExit.Text = "Exit";
            // 
            // panel1
            // 
            panel1.BackColor = Color.White;
            panel1.Controls.Add(btnExit);
            panel1.Controls.Add(btnOpen);
            panel1.Controls.Add(btnNew);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 377);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(10);
            panel1.Size = new Size(662, 53);
            panel1.TabIndex = 4;
            // 
            // panel2
            // 
            panel2.Controls.Add(lvwRecent);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Padding = new Padding(25);
            panel2.Size = new Size(662, 377);
            panel2.TabIndex = 5;
            // 
            // lvwRecent
            // 
            lvwRecent.Columns.AddRange(new ColumnHeader[] { colFilename, colFilepath, colUsed });
            lvwRecent.Dock = DockStyle.Fill;
            lvwRecent.FullRowSelect = true;
            lvwRecent.GridLines = true;
            lvwRecent.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lvwRecent.Location = new Point(25, 25);
            lvwRecent.Name = "lvwRecent";
            lvwRecent.Size = new Size(612, 327);
            lvwRecent.TabIndex = 1;
            lvwRecent.UseCompatibleStateImageBehavior = false;
            lvwRecent.View = View.Details;
            // 
            // colFilename
            // 
            colFilename.Text = "Filename";
            colFilename.Width = 150;
            // 
            // colFilepath
            // 
            colFilepath.Text = "Path";
            colFilepath.Width = 200;
            // 
            // colUsed
            // 
            colUsed.Text = "Used";
            colUsed.Width = 150;
            // 
            // Chooser
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(662, 430);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Chooser";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "HstryDocu Launch...";
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        private Panel panel1;
        private Panel panel2;
        private ListView lvwRecent;
        private ColumnHeader colFilename;
        private ColumnHeader colFilepath;
        private ColumnHeader colUsed;
    }
}
