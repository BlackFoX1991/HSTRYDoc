namespace HSTRYDoc
{
    partial class KeySourceDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblIntro;
        private System.Windows.Forms.Label lblDriveCaption;
        private System.Windows.Forms.ListBox lstDrives;
        private System.Windows.Forms.Button btnRescan;
        private System.Windows.Forms.Label lblFolderCaption;
        private System.Windows.Forms.Label lblKeyFolder;
        private System.Windows.Forms.Label lblKeyCaption;
        private System.Windows.Forms.ListBox lstKeys;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lblIntro = new System.Windows.Forms.Label();
            lblDriveCaption = new System.Windows.Forms.Label();
            lstDrives = new System.Windows.Forms.ListBox();
            btnRescan = new System.Windows.Forms.Button();
            lblFolderCaption = new System.Windows.Forms.Label();
            lblKeyFolder = new System.Windows.Forms.Label();
            lblKeyCaption = new System.Windows.Forms.Label();
            lstKeys = new System.Windows.Forms.ListBox();
            lblHint = new System.Windows.Forms.Label();
            btnOk = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // lblIntro
            // 
            lblIntro.Location = new System.Drawing.Point(12, 12);
            lblIntro.Name = "lblIntro";
            lblIntro.Size = new System.Drawing.Size(660, 42);
            lblIntro.TabIndex = 0;
            lblIntro.Text = "Choose a drive.";
            // 
            // lblDriveCaption
            // 
            lblDriveCaption.Location = new System.Drawing.Point(12, 62);
            lblDriveCaption.Name = "lblDriveCaption";
            lblDriveCaption.Size = new System.Drawing.Size(190, 22);
            lblDriveCaption.TabIndex = 1;
            lblDriveCaption.Text = "Available drives:";
            // 
            // lstDrives
            // 
            lstDrives.FormattingEnabled = true;
            lstDrives.ItemHeight = 20;
            lstDrives.Location = new System.Drawing.Point(12, 88);
            lstDrives.Name = "lstDrives";
            lstDrives.Size = new System.Drawing.Size(190, 184);
            lstDrives.TabIndex = 2;
            // 
            // btnRescan
            // 
            btnRescan.Location = new System.Drawing.Point(12, 278);
            btnRescan.Name = "btnRescan";
            btnRescan.Size = new System.Drawing.Size(190, 30);
            btnRescan.TabIndex = 3;
            btnRescan.Text = "Rescan";
            // 
            // lblFolderCaption
            // 
            lblFolderCaption.Location = new System.Drawing.Point(224, 62);
            lblFolderCaption.Name = "lblFolderCaption";
            lblFolderCaption.Size = new System.Drawing.Size(120, 22);
            lblFolderCaption.TabIndex = 4;
            lblFolderCaption.Text = "Key folder:";
            // 
            // lblKeyFolder
            // 
            lblKeyFolder.AutoEllipsis = true;
            lblKeyFolder.Location = new System.Drawing.Point(224, 88);
            lblKeyFolder.Name = "lblKeyFolder";
            lblKeyFolder.Size = new System.Drawing.Size(448, 24);
            lblKeyFolder.TabIndex = 5;
            lblKeyFolder.Text = "<no drive selected>";
            // 
            // lblKeyCaption
            // 
            lblKeyCaption.Location = new System.Drawing.Point(224, 124);
            lblKeyCaption.Name = "lblKeyCaption";
            lblKeyCaption.Size = new System.Drawing.Size(220, 22);
            lblKeyCaption.TabIndex = 6;
            lblKeyCaption.Text = "Private keys in HSTRY_KEY:";
            // 
            // lstKeys
            // 
            lstKeys.FormattingEnabled = true;
            lstKeys.ItemHeight = 20;
            lstKeys.Location = new System.Drawing.Point(224, 150);
            lstKeys.Name = "lstKeys";
            lstKeys.Size = new System.Drawing.Size(448, 124);
            lstKeys.TabIndex = 7;
            // 
            // lblHint
            // 
            lblHint.Location = new System.Drawing.Point(12, 326);
            lblHint.Name = "lblHint";
            lblHint.Size = new System.Drawing.Size(660, 24);
            lblHint.TabIndex = 8;
            lblHint.Text = "Click OK to continue.";
            // 
            // btnOk
            // 
            btnOk.Location = new System.Drawing.Point(516, 360);
            btnOk.Name = "btnOk";
            btnOk.Size = new System.Drawing.Size(75, 30);
            btnOk.TabIndex = 9;
            btnOk.Text = "OK";
            // 
            // btnCancel
            // 
            btnCancel.Location = new System.Drawing.Point(597, 360);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(75, 30);
            btnCancel.TabIndex = 10;
            btnCancel.Text = "Cancel";
            // 
            // KeySourceDialog
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(684, 402);
            Controls.Add(lblIntro);
            Controls.Add(lblDriveCaption);
            Controls.Add(lstDrives);
            Controls.Add(btnRescan);
            Controls.Add(lblFolderCaption);
            Controls.Add(lblKeyFolder);
            Controls.Add(lblKeyCaption);
            Controls.Add(lstKeys);
            Controls.Add(lblHint);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "KeySourceDialog";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Private key drive";
            Load += KeySourceDialog_Load;
            ResumeLayout(false);
        }
    }
}
