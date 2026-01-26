namespace HSTRYDoc
{
    partial class MaskedData
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MaskedData));
            splitContainer1 = new SplitContainer();
            lblInfo = new Label();
            comboDataMask = new ComboBox();
            lvwData = new ListView();
            ctxData = new ContextMenuStrip(components);
            addDataToolStripMenuItem = new ToolStripMenuItem();
            removeDaToolStripMenuItem = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ctxData.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(lblInfo);
            splitContainer1.Panel1.Controls.Add(comboDataMask);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(lvwData);
            splitContainer1.Size = new Size(713, 469);
            splitContainer1.SplitterDistance = 93;
            splitContainer1.TabIndex = 0;
            // 
            // lblInfo
            // 
            lblInfo.Dock = DockStyle.Fill;
            lblInfo.Location = new Point(0, 0);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(713, 70);
            lblInfo.TabIndex = 0;
            lblInfo.Text = "Choose your Data-Defintion which you pre-defined in your Blocks...";
            lblInfo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // comboDataMask
            // 
            comboDataMask.Dock = DockStyle.Bottom;
            comboDataMask.FormattingEnabled = true;
            comboDataMask.Location = new Point(0, 70);
            comboDataMask.Name = "comboDataMask";
            comboDataMask.Size = new Size(713, 23);
            comboDataMask.TabIndex = 1;
            // 
            // lvwData
            // 
            lvwData.ContextMenuStrip = ctxData;
            lvwData.Dock = DockStyle.Fill;
            lvwData.GridLines = true;
            lvwData.LabelEdit = true;
            lvwData.Location = new Point(0, 0);
            lvwData.Name = "lvwData";
            lvwData.Size = new Size(713, 372);
            lvwData.TabIndex = 0;
            lvwData.UseCompatibleStateImageBehavior = false;
            lvwData.View = View.Details;
            // 
            // ctxData
            // 
            ctxData.Items.AddRange(new ToolStripItem[] { addDataToolStripMenuItem, removeDaToolStripMenuItem });
            ctxData.Name = "ctxData";
            ctxData.ShowImageMargin = false;
            ctxData.Size = new Size(129, 48);
            // 
            // addDataToolStripMenuItem
            // 
            addDataToolStripMenuItem.Name = "addDataToolStripMenuItem";
            addDataToolStripMenuItem.Size = new Size(128, 22);
            addDataToolStripMenuItem.Text = "Add Data...";
            // 
            // removeDaToolStripMenuItem
            // 
            removeDaToolStripMenuItem.Name = "removeDaToolStripMenuItem";
            removeDaToolStripMenuItem.Size = new Size(128, 22);
            removeDaToolStripMenuItem.Text = "Remove Data...";
            // 
            // MaskedData
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(713, 469);
            Controls.Add(splitContainer1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MaskedData";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Defined Data Management...";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ctxData.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private Label lblInfo;
        private ComboBox comboDataMask;
        private ListView lvwData;
        private ContextMenuStrip ctxData;
        private ToolStripMenuItem addDataToolStripMenuItem;
        private ToolStripMenuItem removeDaToolStripMenuItem;
    }
}