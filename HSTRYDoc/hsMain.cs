// hsMain.cs
using System.Globalization;
using System.IO;

namespace HSTRYDoc
{
    public partial class hsMain : Form
    {
        private HSTRYDoc.colorPicker? _colorPopup;
        private Color _currentColor = Color.Black;

        // ---- Container/Editor State ----
        private HSTRYContainer? _container;
        private string? _containerPath;

        private int _currentBlockIndex = -1;
        private bool _loadingBlockIntoEditor = false;
        private bool _blockDirty = false;
        private bool _containerDirty = false;

        public hsMain()
        {
            InitializeComponent();
        }

        private void hsMain_Load(object sender, EventArgs e)
        {
            WireUiEvents();

            // 1) "Öffnen mit..." beim Start: wenn valide hstry Datei übergeben wurde -> laden
            // 2) sonst: neuen Container erstellen
            if (!TryOpenFromCommandLineOrShell())
            {
                // Neuer Container (mit Passwort). Wenn User abbricht -> App schließen.
                if (!UiCreateNewContainer(initialStartup: true))
                {
                    Close();
                    return;
                }
            }

            UpdateUiState();
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

                // Manche Shells geben mehrere args; wir nehmen das erste, das existiert
                string? path = args.Skip(1).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));
                if (string.IsNullOrWhiteSpace(path)) return false;

                // Nur wenn es nach hstry aussieht (Magic) – sonst ignorieren
                if (!LooksLikeHstryFile(path)) return false;

