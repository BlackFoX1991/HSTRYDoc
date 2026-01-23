// KeyManagerDialog.Designer.cs
using System;
using System.Windows.Forms;

namespace HSTRYDoc
{
    partial class KeyManagerDialog
    {
        private System.ComponentModel.IContainer components = null;

        private GroupBox grpMyKeys;
        private Label lblPrivPath;
        private TextBox txtPrivateKeyPath;
        private Button btnBrowsePriv;
        private Button btnCreateKeyPair;
        private Button btnExportPublic;
        private Button btnTransferOwnership;
        private Label lblMyKeyIdCaption;
        private TextBox txtMyKeyId;

        private Label lblMyRecipientStatusCaption;
        private Label lblMyRecipientStatus;
        private Button btnAddMyself;

        private GroupBox grpRecipients;
        private ListView lvwRecipients;
        private ColumnHeader colKeyId;
        private ColumnHeader colAlg;
        private ColumnHeader colWrappedLen;
        private Button btnAddRecipient;
        private Button btnRemoveRecipient;
        private Button btnCopyKeyId;

        private Label lblDropHint;

        private Button btnOk;
        private Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.grpMyKeys = new System.Windows.Forms.GroupBox();
            this.lblPrivPath = new System.Windows.Forms.Label();
            this.txtPrivateKeyPath = new System.Windows.Forms.TextBox();
            this.btnBrowsePriv = new System.Windows.Forms.Button();
            this.btnCreateKeyPair = new System.Windows.Forms.Button();
            this.btnExportPublic = new System.Windows.Forms.Button();
            this.btnTransferOwnership = new System.Windows.Forms.Button();
            this.lblMyKeyIdCaption = new System.Windows.Forms.Label();
            this.txtMyKeyId = new System.Windows.Forms.TextBox();

            this.lblMyRecipientStatusCaption = new System.Windows.Forms.Label();
            this.lblMyRecipientStatus = new System.Windows.Forms.Label();
            this.btnAddMyself = new System.Windows.Forms.Button();

            this.grpRecipients = new System.Windows.Forms.GroupBox();
            this.lvwRecipients = new System.Windows.Forms.ListView();
            this.colKeyId = new System.Windows.Forms.ColumnHeader();
            this.colAlg = new System.Windows.Forms.ColumnHeader();
            this.colWrappedLen = new System.Windows.Forms.ColumnHeader();
            this.btnAddRecipient = new System.Windows.Forms.Button();
            this.btnRemoveRecipient = new System.Windows.Forms.Button();
            this.btnCopyKeyId = new System.Windows.Forms.Button();
            this.lblDropHint = new System.Windows.Forms.Label();

            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            this.grpMyKeys.SuspendLayout();
            this.grpRecipients.SuspendLayout();
            this.SuspendLayout();

            // grpMyKeys
            this.grpMyKeys.Controls.Add(this.lblPrivPath);
            this.grpMyKeys.Controls.Add(this.txtPrivateKeyPath);
            this.grpMyKeys.Controls.Add(this.btnBrowsePriv);
            this.grpMyKeys.Controls.Add(this.btnCreateKeyPair);
            this.grpMyKeys.Controls.Add(this.btnExportPublic);
            this.grpMyKeys.Controls.Add(this.btnTransferOwnership);
            this.grpMyKeys.Controls.Add(this.lblMyKeyIdCaption);
            this.grpMyKeys.Controls.Add(this.txtMyKeyId);
            this.grpMyKeys.Controls.Add(this.lblMyRecipientStatusCaption);
            this.grpMyKeys.Controls.Add(this.lblMyRecipientStatus);
            this.grpMyKeys.Controls.Add(this.btnAddMyself);
            this.grpMyKeys.Location = new System.Drawing.Point(12, 12);
            this.grpMyKeys.Name = "grpMyKeys";
            this.grpMyKeys.Size = new System.Drawing.Size(760, 178);
            this.grpMyKeys.TabIndex = 0;
            this.grpMyKeys.TabStop = false;
            this.grpMyKeys.Text = "My keys";

            // lblPrivPath
            this.lblPrivPath.AutoSize = true;
            this.lblPrivPath.Location = new System.Drawing.Point(16, 28);
            this.lblPrivPath.Name = "lblPrivPath";
            this.lblPrivPath.Size = new System.Drawing.Size(92, 15);
            this.lblPrivPath.TabIndex = 0;
            this.lblPrivPath.Text = "Private key file:";

