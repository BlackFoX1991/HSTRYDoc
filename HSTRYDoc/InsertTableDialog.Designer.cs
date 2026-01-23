// InsertTableDialog.Designer.cs
using System;
using System.Windows.Forms;

namespace HSTRYDoc
{
    partial class InsertTableDialog
    {
        private System.ComponentModel.IContainer components = null;

        private GroupBox grpSize;
        private Label lblHint;
        private Label lblSelected;
        private TableSizePicker sizePicker;

        private GroupBox grpWidth;
        private CheckBox chkFitToEditor;
        private Label lblDefaultWidth;
        private NumericUpDown nudDefaultWidthPx;
        private Button btnEqualize;

        private DataGridView dgvWidths;
        private DataGridViewTextBoxColumn colIndex;
        private DataGridViewTextBoxColumn colWidthPx;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InsertTableDialog));
            grpSize = new GroupBox();
            lblHint = new Label();
            lblSelected = new Label();
            sizePicker = new TableSizePicker();
            grpWidth = new GroupBox();
            chkFitToEditor = new CheckBox();
            lblDefaultWidth = new Label();
            nudDefaultWidthPx = new NumericUpDown();
            btnEqualize = new Button();
            dgvWidths = new DataGridView();
            colIndex = new DataGridViewTextBoxColumn();
            colWidthPx = new DataGridViewTextBoxColumn();
            btnOk = new Button();
            btnCancel = new Button();
            grpSize.SuspendLayout();
            grpWidth.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudDefaultWidthPx).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvWidths).BeginInit();
            SuspendLayout();
            // 
            // grpSize
            // 
            grpSize.Controls.Add(lblHint);
            grpSize.Controls.Add(lblSelected);
            grpSize.Controls.Add(sizePicker);
            grpSize.Location = new Point(12, 12);
            grpSize.Name = "grpSize";
            grpSize.Size = new Size(360, 270);
            grpSize.TabIndex = 0;
            grpSize.TabStop = false;
            grpSize.Text = "Table size";
            // 
            // lblHint
            // 
            lblHint.AutoSize = true;
            lblHint.Location = new Point(16, 28);
            lblHint.Name = "lblHint";
            lblHint.Size = new Size(294, 15);
            lblHint.TabIndex = 0;
            lblHint.Text = "Move the mouse to select rows/columns, click to lock.";
            // 
            // lblSelected
            // 
            lblSelected.AutoSize = true;
            lblSelected.Location = new Point(16, 52);
            lblSelected.Name = "lblSelected";
            lblSelected.Size = new Size(84, 15);
            lblSelected.TabIndex = 1;
            lblSelected.Text = "Selection: 0 x 0";
            // 
            // sizePicker
            // 
            sizePicker.Location = new Point(19, 74);
            sizePicker.MaxCols = 10;
            sizePicker.MaxRows = 8;
            sizePicker.Name = "sizePicker";
            sizePicker.Size = new Size(320, 179);
            sizePicker.TabIndex = 2;
            // 
            // grpWidth
            // 
            grpWidth.Controls.Add(chkFitToEditor);
            grpWidth.Controls.Add(lblDefaultWidth);
            grpWidth.Controls.Add(nudDefaultWidthPx);
            grpWidth.Controls.Add(btnEqualize);
            grpWidth.Controls.Add(dgvWidths);
            grpWidth.Location = new Point(378, 12);
            grpWidth.Name = "grpWidth";
            grpWidth.Size = new Size(360, 270);
            grpWidth.TabIndex = 1;
            grpWidth.TabStop = false;
            grpWidth.Text = "Column widths";
            // 
            // chkFitToEditor
            // 
            chkFitToEditor.AutoSize = true;
            chkFitToEditor.Location = new Point(19, 28);
            chkFitToEditor.Name = "chkFitToEditor";
            chkFitToEditor.Size = new Size(120, 19);
            chkFitToEditor.TabIndex = 0;
            chkFitToEditor.Text = "Fit to editor width";
            chkFitToEditor.UseVisualStyleBackColor = true;
            // 
            // lblDefaultWidth
            // 
            lblDefaultWidth.AutoSize = true;
            lblDefaultWidth.Location = new Point(19, 56);
            lblDefaultWidth.Name = "lblDefaultWidth";
            lblDefaultWidth.Size = new Size(104, 15);
            lblDefaultWidth.TabIndex = 1;
            lblDefaultWidth.Text = "Default width (px):";
            // 
            // nudDefaultWidthPx
            // 
            nudDefaultWidthPx.Location = new Point(150, 53);
            nudDefaultWidthPx.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
            nudDefaultWidthPx.Minimum = new decimal(new int[] { 20, 0, 0, 0 });
            nudDefaultWidthPx.Name = "nudDefaultWidthPx";
            nudDefaultWidthPx.Size = new Size(86, 23);
            nudDefaultWidthPx.TabIndex = 2;
            nudDefaultWidthPx.Value = new decimal(new int[] { 120, 0, 0, 0 });
            // 
            // btnEqualize
            // 
            btnEqualize.Location = new Point(245, 52);
            btnEqualize.Name = "btnEqualize";
            btnEqualize.Size = new Size(94, 25);
            btnEqualize.TabIndex = 3;
            btnEqualize.Text = "Equalize";
            btnEqualize.UseVisualStyleBackColor = true;
            // 
            // dgvWidths
            // 
            dgvWidths.AllowUserToAddRows = false;
            dgvWidths.AllowUserToDeleteRows = false;
            dgvWidths.AllowUserToResizeRows = false;
            dgvWidths.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvWidths.Columns.AddRange(new DataGridViewColumn[] { colIndex, colWidthPx });
            dgvWidths.Location = new Point(19, 88);
            dgvWidths.MultiSelect = false;
            dgvWidths.Name = "dgvWidths";
            dgvWidths.RowHeadersVisible = false;
            dgvWidths.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgvWidths.Size = new Size(320, 165);
            dgvWidths.TabIndex = 4;
            // 
            // colIndex
            // 
            colIndex.HeaderText = "#";
            colIndex.Name = "colIndex";
            colIndex.ReadOnly = true;
            colIndex.Width = 40;
            // 
            // colWidthPx
            // 
            colWidthPx.HeaderText = "Width (px)";
            colWidthPx.Name = "colWidthPx";
            colWidthPx.Width = 120;
            // 
            // btnOk
            // 
            btnOk.Location = new Point(582, 288);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 28);
            btnOk.TabIndex = 2;
            btnOk.Text = "Insert";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(663, 288);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 28);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // InsertTableDialog
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(751, 321);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(grpWidth);
            Controls.Add(grpSize);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "InsertTableDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Insert table...";
            grpSize.ResumeLayout(false);
            grpSize.PerformLayout();
            grpWidth.ResumeLayout(false);
            grpWidth.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudDefaultWidthPx).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvWidths).EndInit();
            ResumeLayout(false);
        }
    }
}
