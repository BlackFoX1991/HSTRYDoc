namespace HSTRYDoc
{
    partial class exporterDiag
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(exporterDiag));
            panel1 = new Panel();
            panel4 = new Panel();
            grpOutput = new GroupBox();
            txtOutput = new TextBox();
            btnChoose = new Button();
            grpFileFormat = new GroupBox();
            radioTxt = new RadioButton();
            radioRtf = new RadioButton();
            radioPdf = new RadioButton();
            panel3 = new Panel();
            lvwBlocks = new ListView();
            colName = new ColumnHeader();
            colHash = new ColumnHeader();
            colSize = new ColumnHeader();
            colCreated = new ColumnHeader();
            colChanged = new ColumnHeader();
            label1 = new Label();
            panel2 = new Panel();
            prgExport = new ProgressBar();
            btnCancel = new Button();
            btnExport = new Button();
            panel1.SuspendLayout();
            panel4.SuspendLayout();
            grpOutput.SuspendLayout();
            grpFileFormat.SuspendLayout();
            panel3.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(panel4);
            panel1.Controls.Add(panel3);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(3, 4, 3, 4);
            panel1.Name = "panel1";
            panel1.Size = new Size(768, 416);
            panel1.TabIndex = 0;
            // 
            // panel4
            // 
            panel4.Controls.Add(grpOutput);
            panel4.Controls.Add(grpFileFormat);
            panel4.Dock = DockStyle.Fill;
            panel4.Location = new Point(386, 0);
            panel4.Margin = new Padding(3, 4, 3, 4);
            panel4.Name = "panel4";
            panel4.Padding = new Padding(17, 20, 17, 20);
            panel4.Size = new Size(382, 416);
            panel4.TabIndex = 1;
            // 
            // grpOutput
            // 
            grpOutput.Controls.Add(txtOutput);
            grpOutput.Controls.Add(btnChoose);
            grpOutput.Dock = DockStyle.Fill;
            grpOutput.Location = new Point(17, 271);
            grpOutput.Margin = new Padding(3, 4, 3, 4);
            grpOutput.Name = "grpOutput";
            grpOutput.Padding = new Padding(29, 33, 29, 33);
            grpOutput.Size = new Size(348, 125);
            grpOutput.TabIndex = 1;
            grpOutput.TabStop = false;
            grpOutput.Text = "Output";
            // 
            // txtOutput
            // 
            txtOutput.BorderStyle = BorderStyle.FixedSingle;
            txtOutput.Dock = DockStyle.Fill;
            txtOutput.Location = new Point(29, 53);
            txtOutput.Margin = new Padding(3, 4, 3, 4);
            txtOutput.Multiline = true;
            txtOutput.Name = "txtOutput";
            txtOutput.ReadOnly = true;
            txtOutput.Size = new Size(258, 39);
            txtOutput.TabIndex = 0;
            // 
            // btnChoose
            // 
            btnChoose.Dock = DockStyle.Right;
            btnChoose.FlatStyle = FlatStyle.Popup;
            btnChoose.Location = new Point(287, 53);
            btnChoose.Margin = new Padding(3, 4, 3, 4);
            btnChoose.Name = "btnChoose";
            btnChoose.Size = new Size(32, 39);
            btnChoose.TabIndex = 1;
            btnChoose.Text = "...";
            btnChoose.UseVisualStyleBackColor = true;
            // 
            // grpFileFormat
            // 
            grpFileFormat.Controls.Add(radioTxt);
            grpFileFormat.Controls.Add(radioRtf);
            grpFileFormat.Controls.Add(radioPdf);
            grpFileFormat.Dock = DockStyle.Top;
            grpFileFormat.Location = new Point(17, 20);
            grpFileFormat.Margin = new Padding(3, 4, 3, 4);
            grpFileFormat.Name = "grpFileFormat";
            grpFileFormat.Padding = new Padding(29, 33, 29, 33);
            grpFileFormat.Size = new Size(348, 251);
            grpFileFormat.TabIndex = 0;
            grpFileFormat.TabStop = false;
            grpFileFormat.Text = "File format...";
            // 
            // radioTxt
            // 
            radioTxt.Dock = DockStyle.Top;
            radioTxt.Location = new Point(29, 131);
            radioTxt.Margin = new Padding(3, 4, 3, 4);
            radioTxt.Name = "radioTxt";
            radioTxt.Size = new Size(290, 39);
            radioTxt.TabIndex = 2;
            radioTxt.TabStop = true;
            radioTxt.Text = "Textfile ( TXT )";
            radioTxt.UseVisualStyleBackColor = true;
            // 
            // radioRtf
            // 
            radioRtf.Dock = DockStyle.Top;
            radioRtf.Location = new Point(29, 92);
            radioRtf.Margin = new Padding(3, 4, 3, 4);
            radioRtf.Name = "radioRtf";
            radioRtf.Size = new Size(290, 39);
            radioRtf.TabIndex = 1;
            radioRtf.TabStop = true;
            radioRtf.Text = "Rich-Text-Format ( RTF )";
            radioRtf.UseVisualStyleBackColor = true;
            // 
            // radioPdf
            // 
            radioPdf.Dock = DockStyle.Top;
            radioPdf.Location = new Point(29, 53);
            radioPdf.Margin = new Padding(3, 4, 3, 4);
            radioPdf.Name = "radioPdf";
            radioPdf.Size = new Size(290, 39);
            radioPdf.TabIndex = 0;
            radioPdf.TabStop = true;
            radioPdf.Text = "Portable-Document-File ( PDF )";
            radioPdf.UseVisualStyleBackColor = true;
            // 
            // panel3
            // 
            panel3.Controls.Add(lvwBlocks);
            panel3.Controls.Add(label1);
            panel3.Dock = DockStyle.Left;
            panel3.Location = new Point(0, 0);
            panel3.Margin = new Padding(3, 4, 3, 4);
            panel3.Name = "panel3";
            panel3.Padding = new Padding(29, 33, 29, 33);
            panel3.Size = new Size(386, 416);
            panel3.TabIndex = 0;
            // 
            // lvwBlocks
            // 
            lvwBlocks.CheckBoxes = true;
            lvwBlocks.Columns.AddRange(new ColumnHeader[] { colName, colHash, colSize, colCreated, colChanged });
            lvwBlocks.Dock = DockStyle.Fill;
            lvwBlocks.FullRowSelect = true;
            lvwBlocks.GridLines = true;
            lvwBlocks.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lvwBlocks.Location = new Point(29, 112);
            lvwBlocks.Margin = new Padding(3, 4, 3, 4);
            lvwBlocks.MultiSelect = false;
            lvwBlocks.Name = "lvwBlocks";
            lvwBlocks.Size = new Size(328, 271);
            lvwBlocks.TabIndex = 1;
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
            // label1
            // 
            label1.Dock = DockStyle.Top;
            label1.Location = new Point(29, 33);
            label1.Name = "label1";
            label1.Size = new Size(328, 79);
            label1.TabIndex = 2;
            label1.Text = "Please select the Blocks you need to export. Please note, by choosing RTF or TXT the exporter will generate multiple Files.";
            // 
            // panel2
            // 
            panel2.Controls.Add(prgExport);
            panel2.Controls.Add(btnCancel);
            panel2.Controls.Add(btnExport);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 416);
            panel2.Margin = new Padding(3, 4, 3, 4);
            panel2.Name = "panel2";
            panel2.Padding = new Padding(17, 20, 17, 20);
            panel2.Size = new Size(768, 83);
            panel2.TabIndex = 1;
            // 
            // prgExport
            // 
            prgExport.Location = new Point(21, 24);
            prgExport.Margin = new Padding(3, 4, 3, 4);
            prgExport.Name = "prgExport";
            prgExport.Size = new Size(458, 35);
            prgExport.TabIndex = 2;
            prgExport.Visible = false;
            // 
            // btnCancel
            // 
            btnCancel.Dock = DockStyle.Right;
            btnCancel.Location = new Point(508, 20);
            btnCancel.Margin = new Padding(3, 4, 3, 4);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(122, 43);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnExport
            // 
            btnExport.Dock = DockStyle.Right;
            btnExport.Location = new Point(630, 20);
            btnExport.Margin = new Padding(3, 4, 3, 4);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(121, 43);
            btnExport.TabIndex = 0;
            btnExport.Text = "Continue...";
            btnExport.UseVisualStyleBackColor = true;
            // 
            // exporterDiag
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(768, 499);
            Controls.Add(panel1);
            Controls.Add(panel2);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 4, 3, 4);
            Name = "exporterDiag";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Export Blocks...";
            panel1.ResumeLayout(false);
            panel4.ResumeLayout(false);
            grpOutput.ResumeLayout(false);
            grpOutput.PerformLayout();
            grpFileFormat.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Panel panel4;
        private GroupBox grpFileFormat;
        private RadioButton radioRtf;
        private RadioButton radioPdf;
        private Panel panel3;
        private Panel panel2;
        private RadioButton radioTxt;
        private ListView lvwBlocks;
        private ColumnHeader colName;
        private ColumnHeader colHash;
        private ColumnHeader colSize;
        private ColumnHeader colCreated;
        private ColumnHeader colChanged;
        private Label label1;
        private GroupBox grpOutput;
        private TextBox txtOutput;
        private Button btnChoose;
        private Button btnCancel;
        private Button btnExport;
        private ProgressBar prgExport;
    }
}