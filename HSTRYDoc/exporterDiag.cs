using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HSTRYDoc
{
    public partial class exporterDiag : Form
    {
        private enum ExportFormat { Pdf, Rtf, Txt }

        private readonly HSTRYContainer _container;
        private ExportFormat _format = ExportFormat.Pdf;

        // PDF: full file path. RTF/TXT: folder path.
        private string _outputPath = string.Empty;

        // Used for RTF -> plain text conversion
        private readonly RichTextBox _rtfToText = new RichTextBox();

        public exporterDiag(HSTRYContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            InitializeComponent();

            // Optional: make your dialog texts English at runtime without touching designer
            Text = "Export blocks...";
            grpFileFormat.Text = "File format";
            grpOutput.Text = "Output";
            label1.Text =
                "Select the blocks you want to export.\r\n" +
                "PDF exports into a single multi-page file.\r\n" +
                "RTF/TXT export creates multiple files in the chosen folder.";

            btnExport.Text = "Export";
            btnCancel.Text = "Cancel";

            // Default
            radioPdf.Checked = true;

            // Wire events
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            btnChoose.Click += (_, __) => ChooseOutput();
            btnExport.Click += async (_, __) => await ExportAsync();

            radioPdf.CheckedChanged += (_, __) => { if (radioPdf.Checked) OnFormatChanged(ExportFormat.Pdf); };
            radioRtf.CheckedChanged += (_, __) => { if (radioRtf.Checked) OnFormatChanged(ExportFormat.Rtf); };
            radioTxt.CheckedChanged += (_, __) => { if (radioTxt.Checked) OnFormatChanged(ExportFormat.Txt); };

            lvwBlocks.ItemChecked += (_, __) => UpdateExportButtonState();
            lvwBlocks.ItemSelectionChanged += (_, __) => UpdateExportButtonState();

            // Fill the list
            PopulateBlocks();

            // Progress bar initial
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

            if (_format == ExportFormat.Pdf)
                grpOutput.Text = "Output file (PDF)";
            else
                grpOutput.Text = "Output folder";
        }

        private void PopulateBlocks()
        {
            lvwBlocks.BeginUpdate();
            try
            {
                lvwBlocks.Items.Clear();

                for (int i = 0; i < _container.Blocks.Count; i++)
                {
                    var b = _container.Blocks[i];

                    string hashHex = Convert.ToHexString(_container.ComputeBlockHash(b));
                    string size = ByteFormat.ToHumanSize(b.StoredSizeBytes);

                    string created = b.CreatedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss:ffffff");
                    string changed = b.ModifiedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss:ffffff");

                    var item = new ListViewItem(b.Title);
                    item.SubItems.Add(hashHex);
                    item.SubItems.Add(size);
                    item.SubItems.Add(created);
                    item.SubItems.Add(changed);

                    item.Tag = i;         // store block index
                    item.Checked = false; // user chooses

                    lvwBlocks.Items.Add(item);
                }
            }
            finally
            {
                lvwBlocks.EndUpdate();
            }
        }

        private List<int> GetCheckedBlockIndices()
        {
            var indices = new List<int>();
            foreach (ListViewItem it in lvwBlocks.Items)
            {
                if(it is null) continue;
                if (!it.Checked) continue;
                if (it.Tag is int idx) indices.Add(idx);
            }
            indices.Sort();
            return indices;
        }

        private void UpdateExportButtonState()
        {
            bool hasSelection = GetCheckedBlockIndices().Count > 0;
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
                MessageBox.Show(this, "Please select at least one block.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(_outputPath))
            {
                MessageBox.Show(this, "Please choose an output location first.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // UI lock
            SetUiEnabled(false);

            // Progress bar
            prgExport.Visible = true;
            prgExport.Minimum = 0;
            prgExport.Maximum = indices.Count;
            prgExport.Value = 0;
            prgExport.Style = ProgressBarStyle.Continuous;

            try
            {
                // Run export without freezing UI
                var progress = new Progress<int>(v =>
                {
                    int val = Math.Max(prgExport.Minimum, Math.Min(prgExport.Maximum, v));
                    prgExport.Value = val;
                });

                await Task.Run(() =>
                {
                    switch (_format)
                    {
                        case ExportFormat.Pdf:
                            ExportPdf(_outputPath, indices, progress);
                            break;
                        case ExportFormat.Rtf:
                            ExportRtfFolder(_outputPath, indices, progress);
                            break;
                        case ExportFormat.Txt:
                            ExportTxtFolder(_outputPath, indices, progress);
                            break;
                    }
                });

                // done
                prgExport.Visible = false;
                MessageBox.Show(this, "Export completed successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                prgExport.Visible = false;
                MessageBox.Show(this, ex.Message, "Export error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            // Export depends on selection/output, will be re-evaluated
            btnExport.Enabled = enabled && btnExport.Enabled;
        }

        // ----------------------------
        // RTF export (multiple files)
        // ----------------------------
        private void ExportRtfFolder(string folder, List<int> indices, IProgress<int> progress)
        {
            Directory.CreateDirectory(folder);

            int done = 0;
            foreach (int i in indices)
            {
                var b = _container.Blocks[i];
                string rtf = _container.GetRtfDocument(i) ?? string.Empty;

                string safe = MakeSafeFileName($"{i + 1:D4}_{b.Title}");
                string path = Path.Combine(folder, safe + ".rtf");

                File.WriteAllText(path, rtf, Encoding.UTF8);

                done++;
                progress.Report(done);
            }
        }

        // ----------------------------
        // TXT export (multiple files)
        // ----------------------------
        private void ExportTxtFolder(string folder, List<int> indices, IProgress<int> progress)
        {
            Directory.CreateDirectory(folder);

            int done = 0;
            foreach (int i in indices)
            {
                var b = _container.Blocks[i];
                string rtf = _container.GetRtfDocument(i) ?? string.Empty;

                // RTF -> plain text
                _rtfToText.Rtf = rtf;
                string text = _rtfToText.Text ?? string.Empty;

                string safe = MakeSafeFileName($"{i + 1:D4}_{b.Title}");
                string path = Path.Combine(folder, safe + ".txt");

                File.WriteAllText(path, text, Encoding.UTF8);

                done++;
                progress.Report(done);
            }
        }

        // ----------------------------
        // PDF export (single multi-page)
        // Each block starts on a new page.
        // Uses Windows "Microsoft Print to PDF".
        // ----------------------------
        private void ExportPdf(string pdfPath, List<int> indices, IProgress<int> progress)
        {
            if (indices.Count == 0) return;

            // Ensure target folder exists
            Directory.CreateDirectory(Path.GetDirectoryName(pdfPath) ?? Environment.CurrentDirectory);

            using var doc = new PrintDocument();

            // Setup printer
            doc.PrinterSettings.PrinterName = "Microsoft Print to PDF";
            if (!doc.PrinterSettings.IsValid)
                throw new InvalidOperationException("The 'Microsoft Print to PDF' printer is not available on this system.");

            doc.PrinterSettings.PrintToFile = true;
            doc.PrinterSettings.PrintFileName = pdfPath;

            // Print state
            int blockPos = 0;
            int charPos = 0;
            bool loadedBlock = false;

            var rtb = new RichTextBox();

            doc.BeginPrint += (_, __) =>
            {
                blockPos = 0;
                charPos = 0;
                loadedBlock = false;
                RichTextBoxPrintHelper.FormatRangeDone(rtb);
            };

            doc.EndPrint += (_, __) =>
            {
                RichTextBoxPrintHelper.FormatRangeDone(rtb);
                rtb.Dispose();
            };

            doc.PrintPage += (_, e) =>
            {
                if (blockPos >= indices.Count)
                {
                    e.HasMorePages = false;
                    return;
                }

                int blockIndex = indices[blockPos];

                if (!loadedBlock)
                {
                    rtb.Rtf = _container.GetRtfDocument(blockIndex) ?? string.Empty;
                    charPos = 0;
                    loadedBlock = true;
                }

                // print current page chunk
                charPos = RichTextBoxPrintHelper.FormatRange(rtb, e, charPos, rtb.TextLength);

                if (charPos < rtb.TextLength)
                {
                    // more pages for same block
                    e.HasMorePages = true;
                    return;
                }

                // block finished -> progress increments once per block
                blockPos++;
                loadedBlock = false;
                RichTextBoxPrintHelper.FormatRangeDone(rtb);

                progress.Report(blockPos);

                // force a new page if there is another block
                e.HasMorePages = blockPos < indices.Count;
            };

            // Print (this uses the configured PrintToFile path)
            doc.Print();
        }

        private static string MakeSafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "block";

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);

            foreach (char c in name)
            {
                sb.Append(invalid.Contains(c) ? '_' : c);
            }

            string s = sb.ToString().Trim();
            if (s.Length > 120) s = s.Substring(0, 120);
            if (string.IsNullOrWhiteSpace(s)) s = "block";
            return s;
        }
    }
}
