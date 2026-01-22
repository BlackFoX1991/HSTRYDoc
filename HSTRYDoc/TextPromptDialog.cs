namespace HSTRYDoc
{
    public partial class TextPromptDialog : Form
    {
        public string InputText => txtInput.Text ?? string.Empty;

        public TextPromptDialog(string title, string label, string initial = "")
        {
            InitializeComponent();

            Text = title;
            lblInfo.Text = label;
            txtInput.Text = initial ?? string.Empty;

            btnOk.Click += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(txtInput.Text))
                {
                    MessageBox.Show(this, "Text darf nicht leer sein.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            };

            btnCancel.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }
}
