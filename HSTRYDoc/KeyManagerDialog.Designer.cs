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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(KeyManagerDialog));
            grpMyKeys = new GroupBox();
            lblPrivPath = new Label();
            txtPrivateKeyPath = new TextBox();
            btnBrowsePriv = new Button();
            btnCreateKeyPair = new Button();
            btnExportPublic = new Button();
            btnTransferOwnership = new Button();
            lblMyKeyIdCaption = new Label();
            txtMyKeyId = new TextBox();
            lblMyRecipientStatusCaption = new Label();
            lblMyRecipientStatus = new Label();
            btnAddMyself = new Button();
            grpRecipients = new GroupBox();
            lvwRecipients = new ListView();
            colKeyId = new ColumnHeader();
            colAlg = new ColumnHeader();
            colWrappedLen = new ColumnHeader();
            btnAddRecipient = new Button();
            btnRemoveRecipient = new Button();
            btnCopyKeyId = new Button();
            lblDropHint = new Label();
            btnOk = new Button();
            btnCancel = new Button();
            grpMyKeys.SuspendLayout();
            grpRecipients.SuspendLayout();
            SuspendLayout();
            // 
            // grpMyKeys
            // 
            grpMyKeys.Controls.Add(lblPrivPath);
            grpMyKeys.Controls.Add(txtPrivateKeyPath);
            grpMyKeys.Controls.Add(btnBrowsePriv);
            grpMyKeys.Controls.Add(btnCreateKeyPair);
            grpMyKeys.Controls.Add(btnExportPublic);
            grpMyKeys.Controls.Add(btnTransferOwnership);
            grpMyKeys.Controls.Add(lblMyKeyIdCaption);
            grpMyKeys.Controls.Add(txtMyKeyId);
            grpMyKeys.Controls.Add(lblMyRecipientStatusCaption);
            grpMyKeys.Controls.Add(lblMyRecipientStatus);
            grpMyKeys.Controls.Add(btnAddMyself);
            grpMyKeys.Location = new Point(12, 12);
            grpMyKeys.Name = "grpMyKeys";
            grpMyKeys.Size = new Size(760, 178);
            grpMyKeys.TabIndex = 0;
            grpMyKeys.TabStop = false;
            grpMyKeys.Text = "My keys";
            // 
            // lblPrivPath
            // 
            lblPrivPath.AutoSize = true;
            lblPrivPath.Location = new Point(16, 28);
            lblPrivPath.Name = "lblPrivPath";
            lblPrivPath.Size = new Size(86, 15);
            lblPrivPath.TabIndex = 0;
            lblPrivPath.Text = "Private key file:";
            // 
            // txtPrivateKeyPath
            // 
            txtPrivateKeyPath.Location = new Point(114, 25);
            txtPrivateKeyPath.Name = "txtPrivateKeyPath";
            txtPrivateKeyPath.ReadOnly = true;
            txtPrivateKeyPath.Size = new Size(522, 23);
            txtPrivateKeyPath.TabIndex = 1;
            // 
            // btnBrowsePriv
            // 
            btnBrowsePriv.Location = new Point(642, 24);
            btnBrowsePriv.Name = "btnBrowsePriv";
            btnBrowsePriv.Size = new Size(100, 25);
            btnBrowsePriv.TabIndex = 2;
            btnBrowsePriv.Text = "Browse...";
            btnBrowsePriv.UseVisualStyleBackColor = true;
            // 
            // btnCreateKeyPair
            // 
            btnCreateKeyPair.Location = new Point(114, 54);
            btnCreateKeyPair.Name = "btnCreateKeyPair";
            btnCreateKeyPair.Size = new Size(180, 27);
            btnCreateKeyPair.TabIndex = 3;
            btnCreateKeyPair.Text = "Create key pair...";
            btnCreateKeyPair.UseVisualStyleBackColor = true;
            // 
            // btnExportPublic
            // 
            btnExportPublic.Location = new Point(300, 54);
            btnExportPublic.Name = "btnExportPublic";
            btnExportPublic.Size = new Size(170, 27);
            btnExportPublic.TabIndex = 4;
            btnExportPublic.Text = "Export public key...";
            btnExportPublic.UseVisualStyleBackColor = true;
            // 
            // btnTransferOwnership
            // 
            btnTransferOwnership.Location = new Point(476, 54);
            btnTransferOwnership.Name = "btnTransferOwnership";
            btnTransferOwnership.Size = new Size(170, 27);
            btnTransferOwnership.TabIndex = 5;
            btnTransferOwnership.Text = "Transfer ownership...";
            btnTransferOwnership.UseVisualStyleBackColor = true;
            // 
            // lblMyKeyIdCaption
            // 
            lblMyKeyIdCaption.AutoSize = true;
            lblMyKeyIdCaption.Location = new Point(16, 97);
            lblMyKeyIdCaption.Name = "lblMyKeyIdCaption";
            lblMyKeyIdCaption.Size = new Size(43, 15);
            lblMyKeyIdCaption.TabIndex = 6;
            lblMyKeyIdCaption.Text = "Key ID:";
            // 
            // txtMyKeyId
            // 
            txtMyKeyId.Location = new Point(114, 94);
            txtMyKeyId.Name = "txtMyKeyId";
            txtMyKeyId.ReadOnly = true;
            txtMyKeyId.Size = new Size(628, 23);
            txtMyKeyId.TabIndex = 7;
            // 
            // lblMyRecipientStatusCaption
            // 
            lblMyRecipientStatusCaption.AutoSize = true;
            lblMyRecipientStatusCaption.Location = new Point(16, 128);
            lblMyRecipientStatusCaption.Name = "lblMyRecipientStatusCaption";
            lblMyRecipientStatusCaption.Size = new Size(42, 15);
            lblMyRecipientStatusCaption.TabIndex = 8;
            lblMyRecipientStatusCaption.Text = "Status:";
            // 
            // lblMyRecipientStatus
            // 
            lblMyRecipientStatus.AutoSize = true;
            lblMyRecipientStatus.Location = new Point(114, 128);
            lblMyRecipientStatus.Name = "lblMyRecipientStatus";
            lblMyRecipientStatus.Size = new Size(122, 15);
            lblMyRecipientStatus.TabIndex = 9;
            lblMyRecipientStatus.Text = "No private key loaded";
            // 
            // btnAddMyself
            // 
            btnAddMyself.Location = new Point(642, 123);
            btnAddMyself.Name = "btnAddMyself";
            btnAddMyself.Size = new Size(100, 27);
            btnAddMyself.TabIndex = 10;
            btnAddMyself.Text = "Add myself";
            btnAddMyself.UseVisualStyleBackColor = true;
            // 
            // grpRecipients
            // 
            grpRecipients.Controls.Add(lvwRecipients);
            grpRecipients.Controls.Add(btnAddRecipient);
            grpRecipients.Controls.Add(btnRemoveRecipient);
            grpRecipients.Controls.Add(btnCopyKeyId);
            grpRecipients.Controls.Add(lblDropHint);
            grpRecipients.Location = new Point(12, 196);
            grpRecipients.Name = "grpRecipients";
            grpRecipients.Size = new Size(760, 330);
            grpRecipients.TabIndex = 1;
            grpRecipients.TabStop = false;
            grpRecipients.Text = "Recipients";
            // 
            // lvwRecipients
            // 
            lvwRecipients.Columns.AddRange(new ColumnHeader[] { colKeyId, colAlg, colWrappedLen });
            lvwRecipients.FullRowSelect = true;
            lvwRecipients.GridLines = true;
            lvwRecipients.Location = new Point(16, 24);
            lvwRecipients.MultiSelect = false;
            lvwRecipients.Name = "lvwRecipients";
            lvwRecipients.Size = new Size(726, 245);
            lvwRecipients.TabIndex = 0;
            lvwRecipients.UseCompatibleStateImageBehavior = false;
            lvwRecipients.View = View.Details;
            // 
            // colKeyId
            // 
            colKeyId.Text = "Key ID (SHA-256)";
            colKeyId.Width = 520;
            // 
            // colAlg
            // 
            colAlg.Text = "Algorithm";
            colAlg.Width = 120;
            // 
            // colWrappedLen
            // 
            colWrappedLen.Text = "Wrapped DEK (bytes)";
            colWrappedLen.Width = 140;
            // 
            // btnAddRecipient
            // 
            btnAddRecipient.Location = new Point(16, 290);
            btnAddRecipient.Name = "btnAddRecipient";
            btnAddRecipient.Size = new Size(140, 27);
            btnAddRecipient.TabIndex = 1;
            btnAddRecipient.Text = "Add recipient...";
            btnAddRecipient.UseVisualStyleBackColor = true;
            // 
            // btnRemoveRecipient
            // 
            btnRemoveRecipient.Location = new Point(162, 290);
            btnRemoveRecipient.Name = "btnRemoveRecipient";
            btnRemoveRecipient.Size = new Size(140, 27);
            btnRemoveRecipient.TabIndex = 2;
            btnRemoveRecipient.Text = "Remove selected";
            btnRemoveRecipient.UseVisualStyleBackColor = true;
            // 
            // btnCopyKeyId
            // 
            btnCopyKeyId.Location = new Point(308, 290);
            btnCopyKeyId.Name = "btnCopyKeyId";
            btnCopyKeyId.Size = new Size(140, 27);
            btnCopyKeyId.TabIndex = 3;
            btnCopyKeyId.Text = "Copy Key ID";
            btnCopyKeyId.UseVisualStyleBackColor = true;
            // 
            // lblDropHint
            // 
            lblDropHint.AutoSize = true;
            lblDropHint.Location = new Point(458, 296);
            lblDropHint.Name = "lblDropHint";
            lblDropHint.Size = new Size(252, 15);
            lblDropHint.TabIndex = 4;
            lblDropHint.Text = "Tip: Drag and drop .hstrypub files onto the list.";
            // 
            // btnOk
            // 
            btnOk.Location = new Point(616, 536);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 28);
            btnOk.TabIndex = 2;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(697, 536);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 28);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // KeyManagerDialog
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(784, 576);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(grpRecipients);
            Controls.Add(grpMyKeys);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "KeyManagerDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Key Management";
            grpMyKeys.ResumeLayout(false);
            grpMyKeys.PerformLayout();
            grpRecipients.ResumeLayout(false);
            grpRecipients.PerformLayout();
            ResumeLayout(false);
        }
    }
}
