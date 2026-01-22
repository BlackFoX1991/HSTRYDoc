namespace HSTRYDoc
{
    partial class hsMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(hsMain));
            mainToolstrip = new ToolStrip();
            newBlockToolStripButton = new ToolStripButton();
            openContainerToolStripButton = new ToolStripButton();
            saveContainerToolStripButton = new ToolStripButton();
            toolStripSeparator = new ToolStripSeparator();
            hilfeToolStripButton = new ToolStripButton();
            statusStrip1 = new StatusStrip();
            ContainerSizeLabel = new ToolStripStatusLabel();
            splitContainer1 = new SplitContainer();
            lvwBlocks = new ListView();
            colName = new ColumnHeader();
            colHash = new ColumnHeader();
            colSize = new ColumnHeader();
            colCreated = new ColumnHeader();
            colChanged = new ColumnHeader();
            ctxBlocks = new ContextMenuStrip(components);
            newBlockToolStripMenuItem = new ToolStripMenuItem();
            renameBlockToolStripMenuItem = new ToolStripMenuItem();
            rtfMainText = new RichTextBox();
            ctxRtf = new ContextMenuStrip(components);
            copyToolStripMenuItem1 = new ToolStripMenuItem();
            pasteToolStripMenuItem1 = new ToolStripMenuItem();
            cutToolStripMenuItem1 = new ToolStripMenuItem();
            selectAllToolStripMenuItem1 = new ToolStripMenuItem();
            toolStripMenuItem4 = new ToolStripSeparator();
            boldToolStripMenuItem = new ToolStripMenuItem();
            italicToolStripMenuItem = new ToolStripMenuItem();
            underlineToolStripMenuItem = new ToolStripMenuItem();
            strikeToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem5 = new ToolStripSeparator();
            forecolorToolStripMenuItem = new ToolStripMenuItem();
            textBackgroundcolorToolStripMenuItem = new ToolStripMenuItem();
            toolStripRtf = new ToolStrip();
            boldToolButton = new ToolStripButton();
            ItalicToolButton = new ToolStripButton();
            UnderlineToolButton = new ToolStripButton();
            StrikeTroughToolButton = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            FontSizeComboBox = new ToolStripComboBox();
            toolStripSeparator3 = new ToolStripSeparator();
            foreColorToolButton = new ToolStripButton();
            backgroundColorToolButton = new ToolStripButton();
            toolStripSeparator4 = new ToolStripSeparator();
            toolButtonCopy = new ToolStripButton();
            toolButtonPaste = new ToolStripButton();
            toolButtonCut = new ToolStripButton();
            toolButtonSelectAll = new ToolStripButton();
            mainMenu = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            newToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            saveContainerToolStripMenuItem = new ToolStripMenuItem();
            saveContainerAsToolStripMenuItem = new ToolStripMenuItem();
            openContainerToolStripMenuItem = new ToolStripMenuItem();
            exportBlockToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripSeparator();
            closeToolStripMenuItem = new ToolStripMenuItem();
            toolsToolStripMenuItem = new ToolStripMenuItem();
            searchInBlockToolStripMenuItem = new ToolStripMenuItem();
            searchInContainerToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            mainToolstrip.SuspendLayout();
            statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ctxBlocks.SuspendLayout();
            ctxRtf.SuspendLayout();
            toolStripRtf.SuspendLayout();
            mainMenu.SuspendLayout();
            SuspendLayout();
            // 
            // mainToolstrip
            // 
            mainToolstrip.Items.AddRange(new ToolStripItem[] { newBlockToolStripButton, openContainerToolStripButton, saveContainerToolStripButton, toolStripSeparator, hilfeToolStripButton });
            mainToolstrip.Location = new Point(0, 24);
            mainToolstrip.Name = "mainToolstrip";
            mainToolstrip.Size = new Size(1022, 25);
            mainToolstrip.TabIndex = 1;
            mainToolstrip.Text = "toolStrip1";
            // 
            // newBlockToolStripButton
            // 
            newBlockToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            newBlockToolStripButton.Image = (Image)resources.GetObject("newBlockToolStripButton.Image");
            newBlockToolStripButton.ImageTransparentColor = Color.Magenta;
            newBlockToolStripButton.Name = "newBlockToolStripButton";
            newBlockToolStripButton.Size = new Size(23, 22);
            newBlockToolStripButton.Text = "&Neu";
            // 
            // openContainerToolStripButton
            // 
            openContainerToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            openContainerToolStripButton.Image = (Image)resources.GetObject("openContainerToolStripButton.Image");
            openContainerToolStripButton.ImageTransparentColor = Color.Magenta;
            openContainerToolStripButton.Name = "openContainerToolStripButton";
            openContainerToolStripButton.Size = new Size(23, 22);
            openContainerToolStripButton.Text = "Ö&ffnen";
            // 
            // saveContainerToolStripButton
            // 
            saveContainerToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            saveContainerToolStripButton.Image = (Image)resources.GetObject("saveContainerToolStripButton.Image");
            saveContainerToolStripButton.ImageTransparentColor = Color.Magenta;
            saveContainerToolStripButton.Name = "saveContainerToolStripButton";
            saveContainerToolStripButton.Size = new Size(23, 22);
            saveContainerToolStripButton.Text = "&Speichern";
            // 
            // toolStripSeparator
            // 
            toolStripSeparator.Name = "toolStripSeparator";
            toolStripSeparator.Size = new Size(6, 25);
            // 
            // hilfeToolStripButton
            // 
            hilfeToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            hilfeToolStripButton.Image = (Image)resources.GetObject("hilfeToolStripButton.Image");
            hilfeToolStripButton.ImageTransparentColor = Color.Magenta;
            hilfeToolStripButton.Name = "hilfeToolStripButton";
            hilfeToolStripButton.Size = new Size(23, 22);
            hilfeToolStripButton.Text = "Hi&lfe";
            hilfeToolStripButton.Click += hilfeToolStripButton_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { ContainerSizeLabel });
            statusStrip1.Location = new Point(0, 638);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1022, 22);
            statusStrip1.TabIndex = 2;
            statusStrip1.Text = "statusStrip1";
            // 
            // ContainerSizeLabel
            // 
            ContainerSizeLabel.Name = "ContainerSizeLabel";
            ContainerSizeLabel.Size = new Size(100, 17);
            ContainerSizeLabel.Text = "<Container_Size>";
            ContainerSizeLabel.Visible = false;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 49);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(lvwBlocks);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(rtfMainText);
            splitContainer1.Panel2.Controls.Add(toolStripRtf);
            splitContainer1.Size = new Size(1022, 589);
            splitContainer1.SplitterDistance = 404;
            splitContainer1.TabIndex = 3;
            // 
            // lvwBlocks
            // 
            lvwBlocks.Columns.AddRange(new ColumnHeader[] { colName, colHash, colSize, colCreated, colChanged });
            lvwBlocks.ContextMenuStrip = ctxBlocks;
            lvwBlocks.Dock = DockStyle.Fill;
            lvwBlocks.FullRowSelect = true;
            lvwBlocks.GridLines = true;
            lvwBlocks.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lvwBlocks.Location = new Point(0, 0);
            lvwBlocks.MultiSelect = false;
            lvwBlocks.Name = "lvwBlocks";
            lvwBlocks.Size = new Size(404, 589);
            lvwBlocks.TabIndex = 0;
            lvwBlocks.UseCompatibleStateImageBehavior = false;
            lvwBlocks.View = View.Details;
            // 
            // colName
            // 
            colName.Text = "Blockname";
            colName.Width = 150;
            // 
            // colHash
            // 
            colHash.Text = "Hash";
            // 
            // colSize
            // 
            colSize.Text = "Size";
            // 
            // colCreated
            // 
            colCreated.Text = "Created";
            // 
            // colChanged
            // 
            colChanged.Text = "Changed";
            // 
            // ctxBlocks
            // 
            ctxBlocks.Items.AddRange(new ToolStripItem[] { newBlockToolStripMenuItem, renameBlockToolStripMenuItem });
            ctxBlocks.Name = "ctxBlocks";
            ctxBlocks.Size = new Size(159, 48);
            // 
            // newBlockToolStripMenuItem
            // 
            newBlockToolStripMenuItem.Image = (Image)resources.GetObject("newBlockToolStripMenuItem.Image");
            newBlockToolStripMenuItem.Name = "newBlockToolStripMenuItem";
            newBlockToolStripMenuItem.Size = new Size(158, 22);
            newBlockToolStripMenuItem.Text = "New Block...";
            // 
            // renameBlockToolStripMenuItem
            // 
            renameBlockToolStripMenuItem.Image = (Image)resources.GetObject("renameBlockToolStripMenuItem.Image");
            renameBlockToolStripMenuItem.Name = "renameBlockToolStripMenuItem";
            renameBlockToolStripMenuItem.Size = new Size(158, 22);
            renameBlockToolStripMenuItem.Text = "Rename Block...";
            // 
            // rtfMainText
            // 
            rtfMainText.BorderStyle = BorderStyle.None;
            rtfMainText.ContextMenuStrip = ctxRtf;
            rtfMainText.Dock = DockStyle.Fill;
            rtfMainText.Location = new Point(0, 25);
            rtfMainText.Name = "rtfMainText";
            rtfMainText.Size = new Size(614, 564);
            rtfMainText.TabIndex = 0;
            rtfMainText.Text = "";
            // 
            // ctxRtf
            // 
            ctxRtf.Items.AddRange(new ToolStripItem[] { copyToolStripMenuItem1, pasteToolStripMenuItem1, cutToolStripMenuItem1, selectAllToolStripMenuItem1, toolStripMenuItem4, boldToolStripMenuItem, italicToolStripMenuItem, underlineToolStripMenuItem, strikeToolStripMenuItem, toolStripMenuItem5, forecolorToolStripMenuItem, textBackgroundcolorToolStripMenuItem });
            ctxRtf.Name = "ctxRtf";
            ctxRtf.Size = new Size(192, 236);
            // 
            // copyToolStripMenuItem1
            // 
            copyToolStripMenuItem1.Image = (Image)resources.GetObject("copyToolStripMenuItem1.Image");
            copyToolStripMenuItem1.Name = "copyToolStripMenuItem1";
            copyToolStripMenuItem1.Size = new Size(191, 22);
            copyToolStripMenuItem1.Text = "Copy";
            // 
            // pasteToolStripMenuItem1
            // 
            pasteToolStripMenuItem1.Image = (Image)resources.GetObject("pasteToolStripMenuItem1.Image");
            pasteToolStripMenuItem1.Name = "pasteToolStripMenuItem1";
            pasteToolStripMenuItem1.Size = new Size(191, 22);
            pasteToolStripMenuItem1.Text = "Paste";
            // 
            // cutToolStripMenuItem1
            // 
            cutToolStripMenuItem1.Image = (Image)resources.GetObject("cutToolStripMenuItem1.Image");
            cutToolStripMenuItem1.Name = "cutToolStripMenuItem1";
            cutToolStripMenuItem1.Size = new Size(191, 22);
            cutToolStripMenuItem1.Text = "Cut";
            // 
            // selectAllToolStripMenuItem1
            // 
            selectAllToolStripMenuItem1.Image = (Image)resources.GetObject("selectAllToolStripMenuItem1.Image");
            selectAllToolStripMenuItem1.Name = "selectAllToolStripMenuItem1";
            selectAllToolStripMenuItem1.Size = new Size(191, 22);
            selectAllToolStripMenuItem1.Text = "Select All";
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            toolStripMenuItem4.Size = new Size(188, 6);
            // 
            // boldToolStripMenuItem
            // 
            boldToolStripMenuItem.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            boldToolStripMenuItem.Image = (Image)resources.GetObject("boldToolStripMenuItem.Image");
            boldToolStripMenuItem.Name = "boldToolStripMenuItem";
            boldToolStripMenuItem.Size = new Size(191, 22);
            boldToolStripMenuItem.Text = "Bold";
            // 
            // italicToolStripMenuItem
            // 
            italicToolStripMenuItem.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point, 0);
            italicToolStripMenuItem.Image = (Image)resources.GetObject("italicToolStripMenuItem.Image");
            italicToolStripMenuItem.Name = "italicToolStripMenuItem";
            italicToolStripMenuItem.Size = new Size(191, 22);
            italicToolStripMenuItem.Text = "Italic";
            // 
            // underlineToolStripMenuItem
            // 
            underlineToolStripMenuItem.Font = new Font("Segoe UI", 9F, FontStyle.Underline, GraphicsUnit.Point, 0);
            underlineToolStripMenuItem.Image = (Image)resources.GetObject("underlineToolStripMenuItem.Image");
            underlineToolStripMenuItem.Name = "underlineToolStripMenuItem";
            underlineToolStripMenuItem.Size = new Size(191, 22);
            underlineToolStripMenuItem.Text = "Underline";
            // 
            // strikeToolStripMenuItem
            // 
            strikeToolStripMenuItem.Font = new Font("Segoe UI", 9F, FontStyle.Strikeout, GraphicsUnit.Point, 0);
            strikeToolStripMenuItem.Image = (Image)resources.GetObject("strikeToolStripMenuItem.Image");
            strikeToolStripMenuItem.Name = "strikeToolStripMenuItem";
            strikeToolStripMenuItem.Size = new Size(191, 22);
            strikeToolStripMenuItem.Text = "Strike";
            // 
            // toolStripMenuItem5
            // 
            toolStripMenuItem5.Name = "toolStripMenuItem5";
            toolStripMenuItem5.Size = new Size(188, 6);
            // 
            // forecolorToolStripMenuItem
            // 
            forecolorToolStripMenuItem.Image = (Image)resources.GetObject("forecolorToolStripMenuItem.Image");
            forecolorToolStripMenuItem.Name = "forecolorToolStripMenuItem";
            forecolorToolStripMenuItem.Size = new Size(191, 22);
            forecolorToolStripMenuItem.Text = "Forecolor";
            // 
            // textBackgroundcolorToolStripMenuItem
            // 
            textBackgroundcolorToolStripMenuItem.Image = (Image)resources.GetObject("textBackgroundcolorToolStripMenuItem.Image");
            textBackgroundcolorToolStripMenuItem.Name = "textBackgroundcolorToolStripMenuItem";
            textBackgroundcolorToolStripMenuItem.Size = new Size(191, 22);
            textBackgroundcolorToolStripMenuItem.Text = "Text-Backgroundcolor";
            // 
            // toolStripRtf
            // 
            toolStripRtf.Items.AddRange(new ToolStripItem[] { boldToolButton, ItalicToolButton, UnderlineToolButton, StrikeTroughToolButton, toolStripSeparator2, FontSizeComboBox, toolStripSeparator3, foreColorToolButton, backgroundColorToolButton, toolStripSeparator4, toolButtonCopy, toolButtonPaste, toolButtonCut, toolButtonSelectAll });
            toolStripRtf.Location = new Point(0, 0);
            toolStripRtf.Name = "toolStripRtf";
            toolStripRtf.Size = new Size(614, 25);
            toolStripRtf.TabIndex = 1;
            toolStripRtf.Text = "toolStrip1";
            // 
            // boldToolButton
            // 
            boldToolButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            boldToolButton.Image = (Image)resources.GetObject("boldToolButton.Image");
            boldToolButton.ImageTransparentColor = Color.Magenta;
            boldToolButton.Name = "boldToolButton";
            boldToolButton.Size = new Size(23, 22);
            boldToolButton.Text = "Bold";
            // 
            // ItalicToolButton
            // 
            ItalicToolButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            ItalicToolButton.Image = (Image)resources.GetObject("ItalicToolButton.Image");
            ItalicToolButton.ImageTransparentColor = Color.Magenta;
            ItalicToolButton.Name = "ItalicToolButton";
            ItalicToolButton.Size = new Size(23, 22);
            ItalicToolButton.Text = "Italic";
            // 
            // UnderlineToolButton
            // 
            UnderlineToolButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            UnderlineToolButton.Image = (Image)resources.GetObject("UnderlineToolButton.Image");
            UnderlineToolButton.ImageTransparentColor = Color.Magenta;
            UnderlineToolButton.Name = "UnderlineToolButton";
            UnderlineToolButton.Size = new Size(23, 22);
            UnderlineToolButton.Text = "Underline";
            // 
            // StrikeTroughToolButton
            // 
            StrikeTroughToolButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            StrikeTroughToolButton.Image = (Image)resources.GetObject("StrikeTroughToolButton.Image");
            StrikeTroughToolButton.ImageTransparentColor = Color.Magenta;
            StrikeTroughToolButton.Name = "StrikeTroughToolButton";
            StrikeTroughToolButton.Size = new Size(23, 22);
            StrikeTroughToolButton.Text = "Striketrough";
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 25);
            // 
            // FontSizeComboBox
            // 
            FontSizeComboBox.Items.AddRange(new object[] { "8", "9", "10", "12", "14", "16", "18", "20", "22", "24", "26", "28", "36", "48", "72" });
            FontSizeComboBox.Name = "FontSizeComboBox";
            FontSizeComboBox.Size = new Size(75, 25);
            FontSizeComboBox.Text = "8";
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 25);
            // 
            // foreColorToolButton
            // 
            foreColorToolButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            foreColorToolButton.Image = (Image)resources.GetObject("foreColorToolButton.Image");
            foreColorToolButton.ImageTransparentColor = Color.Magenta;
            foreColorToolButton.Name = "foreColorToolButton";
            foreColorToolButton.Size = new Size(23, 22);
            foreColorToolButton.Text = "Forecolor";
            // 
            // backgroundColorToolButton
            // 
            backgroundColorToolButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            backgroundColorToolButton.Image = (Image)resources.GetObject("backgroundColorToolButton.Image");
            backgroundColorToolButton.ImageTransparentColor = Color.Magenta;
            backgroundColorToolButton.Name = "backgroundColorToolButton";
            backgroundColorToolButton.Size = new Size(23, 22);
            backgroundColorToolButton.Text = "Text-Backcolor";
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(6, 25);
            // 
            // toolButtonCopy
            // 
            toolButtonCopy.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolButtonCopy.Image = (Image)resources.GetObject("toolButtonCopy.Image");
            toolButtonCopy.ImageTransparentColor = Color.Magenta;
            toolButtonCopy.Name = "toolButtonCopy";
            toolButtonCopy.Size = new Size(23, 22);
            toolButtonCopy.Text = "Copy";
            // 
            // toolButtonPaste
            // 
            toolButtonPaste.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolButtonPaste.Image = (Image)resources.GetObject("toolButtonPaste.Image");
            toolButtonPaste.ImageTransparentColor = Color.Magenta;
            toolButtonPaste.Name = "toolButtonPaste";
            toolButtonPaste.Size = new Size(23, 22);
            toolButtonPaste.Text = "Paste";
            // 
            // toolButtonCut
            // 
            toolButtonCut.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolButtonCut.Image = (Image)resources.GetObject("toolButtonCut.Image");
            toolButtonCut.ImageTransparentColor = Color.Magenta;
            toolButtonCut.Name = "toolButtonCut";
            toolButtonCut.Size = new Size(23, 22);
            toolButtonCut.Text = "Cut";
            // 
            // toolButtonSelectAll
            // 
            toolButtonSelectAll.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolButtonSelectAll.Image = (Image)resources.GetObject("toolButtonSelectAll.Image");
            toolButtonSelectAll.ImageTransparentColor = Color.Magenta;
            toolButtonSelectAll.Name = "toolButtonSelectAll";
            toolButtonSelectAll.Size = new Size(23, 22);
            toolButtonSelectAll.Text = "Select All";
            // 
            // mainMenu
            // 
            mainMenu.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, toolsToolStripMenuItem, helpToolStripMenuItem });
            mainMenu.Location = new Point(0, 0);
            mainMenu.Name = "mainMenu";
            mainMenu.Size = new Size(1022, 24);
            mainMenu.TabIndex = 4;
            mainMenu.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newToolStripMenuItem, toolStripMenuItem1, saveContainerToolStripMenuItem, saveContainerAsToolStripMenuItem, openContainerToolStripMenuItem, exportBlockToolStripMenuItem, toolStripMenuItem2, closeToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.Image = (Image)resources.GetObject("newToolStripMenuItem.Image");
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.Size = new Size(178, 22);
            newToolStripMenuItem.Text = "New Block...";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(175, 6);
            // 
            // saveContainerToolStripMenuItem
            // 
            saveContainerToolStripMenuItem.Image = (Image)resources.GetObject("saveContainerToolStripMenuItem.Image");
            saveContainerToolStripMenuItem.Name = "saveContainerToolStripMenuItem";
            saveContainerToolStripMenuItem.Size = new Size(178, 22);
            saveContainerToolStripMenuItem.Text = "Save Container...";
            // 
            // saveContainerAsToolStripMenuItem
            // 
            saveContainerAsToolStripMenuItem.Name = "saveContainerAsToolStripMenuItem";
            saveContainerAsToolStripMenuItem.Size = new Size(178, 22);
            saveContainerAsToolStripMenuItem.Text = "Save Container As...";
            // 
            // openContainerToolStripMenuItem
            // 
            openContainerToolStripMenuItem.Image = (Image)resources.GetObject("openContainerToolStripMenuItem.Image");
            openContainerToolStripMenuItem.Name = "openContainerToolStripMenuItem";
            openContainerToolStripMenuItem.Size = new Size(178, 22);
            openContainerToolStripMenuItem.Text = "Open Container...";
            // 
            // exportBlockToolStripMenuItem
            // 
            exportBlockToolStripMenuItem.Image = (Image)resources.GetObject("exportBlockToolStripMenuItem.Image");
            exportBlockToolStripMenuItem.Name = "exportBlockToolStripMenuItem";
            exportBlockToolStripMenuItem.Size = new Size(178, 22);
            exportBlockToolStripMenuItem.Text = "Export Blocks...";
            exportBlockToolStripMenuItem.Click += exportBlockToolStripMenuItem_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(175, 6);
            // 
            // closeToolStripMenuItem
            // 
            closeToolStripMenuItem.Image = (Image)resources.GetObject("closeToolStripMenuItem.Image");
            closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            closeToolStripMenuItem.Size = new Size(178, 22);
            closeToolStripMenuItem.Text = "Exit";
            closeToolStripMenuItem.Click += closeToolStripMenuItem_Click;
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { searchInBlockToolStripMenuItem, searchInContainerToolStripMenuItem });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new Size(47, 20);
            toolsToolStripMenuItem.Text = "Tools";
            // 
            // searchInBlockToolStripMenuItem
            // 
            searchInBlockToolStripMenuItem.Image = (Image)resources.GetObject("searchInBlockToolStripMenuItem.Image");
            searchInBlockToolStripMenuItem.Name = "searchInBlockToolStripMenuItem";
            searchInBlockToolStripMenuItem.Size = new Size(186, 22);
            searchInBlockToolStripMenuItem.Text = "Search in Block...";
            // 
            // searchInContainerToolStripMenuItem
            // 
            searchInContainerToolStripMenuItem.Image = (Image)resources.GetObject("searchInContainerToolStripMenuItem.Image");
            searchInContainerToolStripMenuItem.Name = "searchInContainerToolStripMenuItem";
            searchInContainerToolStripMenuItem.Size = new Size(186, 22);
            searchInContainerToolStripMenuItem.Text = "Search in Container...";
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Image = (Image)resources.GetObject("aboutToolStripMenuItem.Image");
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(180, 22);
            aboutToolStripMenuItem.Text = "About...";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // hsMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1022, 660);
            Controls.Add(splitContainer1);
            Controls.Add(statusStrip1);
            Controls.Add(mainToolstrip);
            Controls.Add(mainMenu);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "hsMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "HstryDocu";
            Load += hsMain_Load;
            mainToolstrip.ResumeLayout(false);
            mainToolstrip.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ctxBlocks.ResumeLayout(false);
            ctxRtf.ResumeLayout(false);
            toolStripRtf.ResumeLayout(false);
            toolStripRtf.PerformLayout();
            mainMenu.ResumeLayout(false);
            mainMenu.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ToolStrip mainToolstrip;
        private StatusStrip statusStrip1;
        private SplitContainer splitContainer1;
        private ListView lvwBlocks;
        private ColumnHeader colName;
        private ColumnHeader colHash;
        private ColumnHeader colSize;
        private ColumnHeader colCreated;
        private ColumnHeader colChanged;
        private RichTextBox rtfMainText;
        private MenuStrip mainMenu;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem newToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem saveContainerToolStripMenuItem;
        private ToolStripMenuItem saveContainerAsToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem2;
        private ToolStripMenuItem closeToolStripMenuItem;
        private ToolStripButton newBlockToolStripButton;
        private ToolStripButton openContainerToolStripButton;
        private ToolStripButton saveContainerToolStripButton;
        private ToolStripSeparator toolStripSeparator;
        private ToolStripButton hilfeToolStripButton;
        private ToolStripMenuItem openContainerToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem searchInBlockToolStripMenuItem;
        private ToolStripMenuItem searchInContainerToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripStatusLabel ContainerSizeLabel;
        private ToolStripMenuItem exportBlockToolStripMenuItem;
        private ContextMenuStrip ctxRtf;
        private ToolStripMenuItem copyToolStripMenuItem1;
        private ToolStripMenuItem pasteToolStripMenuItem1;
        private ToolStripMenuItem cutToolStripMenuItem1;
        private ToolStripMenuItem selectAllToolStripMenuItem1;
        private ToolStripSeparator toolStripMenuItem4;
        private ToolStripMenuItem boldToolStripMenuItem;
        private ToolStripMenuItem italicToolStripMenuItem;
        private ToolStripMenuItem underlineToolStripMenuItem;
        private ToolStrip toolStripRtf;
        private ToolStripButton boldToolButton;
        private ToolStripButton ItalicToolButton;
        private ToolStripButton UnderlineToolButton;
        private ToolStripButton StrikeTroughToolButton;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem strikeToolStripMenuItem;
        private ToolStripComboBox FontSizeComboBox;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripButton foreColorToolButton;
        private ToolStripButton backgroundColorToolButton;
        private ToolStripSeparator toolStripMenuItem5;
        private ToolStripMenuItem forecolorToolStripMenuItem;
        private ToolStripMenuItem textBackgroundcolorToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripButton toolButtonCopy;
        private ToolStripButton toolButtonPaste;
        private ToolStripButton toolButtonCut;
        private ToolStripButton toolButtonSelectAll;
        private ContextMenuStrip ctxBlocks;
        private ToolStripMenuItem newBlockToolStripMenuItem;
        private ToolStripMenuItem renameBlockToolStripMenuItem;
    }
}
