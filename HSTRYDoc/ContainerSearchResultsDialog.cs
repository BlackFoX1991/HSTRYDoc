namespace HSTRYDoc
{
    public partial class ContainerSearchResultsDialog : Form
    {
        private readonly List<ContainerSearchHit> _hits = new();

        public ContainerSearchHit? SelectedHit { get; private set; }

        public ContainerSearchResultsDialog()
        {
            InitializeComponent();

            btnOpen.Click += (_, __) =>
            {
                SelectedHit = GetSelectedHit();
                if (SelectedHit == null) return;

                DialogResult = DialogResult.OK;
                Close();
            };

            btnClose.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            lvResults.DoubleClick += (_, __) => btnOpen.PerformClick();
        }

        public void SetResults(IEnumerable<ContainerSearchHit> hits)
        {
            _hits.Clear();
            _hits.AddRange(hits);

            lvResults.BeginUpdate();
            try
            {
                lvResults.Items.Clear();
                foreach (var h in _hits)
                {
                    var item = new ListViewItem(h.BlockTitle);
                    item.SubItems.Add(h.Snippet);
                    item.Tag = h;
                    lvResults.Items.Add(item);
                }
            }
            finally
            {
                lvResults.EndUpdate();
            }

            if (lvResults.Items.Count > 0)
                lvResults.Items[0].Selected = true;
        }

        private ContainerSearchHit? GetSelectedHit()
        {
            if (lvResults.SelectedItems.Count == 0) return null;
            return lvResults.SelectedItems[0].Tag as ContainerSearchHit;
        }
    }
}
