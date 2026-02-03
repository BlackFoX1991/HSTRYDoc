namespace HSTRYDoc
{
    partial class SplashScreen
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SplashScreen));
            picPanel = new Panel();
            label2 = new Label();
            picPanel.SuspendLayout();
            SuspendLayout();
            // 
            // picPanel
            // 
            picPanel.BackgroundImage = (Image)resources.GetObject("picPanel.BackgroundImage");
            picPanel.BackgroundImageLayout = ImageLayout.Stretch;
            picPanel.Controls.Add(label2);
            picPanel.Dock = DockStyle.Fill;
            picPanel.Location = new Point(0, 0);
            picPanel.Name = "picPanel";
            picPanel.Size = new Size(639, 380);
            picPanel.TabIndex = 0;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.ForeColor = Color.White;
            label2.Location = new Point(509, 356);
            label2.Name = "label2";
            label2.Size = new Size(118, 15);
            label2.TabIndex = 6;
            label2.Text = "Ⓒ 2026 Artur Loewen";
            // 
            // SplashScreen
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(639, 380);
            Controls.Add(picPanel);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "SplashScreen";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SplashScreen";
            TopMost = true;
            picPanel.ResumeLayout(false);
            picPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel picPanel;
        private Label label2;
    }
}