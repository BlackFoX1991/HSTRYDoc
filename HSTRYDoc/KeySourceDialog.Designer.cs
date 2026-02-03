namespace HSTRYDoc
{
    partial class KeySourceDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblIntro;
        private System.Windows.Forms.GroupBox grpSource;
        private System.Windows.Forms.RadioButton radioDefault;
        private System.Windows.Forms.RadioButton radioUsb;
        private System.Windows.Forms.RadioButton radioManual;

        private System.Windows.Forms.Label lblDefaultPathCaption;
        private System.Windows.Forms.Label lblDefaultPath;
        private System.Windows.Forms.Label lblDefaultStatus;

        private System.Windows.Forms.Panel pnlUsb;
        private System.Windows.Forms.ListBox lstUsbKeys;
        private System.Windows.Forms.Button btnRescan;
        private System.Windows.Forms.Label lblUsbStatus;

        private System.Windows.Forms.Panel pnlManual;
        private System.Windows.Forms.TextBox txtManualPath;
        private System.Windows.Forms.Button btnBrowse;

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
            grpSource = new System.Windows.Forms.GroupBox();
            radioDefault = new System.Windows.Forms.RadioButton();
            lblDefaultPathCaption = new System.Windows.Forms.Label();
            lblDefaultPath = new System.Windows.Forms.Label();
            lblDefaultStatus = new System.Windows.Forms.Label();
            radioUsb = new System.Windows.Forms.RadioButton();
            pnlUsb = new System.Windows.Forms.Panel();
            lstUsbKeys = new System.Windows.Forms.ListBox();
            btnRescan = new System.Windows.Forms.Button();
            lblUsbStatus = new System.Windows.Forms.Label();
            radioManual = new System.Windows.Forms.RadioButton();
            pnlManual = new System.Windows.Forms.Panel();
            txtManualPath = new System.Windows.Forms.TextBox();
            btnBrowse = new System.Windows.Forms.Button();
            lblHint = new System.Windows.Forms.Label();
            btnOk = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            grpSource.SuspendLayout();
            pnlUsb.SuspendLayout();
            pnlManual.SuspendLayout();
            SuspendLayout();
            // 
            // lblIntro
            // 
            lblIntro.Location = new System.Drawing.Point(12, 12);
            lblIntro.Name = "lblIntro";
            lblIntro.Size = new System.Drawing.Size(660, 40);
            lblIntro.TabIndex = 0;
            lblIntro.Text = "A private key is required to open this container.\r\nChoose where to load the key from.";
            // 
            // grpSource
            // 
            grpSource.Controls.Add(radioDefault);
            grpSource.Controls.Add(lblDefaultPathCaption);
            grpSource.Controls.Add(lblDefaultPath);
            grpSource.Controls.Add(lblDefaultStatus);
            grpSource.Controls.Add(radioUsb);
            grpSource.Controls.Add(pnlUsb);
            grpSource.Controls.Add(radioManual);
            grpSource.Controls.Add(pnlManual);
            grpSource.Location = new System.Drawing.Point(12, 58);
            grpSource.Name = "grpSource";
            grpSource.Size = new System.Drawing.Size(660, 290);
            grpSource.TabIndex = 1;
            grpSource.TabStop = false;
            grpSource.Text = "Key source";
            // 
            // radioDefault
            // 
            radioDefault.Checked = true;
            radioDefault.Location = new System.Drawing.Point(16, 24);
            radioDefault.Name = "radioDefault";
            radioDefault.Size = new System.Drawing.Size(300, 22);
            radioDefault.TabIndex = 0;
            radioDefault.TabStop = true;
            radioDefault.Text = "Use default key (Security_Keys)";
            // 
            // lblDefaultPathCaption
            // 
            lblDefaultPathCaption.Location = new System.Drawing.Point(36, 48);
            lblDefaultPathCaption.Name = "lblDefaultPathCaption";
            lblDefaultPathCaption.Size = new System.Drawing.Size(90, 18);
            lblDefaultPathCaption.TabIndex = 1;
            lblDefaultPathCaption.Text = "Default path:";
            // 
            // lblDefaultPath
            // 
            lblDefaultPath.Location = new System.Drawing.Point(128, 48);
            lblDefaultPath.Name = "lblDefaultPath";
            lblDefaultPath.Size = new System.Drawing.Size(510, 18);
            lblDefaultPath.TabIndex = 2;
            lblDefaultPath.Text = "<default_path>";
            // 
            // lblDefaultStatus
            // 
            lblDefaultStatus.Location = new System.Drawing.Point(128, 68);
            lblDefaultStatus.Name = "lblDefaultStatus";
            lblDefaultStatus.Size = new System.Drawing.Size(510, 18);
            lblDefaultStatus.TabIndex = 3;
            lblDefaultStatus.Text = "Status: <unknown>";
            // 
            // radioUsb
            // 
            radioUsb.Location = new System.Drawing.Point(16, 96);
            radioUsb.Name = "radioUsb";
            radioUsb.Size = new System.Drawing.Size(420, 22);
            radioUsb.TabIndex = 4;
            radioUsb.Text = "Use key from USB drive folder HSTRY_KEY";
            // 
            // pnlUsb
            // 
            pnlUsb.Controls.Add(lstUsbKeys);
            pnlUsb.Controls.Add(btnRescan);
            pnlUsb.Controls.Add(lblUsbStatus);
            pnlUsb.Location = new System.Drawing.Point(36, 120);
            pnlUsb.Name = "pnlUsb";
            pnlUsb.Size = new System.Drawing.Size(602, 72);
            pnlUsb.TabIndex = 5;
            // 
            // lstUsbKeys
            // 
            lstUsbKeys.Location = new System.Drawing.Point(0, 0);
            lstUsbKeys.Name = "lstUsbKeys";
            lstUsbKeys.Size = new System.Drawing.Size(480, 44);
            lstUsbKeys.TabIndex = 0;
            // 
            // btnRescan
            // 
            btnRescan.Location = new System.Drawing.Point(492, 0);
            btnRescan.Name = "btnRescan";
            btnRescan.Size = new System.Drawing.Size(110, 28);
            btnRescan.TabIndex = 1;
            btnRescan.Text = "Rescan";
            // 
            // lblUsbStatus
            // 
            lblUsbStatus.Location = new System.Drawing.Point(492, 34);
            lblUsbStatus.Name = "lblUsbStatus";
            lblUsbStatus.Size = new System.Drawing.Size(110, 36);
            lblUsbStatus.TabIndex = 2;
            lblUsbStatus.Text = "Status: <unknown>";
            // 
            // radioManual
            // 
            radioManual.Location = new System.Drawing.Point(16, 200);
            radioManual.Name = "radioManual";
            radioManual.Size = new System.Drawing.Size(220, 22);
            radioManual.TabIndex = 6;
            radioManual.Text = "Select key file manually";
            // 
            // pnlManual
            // 
            pnlManual.Controls.Add(txtManualPath);
            pnlManual.Controls.Add(btnBrowse);
            pnlManual.Location = new System.Drawing.Point(36, 224);
            pnlManual.Name = "pnlManual";
            pnlManual.Size = new System.Drawing.Size(602, 40);
            pnlManual.TabIndex = 7;
            // 
            // txtManualPath
            // 
            txtManualPath.Location = new System.Drawing.Point(0, 8);
            txtManualPath.Name = "txtManualPath";
            txtManualPath.ReadOnly = true;
            txtManualPath.Size = new System.Drawing.Size(480, 27);
            txtManualPath.TabIndex = 0;
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new System.Drawing.Point(492, 6);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new System.Drawing.Size(110, 28);
            btnBrowse.TabIndex = 1;
            btnBrowse.Text = "Browse...";
            // 
            // lblHint
            // 
            lblHint.Location = new System.Drawing.Point(12, 356);
            lblHint.Name = "lblHint";
            lblHint.Size = new System.Drawing.Size(660, 22);
            lblHint.TabIndex = 2;
            lblHint.Text = "Click OK to continue.";
            // 
            // btnOk
            // 
            btnOk.Location = new System.Drawing.Point(516, 386);
            btnOk.Name = "btnOk";
            btnOk.Size = new System.Drawing.Size(75, 28);
            btnOk.TabIndex = 3;
            btnOk.Text = "OK";
            // 
            // btnCancel
            // 
            btnCancel.Location = new System.Drawing.Point(597, 386);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(75, 28);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            // 
            // KeySourceDialog
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(684, 426);
            Controls.Add(lblIntro);
            Controls.Add(grpSource);
            Controls.Add(lblHint);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "KeySourceDialog";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Private key";
            Load += KeySourceDialog_Load;
            grpSource.ResumeLayout(false);
            pnlUsb.ResumeLayout(false);
            pnlManual.ResumeLayout(false);
            pnlManual.PerformLayout();
            ResumeLayout(false);
        }
    }
}
