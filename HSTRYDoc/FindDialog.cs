using System.ComponentModel;

namespace HSTRYDoc
{
    public partial class FindDialog : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string QueryText
        {
            get => txtQuery.Text ?? string.Empty;
            set => txtQuery.Text = value ?? string.Empty;
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MatchCase
        {
            get => chkMatchCase.Checked;
            set => chkMatchCase.Checked = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool WholeWord
        {
            get => chkWholeWord.Checked;
            set => chkWholeWord.Checked = value;
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Wrap
        {
            get => chkWrap.Checked;
            set => chkWrap.Checked = value;
        }

        public FindDialog()
        {
            InitializeComponent();

            btnFindNext.Click += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(QueryText))
                {
                    MessageBox.Show(this, "Suchtext ist leer.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            btnClose.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            AcceptButton = btnFindNext;
            CancelButton = btnClose;
        }
    }
}
