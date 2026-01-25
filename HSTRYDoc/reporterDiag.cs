using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HSTRYDoc
{

    public partial class reporterDiag : Form
    {
        private readonly CancellationTokenSource _cts = new();
        private Button? _btnCancel;

        public CancellationToken Token => _cts.Token;

        public reporterDiag()
        {
            InitializeComponent();

            Text = "Working…";

            // Ensure prgStatus basics
            prgStatus.Minimum = 0;
            prgStatus.Value = 0;
            prgStatus.Style = ProgressBarStyle.Marquee;

            if (lblStatus != null)
                lblStatus.Text = "Working…";

            EnsureCancelButton();
        }

        private void EnsureCancelButton()
        {
            // Create a cancel button at runtime so this file compiles even if designer has none.
            // If you already have a Cancel button in designer, you can remove this method.
            _btnCancel = new Button
            {
                Text = "Cancel",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Size = new Size(90, 28)
            };

            // Place bottom-right with simple padding
            int pad = 12;
            _btnCancel.Location = new Point(ClientSize.Width - _btnCancel.Width - pad, ClientSize.Height - _btnCancel.Height - pad);
            _btnCancel.Click += (_, __) => _cts.Cancel();

            Controls.Add(_btnCancel);

            // Keep positioned on resize
            Resize += (_, __) =>
            {
                if (_btnCancel == null) return;
                _btnCancel.Location = new Point(ClientSize.Width - _btnCancel.Width - pad, ClientSize.Height - _btnCancel.Height - pad);
            };
        }

        private void ApplyProgress(UiProgress p)
        {
            if (!string.IsNullOrWhiteSpace(p.Message))
                lblStatus.Text = p.Message;

            if (p.Indeterminate)
            {
                prgStatus.Style = ProgressBarStyle.Marquee;
                return;
            }

            prgStatus.Style = ProgressBarStyle.Continuous;

            if (p.Maximum.HasValue)
                prgStatus.Maximum = Math.Max(1, p.Maximum.Value);

            if (p.Value.HasValue)
                prgStatus.Value = Math.Clamp(p.Value.Value, 0, prgStatus.Maximum);
        }

        public IProgress<UiProgress> CreateProgress()
            => new Progress<UiProgress>(ApplyProgress);

        // -------- Static runners (modal-like, but async-safe) --------

        public static Task RunAsync(IWin32Window owner, string title, Func<IProgress<UiProgress>, CancellationToken, Task> work)
            => RunAsync<object>(owner, title, async (p, t) => { await work(p, t); return new object(); });

        public static async Task<T> RunAsync<T>(IWin32Window owner, string title, Func<IProgress<UiProgress>, CancellationToken, Task<T>> work)
        {
            if (work == null) throw new ArgumentNullException(nameof(work));

            // Disable owner to mimic modal behavior, but keep UI thread free for await.
            if (owner is Control cOwner) cOwner.Enabled = false;

            using var dlg = new reporterDiag
            {
                StartPosition = FormStartPosition.CenterParent,
                Text = string.IsNullOrWhiteSpace(title) ? "Working…" : title,
                ShowInTaskbar = false,
                TopMost = true
            };

            // Show modeless (owner disabled)
            dlg.Show(owner);

            var progress = dlg.CreateProgress();

            try
            {
                // Always start with marquee unless work sets a determinate state
                progress.Report(new UiProgress { Message = "Working…", Indeterminate = true });

                // Run work
                T result = await work(progress, dlg.Token);
                return result;
            }
            finally
            {
                // Close dialog on UI thread
                try
                {
                    if (!dlg.IsDisposed)
                        dlg.Close();
                }
                catch { /* ignore */ }

                if (owner is Control cOwner2) cOwner2.Enabled = true;
            }
        }
    }
}
