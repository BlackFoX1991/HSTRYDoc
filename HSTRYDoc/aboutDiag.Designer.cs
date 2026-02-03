namespace HSTRYDoc
{
    partial class aboutDiag
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(aboutDiag));
            linkLabel1 = new LinkLabel();
            label1 = new Label();
            pictureBox1 = new PictureBox();
            label2 = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // linkLabel1
            // 
            linkLabel1.ActiveLinkColor = Color.FromArgb(128, 64, 0);
            linkLabel1.AutoSize = true;
            linkLabel1.LinkColor = Color.FromArgb(192, 192, 255);
            linkLabel1.Location = new Point(12, 293);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(186, 15);
            linkLabel1.TabIndex = 1;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "https://github.com/BlackFoX1991";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // label1
            // 
            label1.BorderStyle = BorderStyle.FixedSingle;
            label1.ForeColor = Color.White;
            label1.Location = new Point(12, 318);
            label1.Name = "label1";
            label1.Size = new Size(464, 69);
            label1.TabIndex = 0;
            label1.Text = resources.GetString("label1.Text");
            // 
            // pictureBox1
            // 
            pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(12, 12);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(464, 276);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 2;
            pictureBox1.TabStop = false;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.ForeColor = Color.FromArgb(64, 64, 64);
            label2.Location = new Point(12, 450);
            label2.Name = "label2";
            label2.Size = new Size(118, 15);
            label2.TabIndex = 3;
            label2.Text = "Ⓒ 2026 Artur Loewen";
            // 
            // aboutDiag
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(488, 474);
            Controls.Add(label2);
            Controls.Add(pictureBox1);
            Controls.Add(linkLabel1);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "aboutDiag";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "About...";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private LinkLabel linkLabel1;
        private Label label1;
        private PictureBox pictureBox1;
        private Label label2;
    }
}