                return OpenContainerFromPath(path);
            }
            catch
            {
                // Startup soll nie crashen -> fallback: new container
                return false;
            }
        }

        private static bool LooksLikeHstryFile(string path)
        {
            // Minimalprüfung: Magic bytes am Dateianfang müssen passen
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
            // Passwort abfragen und laden; true nur bei Erfolg
            using var pwd = new PasswordDialog("Container öffnen", "Container-Passwort eingeben:", requireConfirm: false);
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
                MessageBox.Show(this, ex.Message, "Container öffnen", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ============================================================
        // UI wiring (nur Controls die im Designer NICHT verdrahtet sind)
        // ============================================================
        private void WireUiEvents()
        {
            // Toolstrip (oben)
            newBlockToolStripButton.Click += (_, __) => UiNewBlock();
            openContainerToolStripButton.Click += (_, __) => UiOpenContainer();
            saveContainerToolStripButton.Click += (_, __) => UiSaveContainer();

            // Menü "File"
            newToolStripMenuItem.Click += (_, __) => UiNewBlock();
            openContainerToolStripMenuItem.Click += (_, __) => UiOpenContainer();
            saveContainerToolStripMenuItem.Click += (_, __) => UiSaveContainer();
            saveContainerAsToolStripMenuItem.Click += (_, __) => UiSaveContainerAs();

            // Block Kontextmenü
            newBlockToolStripMenuItem.Click += (_, __) => UiNewBlock();
            renameBlockToolStripMenuItem.Click += (_, __) => UiRenameBlock();

            // ListView
            lvwBlocks.SelectedIndexChanged += (_, __) => UiSelectBlockFromList();

            // RTF Context Menu (Clipboard)
            copyToolStripMenuItem1.Click += (_, __) => rtfMainText.Copy();
            pasteToolStripMenuItem1.Click += (_, __) => rtfMainText.Paste();
            cutToolStripMenuItem1.Click += (_, __) => rtfMainText.Cut();
            selectAllToolStripMenuItem1.Click += (_, __) => rtfMainText.SelectAll();

            // RTF Context Menu (Format)
            boldToolStripMenuItem.Click += (_, __) => boldToolButton_Click(boldToolButton, EventArgs.Empty);
            italicToolStripMenuItem.Click += (_, __) => ItalicToolButton_Click(ItalicToolButton, EventArgs.Empty);
            underlineToolStripMenuItem.Click += (_, __) => UnderlineToolButton_Click(UnderlineToolButton, EventArgs.Empty);
            strikeToolStripMenuItem.Click += (_, __) => StrikeTroughToolButton_Click(StrikeTroughToolButton, EventArgs.Empty);

            // Fore/Backcolor aus ContextMenu
            forecolorToolStripMenuItem.Click += (_, __) => foreColorToolButton_Click(foreColorToolButton, EventArgs.Empty);
            textBackgroundcolorToolStripMenuItem.Click += (_, __) => toolStripButton1_Click(backgroundColorToolButton, EventArgs.Empty);

            // Dirty tracking
            rtfMainText.TextChanged += (_, __) =>
            {
                if (_loadingBlockIntoEditor) return;
                if (_container == null) return;
                if (_currentBlockIndex < 0) return;

                _blockDirty = true;
                _containerDirty = true;
                UpdateUiState();
            };

            // Close handling
            FormClosing += hsMain_FormClosing;
        }

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
                    "Container hat ungespeicherte Änderungen. Jetzt speichern?",
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

        // Rückgabewert: true = Container erstellt, false = abgebrochen
        private bool UiCreateNewContainer(bool initialStartup = false)
        {
            using var pwd = new PasswordDialog("Container erstellen", "Passwort für den neuen Container wählen:", requireConfirm: true);
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
                MessageBox.Show(this, ex.Message, "Container erstellen", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void UiOpenContainer()
        {
            if (!MaybeCommitCurrentBlock()) return;

            if (_containerDirty)
            {
                var res = MessageBox.Show(
                    this,
                    "Container hat ungespeicherte Änderungen. Ohne Speichern fortfahren?",
                    "Container öffnen",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (res != DialogResult.Yes) return;
            }

            using var ofd = new OpenFileDialog
            {
                Filter = "HSTRY Container (*.hstry)|*.hstry|Alle Dateien (*.*)|*.*",
                CheckFileExists = true
            };

            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            // Nur versuchen zu laden, wenn es wirklich wie HSTRY aussieht
            if (!LooksLikeHstryFile(ofd.FileName))
            {
                MessageBox.Show(this, "Die Datei ist keine gültige HSTRY-Containerdatei.", "Container öffnen",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OpenContainerFromPath(ofd.FileName);
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
                MessageBox.Show(this, ex.Message, "Container speichern", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool UiSaveContainerAs()
        {
            if (_container == null) return false;
            if (!MaybeCommitCurrentBlock()) return false;

            using var sfd = new SaveFileDialog
            {
                Filter = "HSTRY Container (*.hstry)|*.hstry|Alle Dateien (*.*)|*.*",
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
                MessageBox.Show(this, ex.Message, "Container speichern unter", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                // Wenn User abbricht: nichts tun
                if (!UiCreateNewContainer()) return;
            }

            if (!MaybeCommitCurrentBlock()) return;

            string title = _container!.GenerateUniqueTitle();

            using (var dlg = new TextPromptDialog("Neuer Block", "Blockname:", title))
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
                var b = _container!.AddRtfDocument(title, emptyRtf ?? string.Empty);
                _containerDirty = true;

                RefreshBlockList(selectIndex: b.Index);
                LoadBlockIntoEditor(b.Index);
                UpdateUiState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Neuer Block", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UiRenameBlock()
        {
            if (_container == null) return;

            int idx = GetSelectedBlockIndex();
            if (idx < 0) return;

            if (!MaybeCommitCurrentBlock()) return;

            var b = _container.Blocks[idx];

            using var dlg = new TextPromptDialog("Block umbenennen", "Neuer Name:", b.Title);
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
                MessageBox.Show(this, ex.Message, "Block umbenennen", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Block laden", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                "Aktueller Block wurde geändert. Änderungen übernehmen?",
                Global.AppName,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (res == DialogResult.Cancel)
                return false;

            if (res == DialogResult.No)
            {
                LoadBlockIntoEditor(_currentBlockIndex); // discard
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
                MessageBox.Show(this, ex.Message, "Änderungen übernehmen", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        // Deine bestehenden Handler (unverändert) + Exit Button
        // ============================================================
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void foreColorToolButton_Click(object sender, EventArgs e)
        {
            var item = (ToolStripItem)sender;

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

        private void ShowColorPickerPopup(
            ToolStripItem ownerItem,
            Color currentColor,
            Action<Color> onOk)
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

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            var item = (ToolStripItem)sender;

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

        private void toolButtonSelectAll_Click(object sender, EventArgs e) => rtfMainText.SelectAll();
        private void toolButtonCut_Click(object sender, EventArgs e) => rtfMainText.Cut();
        private void toolButtonPaste_Click(object sender, EventArgs e) => rtfMainText.Paste();
        private void toolButtonCopy_Click(object sender, EventArgs e) => rtfMainText.Copy();

        private void FontSizeComboBox_Click(object sender, EventArgs e)
        {
        }

        private void FontSizeComboBox_TextUpdate(object sender, EventArgs e)
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

        private void boldToolButton_Click(object sender, EventArgs e) => ToggleSelectionStyle(FontStyle.Bold);
        private void ItalicToolButton_Click(object sender, EventArgs e) => ToggleSelectionStyle(FontStyle.Italic);
        private void UnderlineToolButton_Click(object sender, EventArgs e) => ToggleSelectionStyle(FontStyle.Underline);
        private void StrikeTroughToolButton_Click(object sender, EventArgs e) => ToggleSelectionStyle(FontStyle.Strikeout);

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
                return;
            }

            if (rtb.SelectionFont != null)
            {
                Font f = rtb.SelectionFont;
                FontStyle newStyle = f.Style ^ styleToToggle;
                rtb.SelectionFont = new Font(f.FontFamily, f.Size, newStyle);
                rtb.Select(start, len);
                rtb.Focus();
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
            }
        }
    }
}
