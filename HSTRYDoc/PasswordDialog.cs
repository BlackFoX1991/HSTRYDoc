// PasswordDialog.cs
using System;
using System.Windows.Forms;

namespace HSTRYDoc
{
    public sealed class PasswordDialog : Form
    {
        private readonly TextBox _tbPassword = new() { UseSystemPasswordChar = true, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
        private readonly TextBox _tbConfirm = new() { UseSystemPasswordChar = true, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
        private readonly Label _lblInfo = new() { AutoSize = true };
        private readonly Button _btnOk = new() { Text = "OK", DialogResult = DialogResult.OK };
        private readonly Button _btnCancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel };

        public string Password => _tbPassword.Text ?? string.Empty;

        public PasswordDialog(string title, string info, bool requireConfirm)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(380, requireConfirm ? 180 : 130);

            _lblInfo.Text = info;

            var lblPass = new Label { Text = "Password:", AutoSize = true };
            var lblConf = new Label { Text = "Confirm:", AutoSize = true, Visible = requireConfirm };
            _tbConfirm.Visible = requireConfirm;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = requireConfirm ? 4 : 3,
                Padding = new Padding(12),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            layout.Controls.Add(_lblInfo, 0, 0);
            layout.SetColumnSpan(_lblInfo, 2);

            layout.Controls.Add(lblPass, 0, 1);
            layout.Controls.Add(_tbPassword, 1, 1);

            if (requireConfirm)
            {
                layout.Controls.Add(lblConf, 0, 2);
                layout.Controls.Add(_tbConfirm, 1, 2);
            }

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            buttons.Controls.Add(_btnOk);
            buttons.Controls.Add(_btnCancel);

            layout.Controls.Add(buttons, 0, requireConfirm ? 3 : 2);
            layout.SetColumnSpan(buttons, 2);

            Controls.Add(layout);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            _btnOk.Click += (_, __) =>
            {
                if (requireConfirm && !string.Equals(_tbPassword.Text, _tbConfirm.Text, StringComparison.Ordinal))
                {
                    MessageBox.Show(this, "Passwords do not match.", "Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                if (string.IsNullOrWhiteSpace(_tbPassword.Text))
                {
                    MessageBox.Show(this, "Password must not be empty.", "Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };
        }
    }
}
