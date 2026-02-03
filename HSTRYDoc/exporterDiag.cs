using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace HSTRYDoc
{
    public partial class exporterDiag : Form
    {
        private enum ExportFormat { Pdf, Rtf, Txt }

        private readonly HSTRYContainer _container;
        private ExportFormat _format = ExportFormat.Pdf;

        // PDF: full file path. RTF/TXT: folder path.
        private string _outputPath = string.Empty;

        // Track checked blocks in O(1), avoids scanning list on every UI change
        private readonly HashSet<int> _checked = new();

        public exporterDiag(HSTRYContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            InitializeComponent();

            // English UI strings
            Text = "Export blocks...";
            grpFileFormat.Text = "File format";
            grpOutput.Text = "Output";
            label1.Text =
                "Select the blocks you want to export.\r\n" +
                "PDF exports into a single multi-page file.\r\n" +
                "RTF/TXT export creates multiple files in the chosen folder.\r\n" +
                "Hashes are computed on demand to keep the dialog responsive.";

            // Columns (optional rename; safe even if already set)
            colName.Text = "Block";
            colHash.Text = "Hash (on demand)";
            colSize.Text = "Size";
            colCreated.Text = "Created";
            colChanged.Text = "Modified";

            btnExport.Text = "Export";
            btnCancel.Text = "Cancel";

            radioPdf.Checked = true;

            // Wire events
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            btnChoose.Click += (_, __) => ChooseOutput();
            btnExport.Click += async (_, __) => await ExportAsync();

            radioPdf.CheckedChanged += (_, __) => { if (radioPdf.Checked) OnFormatChanged(ExportFormat.Pdf); };
            radioRtf.CheckedChanged += (_, __) => { if (radioRtf.Checked) OnFormatChanged(ExportFormat.Rtf); };
            radioTxt.CheckedChanged += (_, __) => { if (radioTxt.Checked) OnFormatChanged(ExportFormat.Txt); };

            // Maintain checked set (fast)
            lvwBlocks.ItemChecked += (_, e) =>
            {
                if (e.Item?.Tag is int idx)
                {
                    if (e.Item.Checked) _checked.Add(idx);
                    else _checked.Remove(idx);
                }
                UpdateExportButtonState();
            };

            // Hash on-demand when selection changes (no upfront hashing)
            lvwBlocks.ItemSelectionChanged += async (_, e) =>
            {
                if (e.IsSelected && e.Item != null)
                    await EnsureHashForListItemAsync(e.Item);
            };

            // Fill list fast
            PopulateBlocksFast();

            // Designer progress bar unused (we use reporterDiag)
            prgExport.Visible = false;
            prgExport.Minimum = 0;
            prgExport.Value = 0;

            ResetOutput();
            UpdateExportButtonState();
        }

        private void OnFormatChanged(ExportFormat fmt)
        {
            _format = fmt;
            ResetOutput();
            UpdateExportButtonState();
        }

        private void ResetOutput()
        {
            _outputPath = string.Empty;
            txtOutput.Text = string.Empty;

            grpOutput.Text = (_format == ExportFormat.Pdf)
                ? "Output file (PDF)"
                : "Output folder";
        }

        // IMPORTANT: No hash computation here (prevents UI freeze on large containers)
        private void PopulateBlocksFast()
        {
            lvwBlocks.BeginUpdate();
            try
            {
                lvwBlocks.Items.Clear();
                _checked.Clear();

                for (int i = 0; i < _container.Blocks.Count; i++)
                {
                    var b = _container.Blocks[i];

                    // Skip restricted blocks (independent of container version).
                    // Validate() hydrates inaccessible blocks with "<restricted>".
                    if (string.Equals(b.Title, "<restricted>", StringComparison.Ordinal))
                        continue;

                    string hashHex = string.Empty; // computed on demand
                    string size = ByteFormat.ToHumanSize(b.StoredSizeBytes);

                    string created = b.CreatedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss:ffffff");
                    string changed = b.ModifiedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss:ffffff");

                    var item = new ListViewItem(b.Title);
                    item.SubItems.Add(hashHex);
                    item.SubItems.Add(size);
                    item.SubItems.Add(created);
                    item.SubItems.Add(changed);

                    item.Tag = i;
                    item.Checked = false;

                    lvwBlocks.Items.Add(item);
                }
            }
            finally
            {
                lvwBlocks.EndUpdate();
            }
        }

        private async Task EnsureHashForListItemAsync(ListViewItem item)
        {
            if (item == null) return;
            if (item.SubItems.Count < 2) return;

            string cur = item.SubItems[1].Text ?? string.Empty;

            // Already computed or currently computing?
            if (!string.IsNullOrWhiteSpace(cur) && !string.Equals(cur, "Computing…", StringComparison.Ordinal))
                return;
            if (string.Equals(cur, "Computing…", StringComparison.Ordinal))
                return;

            if (item.Tag is not int idx) return;
            if (idx < 0 || idx >= _container.Blocks.Count) return;

            item.SubItems[1].Text = "Computing…";

            try
            {
                string hex = await Task.Run(() =>
                {
                    byte[] h = _container.ComputeBlockHash(_container.Blocks[idx]);
                    return Convert.ToHexString(h);
                });

                item.SubItems[1].Text = hex;
            }
            catch
            {
                item.SubItems[1].Text = string.Empty;
            }
        }

        private List<int> GetCheckedBlockIndices()
        {
            var indices = _checked.ToList();
            indices.Sort();
            return indices;
        }

        private void UpdateExportButtonState()
        {
            bool hasSelection = _checked.Count > 0;
            bool hasOutput = !string.IsNullOrWhiteSpace(_outputPath);

            btnExport.Enabled = hasSelection && hasOutput;
        }

        private void ChooseOutput()
        {
            if (_format == ExportFormat.Pdf)
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "PDF (*.pdf)|*.pdf|All files (*.*)|*.*",
                    DefaultExt = "pdf",
                    AddExtension = true,
                    FileName = "export.pdf"
                };

                if (sfd.ShowDialog(this) != DialogResult.OK)
                    return;

                _outputPath = sfd.FileName;
                txtOutput.Text = _outputPath;
                UpdateExportButtonState();
                return;
            }

            using var fbd = new FolderBrowserDialog
            {
                Description = "Select an output folder"
            };

            if (fbd.ShowDialog(this) != DialogResult.OK)
                return;

            _outputPath = fbd.SelectedPath;
            txtOutput.Text = _outputPath;
            UpdateExportButtonState();
        }

        private async Task ExportAsync()
        {
            var indices = GetCheckedBlockIndices();
            if (indices.Count == 0)
            {
                MessageBox.Show(this, "Please select at least one block.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(_outputPath))
            {
                MessageBox.Show(this, "Please choose an output location first.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetUiEnabled(false);

            try
            {
                await reporterDiag.RunAsync(
                    owner: this,
                    title: "Export",
                    work: async (progress, token) =>
                    {
                        progress.Report(new UiProgress
                        {
                            Message = "Preparing export…",
                            Indeterminate = false,
                            Maximum = indices.Count,
                            Value = 0
                        });

                        // RichTextBox/PrintDocument work best on STA to avoid hangs.
                        await StaTask.RunAsync(() =>
                        {
                            token.ThrowIfCancellationRequested();

                            switch (_format)
                            {
                                case ExportFormat.Pdf:
                                    ExportPdf(_outputPath, indices, progress, token);
                                    break;
                                case ExportFormat.Rtf:
                                    ExportRtfFolder(_outputPath, indices, progress, token);
                                    break;
                                case ExportFormat.Txt:
                                    ExportTxtFolder(_outputPath, indices, progress, token);
                                    break;
                            }
                        }, token);
                    });

                MessageBox.Show(this, "Export completed successfully.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(this, "Export cancelled.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetUiEnabled(true);
                UpdateExportButtonState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Export error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetUiEnabled(true);
                UpdateExportButtonState();
            }
        }

        private void SetUiEnabled(bool enabled)
        {
            lvwBlocks.Enabled = enabled;
            grpFileFormat.Enabled = enabled;
            grpOutput.Enabled = enabled;

            btnChoose.Enabled = enabled;
            btnCancel.Enabled = enabled;

            // Re-evaluate export availability properly (selection + output).
            if (enabled)
                UpdateExportButtonState();
            else
                btnExport.Enabled = false;
        }


        // ----------------------------
        // RTF export (multiple files)
        // ----------------------------
        private void ExportRtfFolder(string folder, List<int> indices, IProgress<UiProgress> progress, CancellationToken token)
        {
            Directory.CreateDirectory(folder);

            int written = 0;

            for (int done = 0; done < indices.Count; done++)
            {
                token.ThrowIfCancellationRequested();

                int i = indices[done];
                var b = _container.Blocks[i];

                progress.Report(new UiProgress
                {
                    Message = $"Exporting RTF {done + 1}/{indices.Count}: {b.Title}",
                    Value = done + 1,
                    Maximum = indices.Count,
                    Indeterminate = false
                });

                string rtf;
                try
                {
                    rtf = _container.GetRtfDocument(i) ?? string.Empty;
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip restricted blocks
                    continue;
                }

                string safe = MakeSafeFileName($"{i + 1:D4}_{b.Title}");
                string path = Path.Combine(folder, safe + ".rtf");

                File.WriteAllText(path, rtf, Encoding.UTF8);
                written++;
            }

            if (written == 0)
                throw new InvalidOperationException("No readable blocks were exported.");
        }

        // ----------------------------
        // TXT export (multiple files)
        // ----------------------------
        private void ExportTxtFolder(string folder, List<int> indices, IProgress<UiProgress> progress, CancellationToken token)
        {
            Directory.CreateDirectory(folder);

            using var rtb = new RichTextBox();
            int written = 0;

            for (int done = 0; done < indices.Count; done++)
            {
                token.ThrowIfCancellationRequested();

                int i = indices[done];
                var b = _container.Blocks[i];

                progress.Report(new UiProgress
                {
                    Message = $"Exporting TXT {done + 1}/{indices.Count}: {b.Title}",
                    Value = done + 1,
                    Maximum = indices.Count,
                    Indeterminate = false
                });

                string rtf;
                try
                {
                    rtf = _container.GetRtfDocument(i) ?? string.Empty;
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip restricted blocks
                    continue;
                }

                rtb.Rtf = rtf;
                string text = rtb.Text ?? string.Empty;

                string safe = MakeSafeFileName($"{i + 1:D4}_{b.Title}");
                string path = Path.Combine(folder, safe + ".txt");

                File.WriteAllText(path, text, Encoding.UTF8);
                written++;
            }

            if (written == 0)
                throw new InvalidOperationException("No readable blocks were exported.");
        }

        // ----------------------------
        // PDF export (single multi-page)
        // Each block starts on a new page.
        // Uses Windows "Microsoft Print to PDF".
        // ----------------------------
        private void ExportPdf(string pdfPath, List<int> indices, IProgress<UiProgress> progress, CancellationToken token)
        {
            if (indices.Count == 0) return;

            Directory.CreateDirectory(Path.GetDirectoryName(pdfPath) ?? Environment.CurrentDirectory);

            using var doc = new PrintDocument();

            doc.PrinterSettings.PrinterName = "Microsoft Print to PDF";
            if (!doc.PrinterSettings.IsValid)
                throw new InvalidOperationException("The 'Microsoft Print to PDF' printer is not available on this system.");

            doc.PrinterSettings.PrintToFile = true;
            doc.PrinterSettings.PrintFileName = pdfPath;

            int blockPos = 0;
            int charPos = 0;
            bool loadedBlock = false;
            bool cancelled = false;

            var rtb = new RichTextBox();

            doc.BeginPrint += (_, __) =>
            {
                blockPos = 0;
                charPos = 0;
                loadedBlock = false;
                cancelled = false;
                RichTextBoxPrintHelper.FormatRangeDone(rtb);
            };

            doc.EndPrint += (_, __) =>
            {
                RichTextBoxPrintHelper.FormatRangeDone(rtb);
                rtb.Dispose();
            };

            doc.PrintPage += (_, e) =>
            {
                if (token.IsCancellationRequested)
                {
                    cancelled = true;
                    e.HasMorePages = false;
                    return;
                }

                // Find next readable block (skip restricted)
                while (blockPos < indices.Count && !loadedBlock)
                {
                    int blockIndex = indices[blockPos];
                    var b = _container.Blocks[blockIndex];

                    progress.Report(new UiProgress
                    {
                        Message = $"Exporting PDF {blockPos + 1}/{indices.Count}: {b.Title}",
                        Value = blockPos + 1,
                        Maximum = indices.Count,
                        Indeterminate = false
                    });

                    try
                    {
                        rtb.Rtf = _container.GetRtfDocument(blockIndex) ?? string.Empty;
                        charPos = 0;
                        loadedBlock = true;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip restricted block
                        blockPos++;
                        loadedBlock = false;
                        RichTextBoxPrintHelper.FormatRangeDone(rtb);
                    }
                }

                if (blockPos >= indices.Count)
                {
                    e.HasMorePages = false;
                    return;
                }

                // Print current loaded block
                charPos = RichTextBoxPrintHelper.FormatRange(rtb, e, charPos, rtb.TextLength);

                if (charPos < rtb.TextLength)
                {
                    e.HasMorePages = true;
                    return;
                }

                // Finished this block
                blockPos++;
                loadedBlock = false;
                RichTextBoxPrintHelper.FormatRangeDone(rtb);

                // There might be more readable blocks
                e.HasMorePages = blockPos < indices.Count;
            };

            doc.Print();

            if (cancelled)
                throw new OperationCanceledException(token);
        }

        private static string MakeSafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "block";

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);

            foreach (char c in name)
                sb.Append(invalid.Contains(c) ? '_' : c);

            string s = sb.ToString().Trim();
            if (s.Length > 120) s = s.Substring(0, 120);
            if (string.IsNullOrWhiteSpace(s)) s = "block";
            return s;
        }

        private void tsSelectAllBlocks_Click(object sender, EventArgs e)
        {
            if (lvwBlocks.Items.Count == 0)
                return;
            lvwBlocks.BeginUpdate();
            try
            {
                foreach (ListViewItem item in lvwBlocks.Items)
                    item.Checked = true;
            }
            finally
            {
                lvwBlocks.EndUpdate();
            }

        }

        private void tsDiscardSelection_Click(object sender, EventArgs e)
        {
            if (lvwBlocks.Items.Count == 0)
                return;
            lvwBlocks.BeginUpdate();
            try
            {
                foreach (ListViewItem item in lvwBlocks.Items)
                    item.Checked = false;
            }
            finally
            {
                lvwBlocks.EndUpdate();
            }
        }
    }
}
