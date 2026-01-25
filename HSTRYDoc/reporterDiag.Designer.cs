namespace HSTRYDoc
{
    partial class reporterDiag
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(reporterDiag));
            panel1 = new Panel();
            lblStatus = new Label();
            prgStatus = new ProgressBar();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = Color.AliceBlue;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(prgStatus);
            panel1.Controls.Add(lblStatus);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(602, 151);
            panel1.TabIndex = 0;
            // 
            // lblStatus
            // 
            lblStatus.Dock = DockStyle.Top;
            lblStatus.Location = new Point(0, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(600, 82);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "...";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // prgStatus
            // 
            prgStatus.Location = new Point(11, 99);
            prgStatus.Name = "prgStatus";
            prgStatus.Size = new Size(578, 39);
            prgStatus.Style = ProgressBarStyle.Continuous;
            prgStatus.TabIndex = 1;
            // 
            // reporterDiag
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(602, 151);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "reporterDiag";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Progress...";
            TopMost = true;
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private ProgressBar prgStatus;
        private Label lblStatus;
    }
}