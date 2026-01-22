// hsMain.cs (FULL, inline)
// - Chooser is shown on startup if no valid "Open with..." file was loaded
// - NO automatic creation of a new container on startup
// - ALL user-facing texts/messages are English (logic unchanged)

using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace HSTRYDoc
{
    public partial class hsMain : Form
    {
        private HSTRYDoc.colorPicker? _colorPopup;

        // ---- Container/Editor State ----
        private HSTRYContainer? _container;
        private string? _containerPath;

        private int _currentBlockIndex = -1;
        private bool _loadingBlockIntoEditor = false;
        private bool _blockDirty = false;
        private bool _containerDirty = false;

        // ---- Search State ----
        private string _lastFindText = string.Empty;
        private bool _lastFindMatchCase = false;
        private bool _lastFindWholeWord = false;
        private bool _lastFindWrap = true;

        public hsMain()
        {
            InitializeComponent();
        }

        private void hsMain_Load(object sender, EventArgs e)
        {
            WireUiEvents();

            // Startup: "Open with..." OR show Chooser (do NOT auto-create)
            if (!TryOpenFromCommandLineOrShell())
            {
                if (!RunChooserStartup())
                {
                    Close();
                    return;
                }
            }

            UpdateUiState();
            UpdateRtfUiFromSelection();
        }

        // ============================================================
        // Chooser Startup
        // ============================================================
        private bool RunChooserStartup()
        {
            // Show chooser until a container is created/loaded or the user exits
            while (_container == null)
            {
                using var chooser = new Chooser
                {
                    StartPosition = FormStartPosition.CenterParent
                };

                var dr = chooser.ShowDialog(this);

                // Exit (DialogResult.Cancel)
                if (dr == DialogResult.Cancel)
                    return false;

                // New (DialogResult.Yes)
                if (dr == DialogResult.Yes)
                {
                    if (UiCreateNewContainer(initialStartup: true))
                        return true;

                    // user cancelled -> show chooser again
                    continue;
                }

                // Open (DialogResult.No)
                if (dr == DialogResult.No)
                {
                    if (TryOpenContainerInteractive())
                        return true;

                    // user cancelled / invalid / error -> show chooser again
                    continue;
                }

                // Fallback: close
                return false;
            }

            return true;
        }

        // ============================================================
        // Startup Open-With
        // ============================================================
        private bool TryOpenFromCommandLineOrShell()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length < 2) return false;

                string? path = args.Skip(1).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));
                if (string.IsNullOrWhiteSpace(path)) return false;

                if (!LooksLikeHstryFile(path)) return false;

                return OpenContainerFromPath(path);
            }
            catch
            {
                return false;
            }
        }

        private static bool LooksLikeHstryFile(string path)
        {
            // Minimal validation: magic bytes at file start
            try
            {
                using var fs = File.OpenRead(path);
                if (fs.Length < Global.FileMagic.Length) return false;

                byte[] buf = new byte[Global.FileMagic.Length];
                int read = fs.Read(buf, 0, buf.Length);
                if (read != buf.Length) return false;

                return buf.SequenceEqual(Global.FileMagic);
            }
            catch
            {
                return false;
            }
        }

        private bool OpenContainerFromPath(string path)
        {
            using var pwd = new PasswordDialog("Open container", "Enter container password:", requireConfirm: false);
            if (pwd.ShowDialog(this) != DialogResult.OK) return false;

            try
            {
                var c = HSTRYContainer.Load(path, pwd.Password);

                _container?.CloseKeyMaterial();
                _container = c;
                _containerPath = path;

                _currentBlockIndex = -1;
                _blockDirty = false;
                _containerDirty = false;

                RefreshBlockList();
                ClearEditor();
                UpdateUiState();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Open container", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ============================================================
        // UI wiring (Designer: RTF-Toolbar has no events -> wired here)
        // ============================================================
        private void WireUiEvents()
        {
            // Top Toolstrip
            newBlockToolStripButton.Click += (_, __) => UiNewBlock();
            openContainerToolStripButton.Click += (_, __) => UiOpenContainer();
            saveContainerToolStripButton.Click += (_, __) => UiSaveContainer();

            // File Menu
            newToolStripMenuItem.Click += (_, __) => UiNewBlock();
            openContainerToolStripMenuItem.Click += (_, __) => UiOpenContainer();
            saveContainerToolStripMenuItem.Click += (_, __) => UiSaveContainer();
            saveContainerAsToolStripMenuItem.Click += (_, __) => UiSaveContainerAs();

            // Tools Menu: Search
            searchInBlockToolStripMenuItem.Click += (_, __) => UiSearchInBlock();
            searchInContainerToolStripMenuItem.Click += (_, __) => UiSearchInContainer();

            // Blocks context menu
            newBlockToolStripMenuItem.Click += (_, __) => UiNewBlock();
            renameBlockToolStripMenuItem.Click += (_, __) => UiRenameBlock();

            // ListView selection
            lvwBlocks.SelectedIndexChanged += (_, __) => UiSelectBlockFromList();
            lvwBlocks.DoubleClick += (_, __) => UiSelectBlockFromList();

            // RichTextBox context menu clipboard
            copyToolStripMenuItem1.Click += (_, __) => rtfMainText.Copy();
            pasteToolStripMenuItem1.Click += (_, __) => rtfMainText.Paste();
            cutToolStripMenuItem1.Click += (_, __) => rtfMainText.Cut();
            selectAllToolStripMenuItem1.Click += (_, __) => rtfMainText.SelectAll();

            // RichTextBox context menu format
            boldToolStripMenuItem.Click += (_, __) => ToggleSelectionStyle(FontStyle.Bold);
            italicToolStripMenuItem.Click += (_, __) => ToggleSelectionStyle(FontStyle.Italic);
            underlineToolStripMenuItem.Click += (_, __) => ToggleSelectionStyle(FontStyle.Underline);
            strikeToolStripMenuItem.Click += (_, __) => ToggleSelectionStyle(FontStyle.Strikeout);

            // Context menu colors
            forecolorToolStripMenuItem.Click += (_, __) => foreColorToolButton_Click(foreColorToolButton, EventArgs.Empty);
            textBackgroundcolorToolStripMenuItem.Click += (_, __) => toolStripButton1_Click(backgroundColorToolButton, EventArgs.Empty);

            // RTF toolbar format
            boldToolButton.Click += (_, __) => ToggleSelectionStyle(FontStyle.Bold);
            ItalicToolButton.Click += (_, __) => ToggleSelectionStyle(FontStyle.Italic);
            UnderlineToolButton.Click += (_, __) => ToggleSelectionStyle(FontStyle.Underline);
            StrikeTroughToolButton.Click += (_, __) => ToggleSelectionStyle(FontStyle.Strikeout);

            // Font size combo
            FontSizeComboBox.TextUpdate += FontSizeComboBox_TextUpdate;
            FontSizeComboBox.Click += FontSizeComboBox_Click;

            // Color toolbar
            foreColorToolButton.Click += foreColorToolButton_Click;
            backgroundColorToolButton.Click += toolStripButton1_Click;

            // Clipboard toolbar
            toolButtonCopy.Click += toolButtonCopy_Click;
            toolButtonPaste.Click += toolButtonPaste_Click;
            toolButtonCut.Click += toolButtonCut_Click;
            toolButtonSelectAll.Click += toolButtonSelectAll_Click;

            // Editor dirty tracking
            rtfMainText.TextChanged += (_, __) =>
            {
                if (_loadingBlockIntoEditor) return;
                if (_container == null) return;
                if (_currentBlockIndex < 0) return;

                _blockDirty = true;
                _containerDirty = true;
                UpdateUiState();
            };

            // Update toolbar state when selection changes
            rtfMainText.SelectionChanged += (_, __) => UpdateRtfUiFromSelection();

            // Shortcuts in editor
            rtfMainText.KeyDown += RtfMainText_KeyDown;

            // Closing
            FormClosing += hsMain_FormClosing;
        }

        // ============================================================
        // UI state
        // ============================================================
        private void UpdateUiState()
        {
            bool hasContainer = _container != null;

            saveContainerToolStripButton.Enabled = hasContainer;
            saveContainerToolStripMenuItem.Enabled = hasContainer;
            saveContainerAsToolStripMenuItem.Enabled = hasContainer;

            newBlockToolStripButton.Enabled = hasContainer;
            newToolStripMenuItem.Enabled = hasContainer;
            newBlockToolStripMenuItem.Enabled = hasContainer;

            renameBlockToolStripMenuItem.Enabled = hasContainer && _currentBlockIndex >= 0;

            ContainerSizeLabel.Visible = hasContainer;
            ContainerSizeLabel.Text = hasContainer
                ? $"Container: {ByteFormat.ToHumanSize(_container!.GetStoredSizeBytes())}"
                : "<Container_Size>";

            Text = _containerDirty ? $"{Global.AppName} *" : Global.AppName;
        }

        private void hsMain_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!MaybeCommitCurrentBlock())
            {
                e.Cancel = true;
                return;
            }

            if (_containerDirty)
            {
                var res = MessageBox.Show(
                    this,
                    "The container has unsaved changes. Save now?",
                    Global.AppName,
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (res == DialogResult.Yes)
                {
                    if (!UiSaveContainer())
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            _container?.CloseKeyMaterial();
        }

        // ============================================================
        // Container operations
        // ============================================================
        private bool UiCreateNewContainer(bool initialStartup = false)
        {
            using var pwd = new PasswordDialog("Create container", "Choose a password for the new container:", requireConfirm: true);
            if (pwd.ShowDialog(this) != DialogResult.OK)
                return false;

            try
            {
                _container?.CloseKeyMaterial();
                _container = HSTRYContainer.CreateNew(pwd.Password, iterations: 300_000, encoding: Global.CurrentEditorEncoding);
                _containerPath = null;

                _currentBlockIndex = -1;
                _loadingBlockIntoEditor = false;
                _blockDirty = false;
                _containerDirty = true;

                lvwBlocks.Items.Clear();
                ClearEditor();
                UpdateUiState();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Create container", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // bool helper for Chooser (Open)
        private bool TryOpenContainerInteractive()
        {
            if (!MaybeCommitCurrentBlock()) return false;

            if (_containerDirty)
            {
                var res = MessageBox.Show(
                    this,
                    "The container has unsaved changes. Continue without saving?",
                    "Open container",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (res != DialogResult.Yes) return false;
            }

            using var ofd = new OpenFileDialog
            {
                Filter = "HSTRY Container (*.hstry)|*.hstry|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (ofd.ShowDialog(this) != DialogResult.OK) return false;

            if (!LooksLikeHstryFile(ofd.FileName))
            {
                MessageBox.Show(this, "The selected file is not a valid HSTRY container file.", "Open container",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return OpenContainerFromPath(ofd.FileName);
        }

        private void UiOpenContainer()
        {
            _ = TryOpenContainerInteractive();
        }

        private bool UiSaveContainer()
        {
            if (_container == null) return false;
            if (!MaybeCommitCurrentBlock()) return false;

            if (string.IsNullOrWhiteSpace(_containerPath))
                return UiSaveContainerAs();

            try
            {
                _container.Save(_containerPath!);
                _containerDirty = false;
                UpdateUiState();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Save container", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool UiSaveContainerAs()
        {
            if (_container == null) return false;
            if (!MaybeCommitCurrentBlock()) return false;

            using var sfd = new SaveFileDialog
            {
                Filter = "HSTRY Container (*.hstry)|*.hstry|All files (*.*)|*.*",
                DefaultExt = "hstry",
                AddExtension = true,
                FileName = string.IsNullOrWhiteSpace(_containerPath) ? "container.hstry" : Path.GetFileName(_containerPath)
            };

            if (sfd.ShowDialog(this) != DialogResult.OK) return false;

            try
            {
                _container.Save(sfd.FileName);
                _containerPath = sfd.FileName;
                _containerDirty = false;
                UpdateUiState();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Save container as", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ============================================================
        // Blocks / Editor
        // ============================================================
        private void UiNewBlock()
        {
            if (_container == null)
            {
                if (!UiCreateNewContainer()) return;
            }

            if (!MaybeCommitCurrentBlock()) return;

            string title = _container!.GenerateUniqueTitle();
            using (var dlg = new TextPromptDialog("New block", "Block name:", title))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                title = dlg.InputText;
            }

            string emptyRtf;
            _loadingBlockIntoEditor = true;
            try
            {
                rtfMainText.Clear();
                emptyRtf = rtfMainText.Rtf ?? string.Empty;
            }
            finally
            {
                _loadingBlockIntoEditor = false;
            }

            try
            {
                var b = _container!.AddRtfDocument(title, emptyRtf);
                _containerDirty = true;

                RefreshBlockList(selectIndex: b.Index);
                LoadBlockIntoEditor(b.Index);
                UpdateUiState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "New block", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UiRenameBlock()
        {
            if (_container == null) return;

            int idx = GetSelectedBlockIndex();
            if (idx < 0) return;

            if (!MaybeCommitCurrentBlock()) return;

            var b = _container.Blocks[idx];

            using var dlg = new TextPromptDialog("Rename block", "New name:", b.Title);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                _container.RenameBlock(idx, dlg.InputText);
                _containerDirty = true;

                RefreshBlockList(selectIndex: idx);
                LoadBlockIntoEditor(idx);
                UpdateUiState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Rename block", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UiSelectBlockFromList()
        {
            if (_container == null) return;

            int idx = GetSelectedBlockIndex();
            if (idx < 0) return;

            if (idx == _currentBlockIndex) return;

            if (!MaybeCommitCurrentBlock())
            {
                SelectListIndex(_currentBlockIndex);
                return;
            }

            LoadBlockIntoEditor(idx);
            UpdateUiState();
        }

        private void LoadBlockIntoEditor(int index)
        {
            if (_container == null) return;

            try
            {
                _loadingBlockIntoEditor = true;

                string rtf = _container.GetRtfDocument(index);
                rtfMainText.Rtf = rtf ?? string.Empty;

                _currentBlockIndex = index;
                _blockDirty = false;

                UpdateRtfUiFromSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Load block", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _loadingBlockIntoEditor = false;
            }
        }

        private bool MaybeCommitCurrentBlock()
        {
            if (_container == null) return true;
            if (_currentBlockIndex < 0) return true;
            if (!_blockDirty) return true;

            var res = MessageBox.Show(
                this,
                "The current block has been modified. Apply changes?",
                Global.AppName,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (res == DialogResult.Cancel)
                return false;

            if (res == DialogResult.No)
            {
                LoadBlockIntoEditor(_currentBlockIndex);
                return true;
            }

            try
            {
                _container.UpdateRtfDocument(_currentBlockIndex, rtfMainText.Rtf ?? string.Empty);
                _blockDirty = false;
                _containerDirty = true;

                RefreshBlockList(selectIndex: _currentBlockIndex);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Apply changes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void RefreshBlockList(int? selectIndex = null)
        {
            if (_container == null)
            {
                lvwBlocks.Items.Clear();
                return;
            }

            lvwBlocks.BeginUpdate();
            try
            {
                lvwBlocks.Items.Clear();

                foreach (var b in _container.Blocks)
                {
                    string hashHex = Convert.ToHexString(_container.ComputeBlockHash(b));
                    string size = ByteFormat.ToHumanSize(b.StoredSizeBytes);

                    // keep your requested format dd.MM.yyyy HH:mm:ss:ffffff
                    string created = b.CreatedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss:ffffff");
                    string changed = b.ModifiedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss:ffffff");

                    var item = new ListViewItem(b.Title);
                    item.SubItems.Add(hashHex);
                    item.SubItems.Add(size);
                    item.SubItems.Add(created);
                    item.SubItems.Add(changed);
                    item.Tag = b.Index;

                    lvwBlocks.Items.Add(item);
                }
            }
            finally
            {
                lvwBlocks.EndUpdate();
            }

            UpdateUiState();

            if (selectIndex.HasValue)
                SelectListIndex(selectIndex.Value);
        }

        private int GetSelectedBlockIndex()
        {
            if (lvwBlocks.SelectedItems.Count == 0) return -1;
            var item = lvwBlocks.SelectedItems[0];
            return item.Tag is int i ? i : item.Index;
        }

        private void SelectListIndex(int index)
        {
            if (index < 0 || index >= lvwBlocks.Items.Count) return;

            lvwBlocks.BeginUpdate();
            try
            {
                lvwBlocks.SelectedItems.Clear();
                lvwBlocks.Items[index].Selected = true;
                lvwBlocks.Items[index].Focused = true;
                lvwBlocks.EnsureVisible(index);
            }
            finally
            {
                lvwBlocks.EndUpdate();
            }
        }

        private void ClearEditor()
        {
            _loadingBlockIntoEditor = true;
            try
            {
                rtfMainText.Clear();
                _currentBlockIndex = -1;
                _blockDirty = false;
            }
            finally
            {
                _loadingBlockIntoEditor = false;
            }
        }

        // ============================================================
        // Search
        // ============================================================
        private void UiSearchInBlock()
        {
            if (_container == null || _currentBlockIndex < 0)
            {
                MessageBox.Show(this, "No block is currently open.", "Search in block", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new FindDialog
            {
                StartPosition = FormStartPosition.CenterParent,
                QueryText = string.IsNullOrWhiteSpace(_lastFindText) ? (rtfMainText.SelectedText ?? string.Empty) : _lastFindText,
                MatchCase = _lastFindMatchCase,
                WholeWord = _lastFindWholeWord,
                Wrap = _lastFindWrap
            };

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            _lastFindText = dlg.QueryText ?? string.Empty;
            _lastFindMatchCase = dlg.MatchCase;
            _lastFindWholeWord = dlg.WholeWord;
            _lastFindWrap = dlg.Wrap;

            FindNextInEditor(_lastFindText, _lastFindMatchCase, _lastFindWholeWord, _lastFindWrap);
        }

        private void FindNextInEditor(string query, bool matchCase, bool wholeWord, bool wrap)
        {
            if (string.IsNullOrEmpty(query))
                return;

            RichTextBoxFinds opts = RichTextBoxFinds.None;
            if (matchCase) opts |= RichTextBoxFinds.MatchCase;
            if (wholeWord) opts |= RichTextBoxFinds.WholeWord;

            int start = rtfMainText.SelectionStart + rtfMainText.SelectionLength;
            int idx = rtfMainText.Find(query, start, opts);

            if (idx < 0 && wrap)
                idx = rtfMainText.Find(query, 0, opts);

            if (idx < 0)
            {
                MessageBox.Show(this, "No matches found.", "Search in block", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            rtfMainText.Select(idx, query.Length);
            rtfMainText.ScrollToCaret();
            rtfMainText.Focus();
        }

        private void UiSearchInContainer()
        {
            if (_container == null)
            {
                MessageBox.Show(this, "No container is currently open.", "Search in container", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new FindDialog
            {
                StartPosition = FormStartPosition.CenterParent,
                Text = "Search in container",
                QueryText = _lastFindText,
                MatchCase = _lastFindMatchCase,
                WholeWord = _lastFindWholeWord,
                Wrap = true
            };

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            string query = dlg.QueryText ?? string.Empty;
            if (string.IsNullOrWhiteSpace(query))
                return;

            _lastFindText = query;
            _lastFindMatchCase = dlg.MatchCase;
            _lastFindWholeWord = dlg.WholeWord;
            _lastFindWrap = true;

            var results = new List<ContainerSearchHit>();
            using var tmp = new RichTextBox(); // RTF -> plain text

            for (int i = 0; i < _container.Blocks.Count; i++)
            {
                string rtf = _container.GetRtfDocument(i);
                tmp.Rtf = rtf ?? string.Empty;
                string text = tmp.Text ?? string.Empty;

                int idx = FindIndex(text, query, dlg.MatchCase, dlg.WholeWord);
                if (idx >= 0)
                {
                    string snippet = BuildSnippet(text, idx, query.Length, 40);
                    results.Add(new ContainerSearchHit(i, _container.Blocks[i].Title, idx, snippet));
                }
            }

            if (results.Count == 0)
            {
                MessageBox.Show(this, "No matches were found in the container.", "Search in container", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var resDlg = new ContainerSearchResultsDialog();
            resDlg.SetResults(results);

            if (resDlg.ShowDialog(this) != DialogResult.OK)
                return;

            var hit = resDlg.SelectedHit;
            if (hit == null) return;

            if (!MaybeCommitCurrentBlock())
                return;

            LoadBlockIntoEditor(hit.BlockIndex);
            FindNextInEditor(query, dlg.MatchCase, dlg.WholeWord, wrap: true);
        }

        private static int FindIndex(string haystack, string needle, bool matchCase, bool wholeWord)
        {
            if (string.IsNullOrEmpty(needle)) return -1;

            var comparison = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
            int idx = haystack.IndexOf(needle, comparison);
            if (idx < 0) return -1;

            if (!wholeWord) return idx;

            static bool IsWordChar(char c) =>
                char.IsLetterOrDigit(c) || c == '_' ||
                c == 'Ä' || c == 'Ö' || c == 'Ü' ||
                c == 'ä' || c == 'ö' || c == 'ü' || c == 'ß';

            int left = idx - 1;
            int right = idx + needle.Length;

            if (left >= 0 && IsWordChar(haystack[left])) return -1;
            if (right < haystack.Length && IsWordChar(haystack[right])) return -1;

            return idx;
        }

        private static string BuildSnippet(string text, int index, int length, int context)
        {
            int start = Math.Max(0, index - context);
            int end = Math.Min(text.Length, index + length + context);
            string snippet = text.Substring(start, end - start).Replace("\r", " ").Replace("\n", " ");
            if (start > 0) snippet = "…" + snippet;
            if (end < text.Length) snippet = snippet + "…";
            return snippet;
        }

        // ============================================================
        // RTF Formatting + ColorPicker + Clipboard + Shortcuts
        // ============================================================
        private void RtfMainText_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && !e.Shift && e.KeyCode == Keys.F)
            {
                UiSearchInBlock();
                e.Handled = true;
                return;
            }

            if (e.Control && e.Shift && e.KeyCode == Keys.F)
            {
                UiSearchInContainer();
                e.Handled = true;
                return;
            }

            if (!e.Control && !e.Shift && e.KeyCode == Keys.F3)
            {
                if (!string.IsNullOrWhiteSpace(_lastFindText))
                    FindNextInEditor(_lastFindText, _lastFindMatchCase, _lastFindWholeWord, _lastFindWrap);

                e.Handled = true;
            }
        }

        private void UpdateRtfUiFromSelection()
        {
            Font f = rtfMainText.SelectionFont ?? rtfMainText.Font;

            boldToolButton.Checked = f.Bold;
            ItalicToolButton.Checked = f.Italic;
            UnderlineToolButton.Checked = f.Underline;
            StrikeTroughToolButton.Checked = f.Strikeout;

            FontSizeComboBox.Text = ((int)Math.Round(f.Size)).ToString(CultureInfo.InvariantCulture);
        }

        private void FontSizeComboBox_Click(object? sender, EventArgs e)
        {
        }

        private void FontSizeComboBox_TextUpdate(object? sender, EventArgs e)
        {
            if (sender is not ToolStripComboBox cb) return;

            string raw = (cb.Text ?? string.Empty).Trim()
                .Replace("pt", "", StringComparison.OrdinalIgnoreCase);

            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out float size) &&
                !float.TryParse(raw.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out size))
            {
                return;
            }

            if (size < 1f) size = 1f;
            if (size > 200f) size = 200f;

            ApplySelectionFontSize(rtfMainText, size);
            UpdateRtfUiFromSelection();
        }

        private static void ApplySelectionFontSize(RichTextBox rtb, float newSize)
        {
            int start = rtb.SelectionStart;
            int len = rtb.SelectionLength;

            if (len == 0)
            {
                Font baseFont = rtb.SelectionFont ?? rtb.Font;
                rtb.SelectionFont = new Font(baseFont.FontFamily, newSize, baseFont.Style);
                return;
            }

            if (rtb.SelectionFont != null)
            {
                Font f = rtb.SelectionFont;
                rtb.SelectionFont = new Font(f.FontFamily, newSize, f.Style);
                rtb.Select(start, len);
                return;
            }

            rtb.SuspendLayout();
            try
            {
                for (int i = 0; i < len; i++)
                {
                    rtb.Select(start + i, 1);
                    Font f = rtb.SelectionFont ?? rtb.Font;
                    rtb.SelectionFont = new Font(f.FontFamily, newSize, f.Style);
                }

                rtb.Select(start, len);
                rtb.Focus();
            }
            finally
            {
                rtb.ResumeLayout();
            }
        }

        private void ToggleSelectionStyle(FontStyle styleToToggle)
        {
            var rtb = rtfMainText;

            int start = rtb.SelectionStart;
            int len = rtb.SelectionLength;

            if (len == 0)
            {
                Font baseFont = rtb.SelectionFont ?? rtb.Font;
                FontStyle newStyle = baseFont.Style ^ styleToToggle;
                rtb.SelectionFont = new Font(baseFont, newStyle);
                rtb.Focus();
                UpdateRtfUiFromSelection();
                return;
            }

            if (rtb.SelectionFont != null)
            {
                Font f = rtb.SelectionFont;
                FontStyle newStyle = f.Style ^ styleToToggle;
                rtb.SelectionFont = new Font(f.FontFamily, f.Size, newStyle);
                rtb.Select(start, len);
                rtb.Focus();
                UpdateRtfUiFromSelection();
                return;
            }

            rtb.SuspendLayout();
            try
            {
                for (int i = 0; i < len; i++)
                {
                    rtb.Select(start + i, 1);

                    Font f = rtb.SelectionFont ?? rtb.Font;
                    FontStyle newStyle = f.Style ^ styleToToggle;

                    rtb.SelectionFont = new Font(f.FontFamily, f.Size, newStyle);
                }

                rtb.Select(start, len);
                rtb.Focus();
            }
            finally
            {
                rtb.ResumeLayout();
                UpdateRtfUiFromSelection();
            }
        }

        private void foreColorToolButton_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripItem item) return;

            Color current = item.Tag is Color c ? c : Color.Red;

            ShowColorPickerPopup(item, current, newColor =>
            {
                item.Tag = newColor;

                int selStart = rtfMainText.SelectionStart;
                int selLen = rtfMainText.SelectionLength;

                rtfMainText.SelectionColor = newColor;

                rtfMainText.Focus();
                rtfMainText.Select(selStart, selLen);
            });
        }

        private void toolStripButton1_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripItem item) return;

            Color current = item.Tag is Color c ? c : Color.Yellow;

            ShowColorPickerPopup(item, current, newColor =>
            {
                item.Tag = newColor;

                int selStart = rtfMainText.SelectionStart;
                int selLen = rtfMainText.SelectionLength;

                rtfMainText.SelectionBackColor = newColor;

                rtfMainText.Focus();
                rtfMainText.Select(selStart, selLen);
            });
        }

        private static Point AdjustToWorkingArea(Point desired, Size size)
        {
            var wa = Screen.FromPoint(desired).WorkingArea;

            int x = desired.X;
            int y = desired.Y;

            if (x + size.Width > wa.Right) x = wa.Right - size.Width;
            if (x < wa.Left) x = wa.Left;

            if (y + size.Height > wa.Bottom) y = wa.Bottom - size.Height;
            if (y < wa.Top) y = wa.Top;

            return new Point(x, y);
        }

        private void ShowColorPickerPopup(ToolStripItem ownerItem, Color currentColor, Action<Color> onOk)
        {
            if (_colorPopup != null && !_colorPopup.IsDisposed)
            {
                _colorPopup.Close();
                _colorPopup = null;
                return;
            }

            var ts = ownerItem.Owner;
            if (ts == null) return;

            Rectangle rect = ownerItem.Bounds;
            Point screenPos = ts.PointToScreen(new Point(rect.Left, rect.Bottom));

            var dlg = new HSTRYDoc.colorPicker(currentColor)
            {
                StartPosition = FormStartPosition.Manual,
                Location = AdjustToWorkingArea(screenPos, new Size(490, 288)),
                CloseOnDeactivate = true,
                TopMost = true,
                ShowInTaskbar = false
            };

            dlg.FormClosed += (s, args) =>
            {
                if (dlg.DialogResult == DialogResult.OK)
                    onOk(dlg.SelectedColor);

                _colorPopup = null;
                dlg.Dispose();
            };

            _colorPopup = dlg;
            dlg.Show(this);
            dlg.BringToFront();
            dlg.Activate();
        }

        private void toolButtonCopy_Click(object? sender, EventArgs e) => rtfMainText.Copy();
        private void toolButtonPaste_Click(object? sender, EventArgs e) => rtfMainText.Paste();
        private void toolButtonCut_Click(object? sender, EventArgs e) => rtfMainText.Cut();
        private void toolButtonSelectAll_Click(object? sender, EventArgs e) => rtfMainText.SelectAll();

        // ============================================================
        // Required by designer (Exit)
        // ============================================================
        private void closeToolStripMenuItem_Click(object sender, EventArgs e) => Close();
    }

    public sealed record ContainerSearchHit(int BlockIndex, string BlockTitle, int IndexInText, string Snippet);
}
