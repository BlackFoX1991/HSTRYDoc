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
            this.components = new System.ComponentModel.Container();

            this.lblIntro = new System.Windows.Forms.Label();
            this.grpSource = new System.Windows.Forms.GroupBox();
            this.radioDefault = new System.Windows.Forms.RadioButton();
            this.radioUsb = new System.Windows.Forms.RadioButton();
            this.radioManual = new System.Windows.Forms.RadioButton();

            this.lblDefaultPathCaption = new System.Windows.Forms.Label();
            this.lblDefaultPath = new System.Windows.Forms.Label();
            this.lblDefaultStatus = new System.Windows.Forms.Label();

            this.pnlUsb = new System.Windows.Forms.Panel();
            this.lstUsbKeys = new System.Windows.Forms.ListBox();
            this.btnRescan = new System.Windows.Forms.Button();
            this.lblUsbStatus = new System.Windows.Forms.Label();

            this.pnlManual = new System.Windows.Forms.Panel();
            this.txtManualPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();

            this.lblHint = new System.Windows.Forms.Label();

            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            this.grpSource.SuspendLayout();
            this.pnlUsb.SuspendLayout();
            this.pnlManual.SuspendLayout();
            this.SuspendLayout();

            // lblIntro
            this.lblIntro.AutoSize = false;
            this.lblIntro.Location = new System.Drawing.Point(12, 12);
            this.lblIntro.Size = new System.Drawing.Size(660, 40);
            this.lblIntro.Text = "A private key is required to open this container.\r\nChoose where to load the key from.";

            // grpSource
            this.grpSource.Location = new System.Drawing.Point(12, 58);
            this.grpSource.Size = new System.Drawing.Size(660, 290);
            this.grpSource.Text = "Key source";

            // radioDefault
            this.radioDefault.Location = new System.Drawing.Point(16, 24);
            this.radioDefault.Size = new System.Drawing.Size(220, 22);
            this.radioDefault.Text = "Use default key (Security_Keys)";
            this.radioDefault.Checked = true;

            // radioUsb
            this.radioUsb.Location = new System.Drawing.Point(16, 96);
            this.radioUsb.Size = new System.Drawing.Size(360, 22);
            this.radioUsb.Text = "Use key from USB drive folder HSTRY_KEY";

            // radioManual
            this.radioManual.Location = new System.Drawing.Point(16, 200);
            this.radioManual.Size = new System.Drawing.Size(220, 22);
            this.radioManual.Text = "Select key file manually";

            // Default section labels
            this.lblDefaultPathCaption.Location = new System.Drawing.Point(36, 48);
            this.lblDefaultPathCaption.Size = new System.Drawing.Size(90, 18);
            this.lblDefaultPathCaption.Text = "Default path:";

            this.lblDefaultPath.Location = new System.Drawing.Point(128, 48);
            this.lblDefaultPath.Size = new System.Drawing.Size(510, 18);
            this.lblDefaultPath.Text = "<default_path>";

            this.lblDefaultStatus.Location = new System.Drawing.Point(128, 68);
            this.lblDefaultStatus.Size = new System.Drawing.Size(510, 18);
            this.lblDefaultStatus.Text = "Status: <unknown>";

            // pnlUsb
            this.pnlUsb.Location = new System.Drawing.Point(36, 120);
            this.pnlUsb.Size = new System.Drawing.Size(602, 72);

            // lstUsbKeys
            this.lstUsbKeys.Location = new System.Drawing.Point(0, 0);
            this.lstUsbKeys.Size = new System.Drawing.Size(480, 56);

            // btnRescan
            this.btnRescan.Location = new System.Drawing.Point(492, 0);
            this.btnRescan.Size = new System.Drawing.Size(110, 28);
            this.btnRescan.Text = "Rescan";

            // lblUsbStatus
            this.lblUsbStatus.Location = new System.Drawing.Point(492, 34);
            this.lblUsbStatus.Size = new System.Drawing.Size(110, 36);
            this.lblUsbStatus.Text = "Status: <unknown>";

            // pnlManual
            this.pnlManual.Location = new System.Drawing.Point(36, 224);
            this.pnlManual.Size = new System.Drawing.Size(602, 40);

            // txtManualPath
            this.txtManualPath.Location = new System.Drawing.Point(0, 8);
            this.txtManualPath.Size = new System.Drawing.Size(480, 23);
            this.txtManualPath.ReadOnly = true;

            // btnBrowse
            this.btnBrowse.Location = new System.Drawing.Point(492, 6);
            this.btnBrowse.Size = new System.Drawing.Size(110, 28);
            this.btnBrowse.Text = "Browse...";

            // lblHint
            this.lblHint.Location = new System.Drawing.Point(12, 356);
            this.lblHint.Size = new System.Drawing.Size(660, 22);
            this.lblHint.Text = "Click OK to continue.";

            // btnOk
            this.btnOk.Location = new System.Drawing.Point(516, 386);
            this.btnOk.Size = new System.Drawing.Size(75, 28);
            this.btnOk.Text = "OK";

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(597, 386);
            this.btnCancel.Size = new System.Drawing.Size(75, 28);
            this.btnCancel.Text = "Cancel";

            // Compose panels
            this.pnlUsb.Controls.Add(this.lstUsbKeys);
            this.pnlUsb.Controls.Add(this.btnRescan);
            this.pnlUsb.Controls.Add(this.lblUsbStatus);

            this.pnlManual.Controls.Add(this.txtManualPath);
            this.pnlManual.Controls.Add(this.btnBrowse);

            // Compose group
            this.grpSource.Controls.Add(this.radioDefault);
            this.grpSource.Controls.Add(this.lblDefaultPathCaption);
            this.grpSource.Controls.Add(this.lblDefaultPath);
            this.grpSource.Controls.Add(this.lblDefaultStatus);

            this.grpSource.Controls.Add(this.radioUsb);
            this.grpSource.Controls.Add(this.pnlUsb);

            this.grpSource.Controls.Add(this.radioManual);
            this.grpSource.Controls.Add(this.pnlManual);

            // Form
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 426);
            this.Controls.Add(this.lblIntro);
            this.Controls.Add(this.grpSource);
            this.Controls.Add(this.lblHint);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Private key";

            this.Load += new System.EventHandler(this.KeySourceDialog_Load);

            this.grpSource.ResumeLayout(false);
            this.pnlUsb.ResumeLayout(false);
            this.pnlManual.ResumeLayout(false);
            this.pnlManual.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
