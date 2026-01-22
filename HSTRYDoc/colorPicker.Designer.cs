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
            pnlRight.Location = new Point(274, 12);
            pnlRight.Name = "pnlRight";
            pnlRight.Size = new Size(204, 220);
            pnlRight.TabIndex = 1;
            // 
            // lblHex
            // 
            lblHex.AutoSize = true;
            lblHex.Location = new Point(6, 165);
            lblHex.Name = "lblHex";
            lblHex.Size = new Size(27, 15);
            lblHex.TabIndex = 3;
            lblHex.Text = "Hex";
            // 
            // txtHex
            // 
            txtHex.Location = new Point(6, 183);
            txtHex.Name = "txtHex";
            txtHex.Size = new Size(192, 23);
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
            grpRGBA.Location = new Point(6, 0);
            grpRGBA.Name = "grpRGBA";
            grpRGBA.Size = new Size(192, 128);
            grpRGBA.TabIndex = 0;
            grpRGBA.TabStop = false;
            grpRGBA.Text = "RGBA";
            // 
            // lblA
            // 
            lblA.AutoSize = true;
            lblA.Location = new Point(6, 98);
            lblA.Name = "lblA";
            lblA.Size = new Size(15, 15);
            lblA.TabIndex = 6;
            lblA.Text = "A";
            // 
            // lblB
            // 
            lblB.AutoSize = true;
            lblB.Location = new Point(6, 71);
            lblB.Name = "lblB";
            lblB.Size = new Size(14, 15);
            lblB.TabIndex = 4;
            lblB.Text = "B";
            // 
            // lblG
            // 
            lblG.AutoSize = true;
            lblG.Location = new Point(6, 44);
            lblG.Name = "lblG";
            lblG.Size = new Size(15, 15);
            lblG.TabIndex = 2;
            lblG.Text = "G";
            // 
            // lblR
            // 
            lblR.AutoSize = true;
            lblR.Location = new Point(6, 17);
            lblR.Name = "lblR";
            lblR.Size = new Size(14, 15);
            lblR.TabIndex = 0;
            lblR.Text = "R";
            // 
            // numA
            // 
            numA.Location = new Point(30, 96);
            numA.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numA.Name = "numA";
            numA.Size = new Size(156, 23);
            numA.TabIndex = 7;
            // 
            // numB
            // 
            numB.Location = new Point(30, 69);
            numB.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numB.Name = "numB";
            numB.Size = new Size(156, 23);
            numB.TabIndex = 5;
            // 
            // numG
            // 
            numG.Location = new Point(30, 42);
            numG.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numG.Name = "numG";
            numG.Size = new Size(156, 23);
            numG.TabIndex = 3;
            // 
            // numR
            // 
            numR.Location = new Point(30, 15);
            numR.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            numR.Name = "numR";
            numR.Size = new Size(156, 23);
            numR.TabIndex = 1;
            // 
            // pnlPreview
            // 
            pnlPreview.BorderStyle = BorderStyle.FixedSingle;
            pnlPreview.Location = new Point(6, 147);
            pnlPreview.Name = "pnlPreview";
            pnlPreview.Size = new Size(192, 12);
            pnlPreview.TabIndex = 2;
            // 
            // lblPreview
            // 
            lblPreview.AutoSize = true;
            lblPreview.Location = new Point(6, 129);
            lblPreview.Name = "lblPreview";
            lblPreview.Size = new Size(48, 15);
            lblPreview.TabIndex = 1;
            lblPreview.Text = "Preview";
            // 
            // btnOk
            // 
            btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOk.Location = new Point(322, 243);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 29);
            btnOk.TabIndex = 2;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(403, 243);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 29);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // picWheel
            // 
            picWheel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            picWheel.BackColor = Color.Transparent;
            picWheel.Location = new Point(12, 12);
            picWheel.Name = "picWheel";
            picWheel.Size = new Size(234, 204);
            picWheel.SizeMode = PictureBoxSizeMode.CenterImage;
            picWheel.TabIndex = 0;
            picWheel.TabStop = false;
            // 
            // colorPicker
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(490, 288);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(pnlRight);
            Controls.Add(picWheel);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
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
