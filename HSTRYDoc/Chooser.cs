using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HSTRYDoc
{
    public partial class Chooser : Form
    {
        public string? SelectedRecentPath { get; private set; }

        public Chooser()
        {
            InitializeComponent();

            AcceptButton = btnNew;
            CancelButton = btnExit;

            // Open recent on double click
            lvwRecent.DoubleClick += (_, __) => OpenSelectedRecent();
            // Optional: Enter key also opens
            lvwRecent.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    OpenSelectedRecent();
                    e.Handled = true;
                }
            };
        }

        public void SetRecent(IEnumerable<RecentFileEntry> items)
        {
            lvwRecent.BeginUpdate();
            try
            {
                lvwRecent.Items.Clear();

                foreach (var r in items)
                {
                    var it = new ListViewItem(r.FileName);
                    it.SubItems.Add(r.FilePath);
                    it.SubItems.Add(r.LastUsedLocalFormatted);
                    it.Tag = r.FilePath;
                    lvwRecent.Items.Add(it);
                }
            }
            finally
            {
                lvwRecent.EndUpdate();
            }
        }

        private void OpenSelectedRecent()
        {
            if (lvwRecent.SelectedItems.Count == 0) return;

            var it = lvwRecent.SelectedItems[0];
            var path = it.Tag as string;

            if (string.IsNullOrWhiteSpace(path)) return;

            SelectedRecentPath = path;

            // Reuse your existing "Open" flow (DialogResult.No)
            DialogResult = DialogResult.No;
            Close();
        }
    }
}
