// TextPromptDialog.cs
using System;
using System.Windows.Forms;

namespace HSTRYDoc
{
    public sealed class TextPromptDialog : Form
    {
        private readonly TextBox _tb = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
        private readonly Label _lbl = new() { AutoSize = true };
        private readonly Button _ok = new() { Text = "OK", DialogResult = DialogResult.OK };
        private readonly Button _cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel };

        public string InputText => _tb.Text ?? string.Empty;

        public TextPromptDialog(string title, string label, string initial = "")
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(420, 140);

            _lbl.Text = label;
            _tb.Text = initial;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(12),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            layout.Controls.Add(_lbl, 0, 0);
            layout.SetColumnSpan(_lbl, 2);

            layout.Controls.Add(new Label { Text = "Text:", AutoSize = true }, 0, 1);
            layout.Controls.Add(_tb, 1, 1);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            buttons.Controls.Add(_ok);
            buttons.Controls.Add(_cancel);

            layout.Controls.Add(buttons, 0, 2);
            layout.SetColumnSpan(buttons, 2);

            Controls.Add(layout);

            AcceptButton = _ok;
            CancelButton = _cancel;

            _ok.Click += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(_tb.Text))
                {
                    MessageBox.Show(this, "Text must not be empty.", title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };
        }
    }
}