            // txtPrivateKeyPath
            this.txtPrivateKeyPath.Location = new System.Drawing.Point(114, 25);
            this.txtPrivateKeyPath.Name = "txtPrivateKeyPath";
            this.txtPrivateKeyPath.ReadOnly = true;
            this.txtPrivateKeyPath.Size = new System.Drawing.Size(522, 23);
            this.txtPrivateKeyPath.TabIndex = 1;

            // btnBrowsePriv
            this.btnBrowsePriv.Location = new System.Drawing.Point(642, 24);
            this.btnBrowsePriv.Name = "btnBrowsePriv";
            this.btnBrowsePriv.Size = new System.Drawing.Size(100, 25);
            this.btnBrowsePriv.TabIndex = 2;
            this.btnBrowsePriv.Text = "Browse...";
            this.btnBrowsePriv.UseVisualStyleBackColor = true;

            // btnCreateKeyPair
            this.btnCreateKeyPair.Location = new System.Drawing.Point(114, 54);
            this.btnCreateKeyPair.Name = "btnCreateKeyPair";
            this.btnCreateKeyPair.Size = new System.Drawing.Size(180, 27);
            this.btnCreateKeyPair.TabIndex = 3;
            this.btnCreateKeyPair.Text = "Create key pair...";
            this.btnCreateKeyPair.UseVisualStyleBackColor = true;

            // btnExportPublic
            this.btnExportPublic.Location = new System.Drawing.Point(300, 54);
            this.btnExportPublic.Name = "btnExportPublic";
            this.btnExportPublic.Size = new System.Drawing.Size(170, 27);
            this.btnExportPublic.TabIndex = 4;
            this.btnExportPublic.Text = "Export public key...";
            this.btnExportPublic.UseVisualStyleBackColor = true;

            // btnTransferOwnership
            this.btnTransferOwnership.Location = new System.Drawing.Point(476, 54);
            this.btnTransferOwnership.Name = "btnTransferOwnership";
            this.btnTransferOwnership.Size = new System.Drawing.Size(170, 27);
            this.btnTransferOwnership.TabIndex = 5;
            this.btnTransferOwnership.Text = "Transfer ownership...";
            this.btnTransferOwnership.UseVisualStyleBackColor = true;

            // lblMyKeyIdCaption
            this.lblMyKeyIdCaption.AutoSize = true;
            this.lblMyKeyIdCaption.Location = new System.Drawing.Point(16, 97);
            this.lblMyKeyIdCaption.Name = "lblMyKeyIdCaption";
            this.lblMyKeyIdCaption.Size = new System.Drawing.Size(46, 15);
            this.lblMyKeyIdCaption.TabIndex = 6;
            this.lblMyKeyIdCaption.Text = "Key ID:";

            // txtMyKeyId
            this.txtMyKeyId.Location = new System.Drawing.Point(114, 94);
            this.txtMyKeyId.Name = "txtMyKeyId";
            this.txtMyKeyId.ReadOnly = true;
            this.txtMyKeyId.Size = new System.Drawing.Size(628, 23);
            this.txtMyKeyId.TabIndex = 7;

            // lblMyRecipientStatusCaption
            this.lblMyRecipientStatusCaption.AutoSize = true;
            this.lblMyRecipientStatusCaption.Location = new System.Drawing.Point(16, 128);
            this.lblMyRecipientStatusCaption.Name = "lblMyRecipientStatusCaption";
            this.lblMyRecipientStatusCaption.Size = new System.Drawing.Size(48, 15);
            this.lblMyRecipientStatusCaption.TabIndex = 8;
            this.lblMyRecipientStatusCaption.Text = "Status:";

            // lblMyRecipientStatus
            this.lblMyRecipientStatus.AutoSize = true;
            this.lblMyRecipientStatus.Location = new System.Drawing.Point(114, 128);
            this.lblMyRecipientStatus.Name = "lblMyRecipientStatus";
            this.lblMyRecipientStatus.Size = new System.Drawing.Size(108, 15);
            this.lblMyRecipientStatus.TabIndex = 9;
            this.lblMyRecipientStatus.Text = "No private key loaded";

