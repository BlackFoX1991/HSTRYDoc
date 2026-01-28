// KeyManagerDialog.Designer.cs (V4 Option A: per-block access control + bulk ops)
using System;
using System.Drawing;
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
        private ColumnHeader colSpkiLen;
        private Button btnAddRecipient;
        private Button btnRemoveRecipient;
        private Button btnCopyKeyId;

        private Label lblDropHint;

        // Block access controls (V4)
        private GroupBox grpBlockAccess;
        private Label lblBlockAccessHint;
        private ListView lvwBlocks;
        private ColumnHeader colBlockIndex;
        private ColumnHeader colBlockTitle;
        private ColumnHeader colBlockRights;

        private Button btnGrantRead;      // selected block only
        private Button btnGrantWrite;     // ALL blocks (Read+Write)
        private Button btnRevokeAccess;   // selected blocks
        private Button btnGrantReadAll;   // ALL blocks (Read)
        private Button btnRevokeAll;      // ALL blocks (remove slot)

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
            colSpkiLen = new ColumnHeader();
            btnAddRecipient = new Button();
            btnRemoveRecipient = new Button();
            btnCopyKeyId = new Button();
            lblDropHint = new Label();

            grpBlockAccess = new GroupBox();
            lblBlockAccessHint = new Label();
            lvwBlocks = new ListView();
            colBlockIndex = new ColumnHeader();
            colBlockTitle = new ColumnHeader();
            colBlockRights = new ColumnHeader();

            btnGrantRead = new Button();
            btnGrantWrite = new Button();
            btnRevokeAccess = new Button();
            btnGrantReadAll = new Button();
            btnRevokeAll = new Button();

            btnOk = new Button();
            btnCancel = new Button();

            grpMyKeys.SuspendLayout();
            grpRecipients.SuspendLayout();
            grpBlockAccess.SuspendLayout();
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
            grpMyKeys.Location = new Point(14, 16);
            grpMyKeys.Margin = new Padding(3, 4, 3, 4);
            grpMyKeys.Name = "grpMyKeys";
            grpMyKeys.Padding = new Padding(3, 4, 3, 4);
            grpMyKeys.Size = new Size(869, 237);
            grpMyKeys.TabIndex = 0;
            grpMyKeys.TabStop = false;
            grpMyKeys.Text = "My keys";
            // 
            // lblPrivPath
            // 
            lblPrivPath.AutoSize = true;
            lblPrivPath.Location = new Point(18, 37);
            lblPrivPath.Name = "lblPrivPath";
            lblPrivPath.Size = new Size(108, 20);
            lblPrivPath.TabIndex = 0;
            lblPrivPath.Text = "Private key file:";
            // 
            // txtPrivateKeyPath
            // 
            txtPrivateKeyPath.Location = new Point(130, 33);
            txtPrivateKeyPath.Margin = new Padding(3, 4, 3, 4);
            txtPrivateKeyPath.Name = "txtPrivateKeyPath";
            txtPrivateKeyPath.ReadOnly = true;
            txtPrivateKeyPath.Size = new Size(596, 27);
            txtPrivateKeyPath.TabIndex = 1;
            // 
            // btnBrowsePriv
            // 
            btnBrowsePriv.Location = new Point(734, 32);
            btnBrowsePriv.Margin = new Padding(3, 4, 3, 4);
            btnBrowsePriv.Name = "btnBrowsePriv";
            btnBrowsePriv.Size = new Size(114, 33);
            btnBrowsePriv.TabIndex = 2;
            btnBrowsePriv.Text = "Browse...";
            btnBrowsePriv.UseVisualStyleBackColor = true;
            // 
            // btnCreateKeyPair
            // 
            btnCreateKeyPair.Location = new Point(130, 72);
            btnCreateKeyPair.Margin = new Padding(3, 4, 3, 4);
            btnCreateKeyPair.Name = "btnCreateKeyPair";
            btnCreateKeyPair.Size = new Size(206, 36);
            btnCreateKeyPair.TabIndex = 3;
            btnCreateKeyPair.Text = "Create key pair...";
            btnCreateKeyPair.UseVisualStyleBackColor = true;
            // 
            // btnExportPublic
            // 
            btnExportPublic.Location = new Point(343, 72);
            btnExportPublic.Margin = new Padding(3, 4, 3, 4);
            btnExportPublic.Name = "btnExportPublic";
            btnExportPublic.Size = new Size(194, 36);
            btnExportPublic.TabIndex = 4;
            btnExportPublic.Text = "Export public key...";
            btnExportPublic.UseVisualStyleBackColor = true;
            // 
            // btnTransferOwnership
            // 
            btnTransferOwnership.Location = new Point(544, 72);
            btnTransferOwnership.Margin = new Padding(3, 4, 3, 4);
            btnTransferOwnership.Name = "btnTransferOwnership";
            btnTransferOwnership.Size = new Size(194, 36);
            btnTransferOwnership.TabIndex = 5;
            btnTransferOwnership.Text = "Transfer ownership...";
            btnTransferOwnership.UseVisualStyleBackColor = true;
            // 
            // lblMyKeyIdCaption
            // 
            lblMyKeyIdCaption.AutoSize = true;
            lblMyKeyIdCaption.Location = new Point(18, 129);
            lblMyKeyIdCaption.Name = "lblMyKeyIdCaption";
            lblMyKeyIdCaption.Size = new Size(55, 20);
            lblMyKeyIdCaption.TabIndex = 6;
            lblMyKeyIdCaption.Text = "Key ID:";
            // 
            // txtMyKeyId
            // 
            txtMyKeyId.Location = new Point(130, 125);
            txtMyKeyId.Margin = new Padding(3, 4, 3, 4);
            txtMyKeyId.Name = "txtMyKeyId";
            txtMyKeyId.ReadOnly = true;
            txtMyKeyId.Size = new Size(717, 27);
            txtMyKeyId.TabIndex = 7;
            // 
            // lblMyRecipientStatusCaption
            // 
            lblMyRecipientStatusCaption.AutoSize = true;
            lblMyRecipientStatusCaption.Location = new Point(18, 171);
            lblMyRecipientStatusCaption.Name = "lblMyRecipientStatusCaption";
            lblMyRecipientStatusCaption.Size = new Size(52, 20);
            lblMyRecipientStatusCaption.TabIndex = 8;
            lblMyRecipientStatusCaption.Text = "Status:";
            // 
            // lblMyRecipientStatus
            // 
            lblMyRecipientStatus.AutoSize = true;
            lblMyRecipientStatus.Location = new Point(130, 171);
            lblMyRecipientStatus.Name = "lblMyRecipientStatus";
            lblMyRecipientStatus.Size = new Size(156, 20);
            lblMyRecipientStatus.TabIndex = 9;
            lblMyRecipientStatus.Text = "No private key loaded";
            // 
            // btnAddMyself
            // 
            btnAddMyself.Location = new Point(734, 164);
            btnAddMyself.Margin = new Padding(3, 4, 3, 4);
            btnAddMyself.Name = "btnAddMyself";
            btnAddMyself.Size = new Size(114, 36);
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
            grpRecipients.Location = new Point(14, 261);
            grpRecipients.Margin = new Padding(3, 4, 3, 4);
            grpRecipients.Name = "grpRecipients";
            grpRecipients.Padding = new Padding(3, 4, 3, 4);
            grpRecipients.Size = new Size(869, 255);
            grpRecipients.TabIndex = 1;
            grpRecipients.TabStop = false;
            grpRecipients.Text = "Recipients";
            // 
            // lvwRecipients
            // 
            lvwRecipients.Columns.AddRange(new ColumnHeader[] { colKeyId, colAlg, colWrappedLen, colSpkiLen });
            lvwRecipients.FullRowSelect = true;
            lvwRecipients.GridLines = true;
            lvwRecipients.Location = new Point(18, 32);
            lvwRecipients.Margin = new Padding(3, 4, 3, 4);
            lvwRecipients.MultiSelect = false;
            lvwRecipients.Name = "lvwRecipients";
            lvwRecipients.Size = new Size(829, 160);
            lvwRecipients.TabIndex = 0;
            lvwRecipients.UseCompatibleStateImageBehavior = false;
            lvwRecipients.View = View.Details;
            // 
            // colKeyId
            // 
            colKeyId.Text = "Key ID (SHA-256)";
            colKeyId.Width = 430;
            // 
            // colAlg
            // 
            colAlg.Text = "Algorithm";
            colAlg.Width = 120;
            // 
            // colWrappedLen
            // 
            colWrappedLen.Text = "Wrapped DEK (bytes)";
            colWrappedLen.Width = 150;
            // 
            // colSpkiLen
            // 
            colSpkiLen.Text = "Public key (bytes)";
            colSpkiLen.Width = 120;
            // 
            // btnAddRecipient
            // 
            btnAddRecipient.Location = new Point(18, 207);
            btnAddRecipient.Margin = new Padding(3, 4, 3, 4);
            btnAddRecipient.Name = "btnAddRecipient";
            btnAddRecipient.Size = new Size(160, 36);
            btnAddRecipient.TabIndex = 1;
            btnAddRecipient.Text = "Add recipient...";
            btnAddRecipient.UseVisualStyleBackColor = true;
            // 
            // btnRemoveRecipient
            // 
            btnRemoveRecipient.Location = new Point(185, 207);
            btnRemoveRecipient.Margin = new Padding(3, 4, 3, 4);
            btnRemoveRecipient.Name = "btnRemoveRecipient";
            btnRemoveRecipient.Size = new Size(160, 36);
            btnRemoveRecipient.TabIndex = 2;
            btnRemoveRecipient.Text = "Remove selected";
            btnRemoveRecipient.UseVisualStyleBackColor = true;
            // 
            // btnCopyKeyId
            // 
            btnCopyKeyId.Location = new Point(352, 207);
            btnCopyKeyId.Margin = new Padding(3, 4, 3, 4);
            btnCopyKeyId.Name = "btnCopyKeyId";
            btnCopyKeyId.Size = new Size(160, 36);
            btnCopyKeyId.TabIndex = 3;
            btnCopyKeyId.Text = "Copy Key ID";
            btnCopyKeyId.UseVisualStyleBackColor = true;
            // 
            // lblDropHint
            // 
            lblDropHint.AutoSize = true;
            lblDropHint.Location = new Point(523, 215);
            lblDropHint.Name = "lblDropHint";
            lblDropHint.Size = new Size(316, 20);
            lblDropHint.TabIndex = 4;
            lblDropHint.Text = "Tip: Drag and drop .hstrypub files onto the list.";
            // 
            // grpBlockAccess
            // 
            grpBlockAccess.Controls.Add(lblBlockAccessHint);
            grpBlockAccess.Controls.Add(lvwBlocks);
            grpBlockAccess.Controls.Add(btnGrantRead);
            grpBlockAccess.Controls.Add(btnGrantWrite);
            grpBlockAccess.Controls.Add(btnRevokeAccess);
            grpBlockAccess.Controls.Add(btnGrantReadAll);
            grpBlockAccess.Controls.Add(btnRevokeAll);
            grpBlockAccess.Location = new Point(14, 524);
            grpBlockAccess.Margin = new Padding(3, 4, 3, 4);
            grpBlockAccess.Name = "grpBlockAccess";
            grpBlockAccess.Padding = new Padding(3, 4, 3, 4);
            grpBlockAccess.Size = new Size(869, 177);
            grpBlockAccess.TabIndex = 2;
            grpBlockAccess.TabStop = false;
            grpBlockAccess.Text = "Block access (V4)";
            // 
            // lblBlockAccessHint
            // 
            lblBlockAccessHint.AutoSize = true;
            lblBlockAccessHint.Location = new Point(18, 26);
            lblBlockAccessHint.Name = "lblBlockAccessHint";
            lblBlockAccessHint.Size = new Size(364, 20);
            lblBlockAccessHint.TabIndex = 0;
            lblBlockAccessHint.Text = "Select a recipient above to view and edit block rights.";
            // 
            // lvwBlocks
            // 
            lvwBlocks.Columns.AddRange(new ColumnHeader[] { colBlockIndex, colBlockTitle, colBlockRights });
            lvwBlocks.FullRowSelect = true;
            lvwBlocks.GridLines = true;
            lvwBlocks.Location = new Point(18, 50);
            lvwBlocks.Margin = new Padding(3, 4, 3, 4);
            lvwBlocks.MultiSelect = true;
            lvwBlocks.Name = "lvwBlocks";
            lvwBlocks.Size = new Size(629, 80);
            lvwBlocks.TabIndex = 1;
            lvwBlocks.UseCompatibleStateImageBehavior = false;
            lvwBlocks.View = View.Details;
            // 
            // colBlockIndex
            // 
            colBlockIndex.Text = "Index";
            colBlockIndex.Width = 70;
            // 
            // colBlockTitle
            // 
            colBlockTitle.Text = "Title";
            colBlockTitle.Width = 420;
            // 
            // colBlockRights
            // 
            colBlockRights.Text = "Rights";
            colBlockRights.Width = 120;
            // 
            // btnGrantRead
            // 
            btnGrantRead.Location = new Point(658, 50);
            btnGrantRead.Margin = new Padding(3, 4, 3, 4);
            btnGrantRead.Name = "btnGrantRead";
            btnGrantRead.Size = new Size(190, 33);
            btnGrantRead.TabIndex = 2;
            btnGrantRead.Text = "Grant read (selected)";
            btnGrantRead.UseVisualStyleBackColor = true;
            // 
            // btnGrantWrite
            // 
            btnGrantWrite.Location = new Point(658, 89);
            btnGrantWrite.Margin = new Padding(3, 4, 3, 4);
            btnGrantWrite.Name = "btnGrantWrite";
            btnGrantWrite.Size = new Size(190, 33);
            btnGrantWrite.TabIndex = 3;
            btnGrantWrite.Text = "Grant write (all)";
            btnGrantWrite.UseVisualStyleBackColor = true;
            // 
            // btnRevokeAccess
            // 
            btnRevokeAccess.Location = new Point(658, 128);
            btnRevokeAccess.Margin = new Padding(3, 4, 3, 4);
            btnRevokeAccess.Name = "btnRevokeAccess";
            btnRevokeAccess.Size = new Size(190, 33);
            btnRevokeAccess.TabIndex = 4;
            btnRevokeAccess.Text = "Revoke (selected)";
            btnRevokeAccess.UseVisualStyleBackColor = true;
            // 
            // btnGrantReadAll
            // 
            btnGrantReadAll.Location = new Point(18, 134);
            btnGrantReadAll.Margin = new Padding(3, 4, 3, 4);
            btnGrantReadAll.Name = "btnGrantReadAll";
            btnGrantReadAll.Size = new Size(309, 33);
            btnGrantReadAll.TabIndex = 5;
            btnGrantReadAll.Text = "Grant read (all)";
            btnGrantReadAll.UseVisualStyleBackColor = true;
            // 
            // btnRevokeAll
            // 
            btnRevokeAll.Location = new Point(338, 134);
            btnRevokeAll.Margin = new Padding(3, 4, 3, 4);
            btnRevokeAll.Name = "btnRevokeAll";
            btnRevokeAll.Size = new Size(309, 33);
            btnRevokeAll.TabIndex = 6;
            btnRevokeAll.Text = "Revoke all";
            btnRevokeAll.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            btnOk.Location = new Point(704, 715);
            btnOk.Margin = new Padding(3, 4, 3, 4);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(86, 37);
            btnOk.TabIndex = 3;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(797, 715);
            btnCancel.Margin = new Padding(3, 4, 3, 4);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(86, 37);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // KeyManagerDialog
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(896, 768);
            Controls.Add(grpBlockAccess);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(grpRecipients);
            Controls.Add(grpMyKeys);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "KeyManagerDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Key Management";
            grpMyKeys.ResumeLayout(false);
            grpMyKeys.PerformLayout();
            grpRecipients.ResumeLayout(false);
            grpRecipients.PerformLayout();
            grpBlockAccess.ResumeLayout(false);
            grpBlockAccess.PerformLayout();
            ResumeLayout(false);
        }
    }
}
