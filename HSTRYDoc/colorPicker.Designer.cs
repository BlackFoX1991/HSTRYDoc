// colorPicker.Designer.cs
namespace HSTRYDoc
{
    partial class colorPicker
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(colorPicker));
            pnlRight = new Panel();
            lblHex = new Label();
            txtHex = new TextBox();
            grpRGBA = new GroupBox();
            lblA = new Label();
            lblB = new Label();
            lblG = new Label();
            lblR = new Label();
            numA = new NumericUpDown();
            numB = new NumericUpDown();
            numG = new NumericUpDown();
            numR = new NumericUpDown();
            pnlPreview = new Panel();
            lblPreview = new Label();
            btnOk = new Button();
            btnCancel = new Button();
            picWheel = new PictureBox();
            pnlRight.SuspendLayout();
            grpRGBA.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numA).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numB).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numG).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numR).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picWheel).BeginInit();
            SuspendLayout();
            // 
            // pnlRight
            // 
            pnlRight.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            pnlRight.Controls.Add(lblHex);
            pnlRight.Controls.Add(txtHex);
            pnlRight.Controls.Add(grpRGBA);
            pnlRight.Controls.Add(pnlPreview);
            pnlRight.Controls.Add(lblPreview);
            pnlRight.Location = new Point(313, 16);
            pnlRight.Margin = new Padding(3, 4, 3, 4);
            pnlRight.Name = "pnlRight";
            pnlRight.Size = new Size(233, 293);
            pnlRight.TabIndex = 1;
            // 
            // lblHex
            // 
            lblHex.AutoSize = true;
            lblHex.Location = new Point(7, 220);
            lblHex.Name = "lblHex";
            lblHex.Size = new Size(35, 20);
            lblHex.TabIndex = 3;
            lblHex.Text = "Hex";
            // 
            // txtHex
            // 
            txtHex.Location = new Point(7, 244);
            txtHex.Margin = new Padding(3, 4, 3, 4);
            txtHex.Name = "txtHex";
            txtHex.Size = new Size(219, 27);
            txtHex.TabIndex = 4;
            // 
            // grpRGBA
            // 
            grpRGBA.Controls.Add(lblA);
            grpRGBA.Controls.Add(lblB);
            grpRGBA.Controls.Add(lblG);
            grpRGBA.Controls.Add(lblR);
            grpRGBA.Controls.Add(numA);
            grpRGBA.Controls.Add(numB);
            grpRGBA.Controls.Add(numG);
            grpRGBA.Controls.Add(numR);
            grpRGBA.Location = new Point(7, 0);
            grpRGBA.Margin = new Padding(3, 4, 3, 4);
            grpRGBA.Name = "grpRGBA";
            grpRGBA.Padding = new Padding(3, 4, 3, 4);
            grpRGBA.Size = new Size(219, 171);
            grpRGBA.TabIndex = 0;
            grpRGBA.TabStop = false;
            grpRGBA.Text = "RGBA";
            // 
            // lblA
            // 
            lblA.AutoSize = true;
            lblA.Location = new Point(7, 131);
            lblA.Name = "lblA";
            lblA.Size = new Size(19, 20);
            lblA.TabIndex = 6;
            lblA.Text = "A";
            // 
            // lblB
            // 
            lblB.AutoSize = true;
            lblB.Location = new Point(7, 95);
            lblB.Name = "lblB";
            lblB.Size = new Size(18, 20);
            lblB.TabIndex = 4;
            lblB.Text = "B";
            // 
            // lblG
            // 
            lblG.AutoSize = true;
            lblG.Location = new Point(7, 59);
            lblG.Name = "lblG";
            lblG.Size = new Size(19, 20);
            lblG.TabIndex = 2;
            lblG.Text = "G";
            // 
            // lblR
            // 
            lblR.AutoSize = true;
            lblR.Location = new Point(7, 23);
            lblR.Name = "lblR";
            lblR.Size = new Size(18, 20);
            lblR.TabIndex = 0;
            lblR.Text = "R";
            // 
            // numA
            // 
            numA.Location = new Point(34, 128);
            numA.Margin = new Padding(3, 4, 3, 4);
            numA.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numA.Name = "numA";
            numA.Size = new Size(178, 27);
            numA.TabIndex = 7;
            // 
            // numB
            // 
            numB.Location = new Point(34, 92);
            numB.Margin = new Padding(3, 4, 3, 4);
            numB.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numB.Name = "numB";
            numB.Size = new Size(178, 27);
            numB.TabIndex = 5;
            // 
            // numG
            // 
            numG.Location = new Point(34, 56);
            numG.Margin = new Padding(3, 4, 3, 4);
            numG.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numG.Name = "numG";
            numG.Size = new Size(178, 27);
            numG.TabIndex = 3;
            // 
            // numR
            // 
            numR.Location = new Point(34, 20);
            numR.Margin = new Padding(3, 4, 3, 4);
            numR.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numR.Name = "numR";
            numR.Size = new Size(178, 27);
            numR.TabIndex = 1;
            // 
            // pnlPreview
            // 
            pnlPreview.BorderStyle = BorderStyle.FixedSingle;
            pnlPreview.Location = new Point(7, 196);
            pnlPreview.Margin = new Padding(3, 4, 3, 4);
            pnlPreview.Name = "pnlPreview";
            pnlPreview.Size = new Size(219, 15);
            pnlPreview.TabIndex = 2;
            // 
            // lblPreview
            // 
            lblPreview.AutoSize = true;
            lblPreview.Location = new Point(7, 172);
            lblPreview.Name = "lblPreview";
            lblPreview.Size = new Size(60, 20);
            lblPreview.TabIndex = 1;
            lblPreview.Text = "Preview";
            // 
            // btnOk
            // 
            btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOk.Location = new Point(368, 324);
            btnOk.Margin = new Padding(3, 4, 3, 4);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(86, 39);
            btnOk.TabIndex = 2;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(461, 324);
            btnCancel.Margin = new Padding(3, 4, 3, 4);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(86, 39);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // picWheel
            // 
            picWheel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            picWheel.BackColor = Color.Transparent;
            picWheel.Location = new Point(14, 16);
            picWheel.Margin = new Padding(3, 4, 3, 4);
            picWheel.Name = "picWheel";
            picWheel.Size = new Size(267, 272);
            picWheel.SizeMode = PictureBoxSizeMode.CenterImage;
            picWheel.TabIndex = 0;
            picWheel.TabStop = false;
            // 
            // colorPicker
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(560, 384);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(pnlRight);
            Controls.Add(picWheel);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "colorPicker";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Pick a Color...";
            TopMost = true;
            pnlRight.ResumeLayout(false);
            pnlRight.PerformLayout();
            grpRGBA.ResumeLayout(false);
            grpRGBA.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numA).EndInit();
            ((System.ComponentModel.ISupportInitialize)numB).EndInit();
            ((System.ComponentModel.ISupportInitialize)numG).EndInit();
            ((System.ComponentModel.ISupportInitialize)numR).EndInit();
            ((System.ComponentModel.ISupportInitialize)picWheel).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.PictureBox picWheel;
        private System.Windows.Forms.Panel pnlRight;
        private System.Windows.Forms.GroupBox grpRGBA;
        private System.Windows.Forms.Label lblA;
        private System.Windows.Forms.Label lblB;
        private System.Windows.Forms.Label lblG;
        private System.Windows.Forms.Label lblR;
        private System.Windows.Forms.NumericUpDown numA;
        private System.Windows.Forms.NumericUpDown numB;
        private System.Windows.Forms.NumericUpDown numG;
        private System.Windows.Forms.NumericUpDown numR;
        private System.Windows.Forms.Label lblHex;
        private System.Windows.Forms.TextBox txtHex;
        private System.Windows.Forms.Panel pnlPreview;
        private System.Windows.Forms.Label lblPreview;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}