            // btnAddMyself
            this.btnAddMyself.Location = new System.Drawing.Point(642, 123);
            this.btnAddMyself.Name = "btnAddMyself";
            this.btnAddMyself.Size = new System.Drawing.Size(100, 27);
            this.btnAddMyself.TabIndex = 10;
            this.btnAddMyself.Text = "Add myself";
            this.btnAddMyself.UseVisualStyleBackColor = true;

            // grpRecipients
            this.grpRecipients.Controls.Add(this.lvwRecipients);
            this.grpRecipients.Controls.Add(this.btnAddRecipient);
            this.grpRecipients.Controls.Add(this.btnRemoveRecipient);
            this.grpRecipients.Controls.Add(this.btnCopyKeyId);
            this.grpRecipients.Controls.Add(this.lblDropHint);
            this.grpRecipients.Location = new System.Drawing.Point(12, 196);
            this.grpRecipients.Name = "grpRecipients";
            this.grpRecipients.Size = new System.Drawing.Size(760, 330);
            this.grpRecipients.TabIndex = 1;
            this.grpRecipients.TabStop = false;
            this.grpRecipients.Text = "Recipients";

            // lvwRecipients
            this.lvwRecipients.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colKeyId,
            this.colAlg,
            this.colWrappedLen});
            this.lvwRecipients.FullRowSelect = true;
            this.lvwRecipients.GridLines = true;
            this.lvwRecipients.HideSelection = false;
            this.lvwRecipients.Location = new System.Drawing.Point(16, 24);
            this.lvwRecipients.MultiSelect = false;
            this.lvwRecipients.Name = "lvwRecipients";
            this.lvwRecipients.Size = new System.Drawing.Size(726, 245);
            this.lvwRecipients.TabIndex = 0;
            this.lvwRecipients.UseCompatibleStateImageBehavior = false;
            this.lvwRecipients.View = System.Windows.Forms.View.Details;

            // colKeyId
            this.colKeyId.Text = "Key ID (SHA-256)";
            this.colKeyId.Width = 520;

            // colAlg
            this.colAlg.Text = "Algorithm";
            this.colAlg.Width = 120;

            // colWrappedLen
            this.colWrappedLen.Text = "Wrapped DEK (bytes)";
            this.colWrappedLen.Width = 140;

            // btnAddRecipient
            this.btnAddRecipient.Location = new System.Drawing.Point(16, 290);
            this.btnAddRecipient.Name = "btnAddRecipient";
            this.btnAddRecipient.Size = new System.Drawing.Size(140, 27);
            this.btnAddRecipient.TabIndex = 1;
            this.btnAddRecipient.Text = "Add recipient...";
            this.btnAddRecipient.UseVisualStyleBackColor = true;

            // btnRemoveRecipient
            this.btnRemoveRecipient.Location = new System.Drawing.Point(162, 290);
            this.btnRemoveRecipient.Name = "btnRemoveRecipient";
            this.btnRemoveRecipient.Size = new System.Drawing.Size(140, 27);
            this.btnRemoveRecipient.TabIndex = 2;
            this.btnRemoveRecipient.Text = "Remove selected";
            this.btnRemoveRecipient.UseVisualStyleBackColor = true;

            // btnCopyKeyId
            this.btnCopyKeyId.Location = new System.Drawing.Point(308, 290);
            this.btnCopyKeyId.Name = "btnCopyKeyId";
            this.btnCopyKeyId.Size = new System.Drawing.Size(140, 27);
            this.btnCopyKeyId.TabIndex = 3;
            this.btnCopyKeyId.Text = "Copy Key ID";
            this.btnCopyKeyId.UseVisualStyleBackColor = true;

            // lblDropHint
            this.lblDropHint.AutoSize = true;
            this.lblDropHint.Location = new System.Drawing.Point(458, 296);
            this.lblDropHint.Name = "lblDropHint";
            this.lblDropHint.Size = new System.Drawing.Size(284, 15);
            this.lblDropHint.TabIndex = 4;
            this.lblDropHint.Text = "Tip: Drag and drop .hstrypub files onto the list.";

            // btnOk
            this.btnOk.Location = new System.Drawing.Point(616, 536);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 28);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(697, 536);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 28);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;

            // KeyManagerDialog
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 576);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.grpRecipients);
            this.Controls.Add(this.grpMyKeys);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "KeyManagerDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Key Management";

            this.grpMyKeys.ResumeLayout(false);
            this.grpMyKeys.PerformLayout();
            this.grpRecipients.ResumeLayout(false);
            this.grpRecipients.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
