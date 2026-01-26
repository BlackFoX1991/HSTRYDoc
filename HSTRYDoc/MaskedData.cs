using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace HSTRYDoc
{
    public partial class MaskedData : Form
    {
        private readonly HSTRYContainer _container;

        private readonly List<MaskDefinition> _defs = new();
        private readonly Dictionary<string, MaskDefinition> _defById = new(StringComparer.OrdinalIgnoreCase);

        private MaskDefinition? _currentDef;

        // data blocks for current mask
        private List<int> _currentDataBlockIndices = new();

        // aggregated records
        private readonly MaskDataDocument _currentDoc = new();

        // base mask: blockIndex -> list of record dicts
        private readonly Dictionary<int, List<Dictionary<string, string>>> _recordsByBlock =
            new Dictionary<int, List<Dictionary<string, string>>>();

        // reference lookups: RefMaskId -> (ID -> record dict)
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _refLookup =
            new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

        // Column specs (excluding "#")
        private readonly List<ColumnSpec> _columns = new();

        private TextBox? _cellEditor;
        private ListViewItem? _cellEditItem;
        private int _cellEditSubIndex = -1;
        private bool _cellEditClosing = false;

        public bool ContainerChanged { get; private set; } = false;

        private sealed class ColumnSpec
        {
            public bool Editable; // only base fields are editable
            public string Header = string.Empty;

            // For editable base fields:
            public string? BaseFieldName;
            public string? BaseRefMaskId; // not null if this base field is a reference

            // For derived ref fields:
            public string? RefFieldName;     // e.g. "PATIENT_ID"
            public string? RefMaskId;        // e.g. "PERSON"
            public string? RefTargetField;   // e.g. "FIRSTNAME"
        }

        public MaskedData(HSTRYContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            InitializeComponent();

            Text = "Additional Data...";
            lblInfo.Text = "Choose a data definition that you defined in your blocks (*.model).";

            addDataToolStripMenuItem.Text = "Add Data...";
            removeDaToolStripMenuItem.Text = "Remove Data...";

            comboDataMask.DropDownStyle = ComboBoxStyle.DropDownList;

            comboDataMask.SelectedIndexChanged += (_, __) => LoadSelectedMask();
            addDataToolStripMenuItem.Click += (_, __) => UiAddRecord();
            removeDaToolStripMenuItem.Click += (_, __) => UiRemoveSelectedRecord();

            lvwData.FullRowSelect = true;
            lvwData.LabelEdit = false;

            lvwData.MouseDoubleClick += (_, e) => UiBeginCellEditAtMouse(e);
            lvwData.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.F2)
                {
                    UiBeginCellEditOnSelection();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    CancelCellEdit();
                    e.Handled = true;
                }
            };

            PopulateDefinitions();
        }

        // ------------------------------------------------------------
        // Definitions scan + global uniqueness
        // ------------------------------------------------------------
        private void PopulateDefinitions()
        {
            _defs.Clear();
            _defById.Clear();

            var errors = new List<string>();
            var seenGlobal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var b in _container.Blocks)
            {
                if (!b.Title.EndsWith(".model", StringComparison.OrdinalIgnoreCase))
                    continue;

                string text = GetPlainTextFromBlockIndex(b.Index);

                if (!MaskedDataParsers.TryParseMaskDefinitions(b.Title, text, out var defsInBlock, out var err))
                {
                    errors.Add(err);
                    continue;
                }

                foreach (var d in defsInBlock)
                {
                    if (!seenGlobal.Add(d.MaskId))
                    {
                        errors.Add($"Duplicate MASK id '{d.MaskId}' found. Models must be unique.");
                        continue;
                    }

                    _defs.Add(d);
                    _defById[d.MaskId] = d;
                }
            }

            // validate references (existence + ID field)
            errors.AddRange(ValidateReferenceModels());

            comboDataMask.BeginUpdate();
            try
            {
                comboDataMask.Items.Clear();
                foreach (var d in _defs.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase))
                    comboDataMask.Items.Add(d);
            }
            finally
            {
                comboDataMask.EndUpdate();
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(this,
                    string.Join("\r\n", errors.Distinct()),
                    "Model warnings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            if (comboDataMask.Items.Count > 0)
                comboDataMask.SelectedIndex = 0;
            else
                SetupEmptyGrid();
        }

        private IEnumerable<string> ValidateReferenceModels()
        {
            var errors = new List<string>();

            foreach (var d in _defs)
            {
                foreach (var f in d.Fields)
                {
                    if (!f.IsReference) continue;

                    if (string.IsNullOrWhiteSpace(f.RefMaskId))
                        continue;

                    if (!_defById.TryGetValue(f.RefMaskId!, out var refDef))
                    {
                        errors.Add($"MASK '{d.MaskId}' references unknown model '{f.RefMaskId}' in field '{f.Name}'.");
                        continue;
                    }

                    if (!refDef.HasIdField)
                    {
                        errors.Add($"Referenced model '{refDef.MaskId}' must define an 'ID' field (required for relations).");
                    }
                }
            }

            return errors;
        }

        private void SetupEmptyGrid()
        {
            lvwData.BeginUpdate();
            try
            {
                lvwData.Columns.Clear();
                lvwData.Items.Clear();
                lvwData.Columns.Add("Info", 600);
                lvwData.Items.Add(new ListViewItem("No .model definitions found."));
            }
            finally
            {
                lvwData.EndUpdate();
            }
        }

        // ------------------------------------------------------------
        // Selection -> load base data + reference lookups
        // ------------------------------------------------------------
        private void LoadSelectedMask()
        {
            CancelCellEdit();

            if (comboDataMask.SelectedItem is not MaskDefinition def)
                return;

            _currentDef = def;

            _currentDataBlockIndices = FindDataBlockIndicesByMaskId(def.MaskId);

            if (_currentDataBlockIndices.Count == 0)
            {
                BuildGridColumns(def, includeReferences: true);
                lvwData.Items.Clear();

                MessageBox.Show(
                    this,
                    $"No data block found for MASK '{def.MaskId}'.\n\n" +
                    $"Create a block with any name that ends with:\n{def.DataBlockSuffix}\n\n" +
                    $"Example:\nmy_records{def.DataBlockSuffix}",
                    "Data block missing",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                addDataToolStripMenuItem.Enabled = false;
                removeDaToolStripMenuItem.Enabled = false;
                return;
            }

            addDataToolStripMenuItem.Enabled = true;
            removeDaToolStripMenuItem.Enabled = true;

            // load base records
            _currentDoc.Records.Clear();
            _recordsByBlock.Clear();

            foreach (int blockIndex in _currentDataBlockIndices)
            {
                string dataText = GetPlainTextFromBlockIndex(blockIndex);
                var parsed = MaskedDataParsers.ParseDataRecords(dataText);

                _recordsByBlock[blockIndex] = parsed;

                foreach (var rec in parsed)
                {
                    _currentDoc.Records.Add(new MaskRecord
                    {
                        SourceBlockIndex = blockIndex,
                        Data = rec
                    });
                }
            }

            // build reference lookups (for any refs in current model)
            BuildReferenceLookupsForCurrentDef();

            // build columns (base + derived ref columns)
            BuildGridColumns(def, includeReferences: true);

            // unknown keys handling (only against base model fields)
            bool changed = AskAndOptionallyRemoveUnknownKeys(def, _currentDoc);
            if (changed)
            {
                SaveAllCurrentBlocksBackToContainer();
                ContainerChanged = true;
            }

            RenderRecords(def, _currentDoc);
        }

        private void BuildReferenceLookupsForCurrentDef()
        {
            _refLookup.Clear();

            if (_currentDef == null)
                return;

            // find distinct referenced mask ids
            var refIds = _currentDef.Fields
                .Where(f => f.IsReference && !string.IsNullOrWhiteSpace(f.RefMaskId))
                .Select(f => f.RefMaskId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var refMaskId in refIds)
            {
                if (!_defById.TryGetValue(refMaskId, out var refDef))
                    continue;

                // must have ID field to work
                if (!refDef.HasIdField)
                    continue;

                // load ALL ".data:<refMaskId>" blocks
                var refBlocks = FindDataBlockIndicesByMaskId(refMaskId);
                var lookup = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

                // stable block order by title
                var ordered = refBlocks
                    .Select(i => (Index: i, Title: _container.Blocks[i].Title ?? ""))
                    .OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.Index)
                    .ToList();

                var dupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (int blockIndex in ordered)
                {
                    string txt = GetPlainTextFromBlockIndex(blockIndex);
                    var records = MaskedDataParsers.ParseDataRecords(txt);

                    foreach (var rec in records)
                    {
                        if (!rec.TryGetValue("ID", out var id) || string.IsNullOrWhiteSpace(id))
                            continue;

                        if (lookup.ContainsKey(id))
                        {
                            dupIds.Add(id);
                            continue; // first wins
                        }

                        lookup[id] = rec;
                    }
                }

                _refLookup[refMaskId] = lookup;

                if (dupIds.Count > 0)
                {
                    MessageBox.Show(
                        this,
                        $"Referenced data for '{refMaskId}' contains duplicate IDs.\n\n" +
                        $"Duplicates (first occurrence kept):\n{string.Join(", ", dupIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}",
                        "Reference data warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        // ------------------------------------------------------------
        // Data blocks matching
        // ------------------------------------------------------------
        private List<int> FindDataBlockIndicesByMaskId(string maskId)
        {
            var result = new List<(int Index, string Title)>();

            if (string.IsNullOrWhiteSpace(maskId))
                return new List<int>();

            string suffix = ".data:" + maskId;

            for (int i = 0; i < _container.Blocks.Count; i++)
            {
                string title = _container.Blocks[i].Title ?? string.Empty;
                if (title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    result.Add((i, title));
            }

            result.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Title, b.Title));
            return result.Select(x => x.Index).ToList();
        }

        // ------------------------------------------------------------
        // Columns (base + derived ref columns)
        // ------------------------------------------------------------
        private void BuildGridColumns(MaskDefinition def, bool includeReferences)
        {
            _columns.Clear();

            lvwData.BeginUpdate();
            try
            {
                lvwData.Columns.Clear();
                lvwData.Columns.Add("#", 40);

                // base fields
                foreach (var f in def.Fields)
                {
                    _columns.Add(new ColumnSpec
                    {
                        Editable = true,
                        Header = f.ToString(),
                        BaseFieldName = f.Name,
                        BaseRefMaskId = f.RefMaskId
                    });

                    lvwData.Columns.Add(f.ToString(), 160);

                    // derived columns from reference model
                    if (includeReferences && f.IsReference && !string.IsNullOrWhiteSpace(f.RefMaskId))
                    {
                        if (_defById.TryGetValue(f.RefMaskId!, out var refDef) && refDef.HasIdField)
                        {
                            foreach (var rf in refDef.Fields)
                            {
                                if (string.Equals(rf.Name, "ID", StringComparison.OrdinalIgnoreCase))
                                    continue; // don't duplicate

                                string header = $"{f.Name}.{f.RefMaskId}.{rf.Name}";

                                _columns.Add(new ColumnSpec
                                {
                                    Editable = false,
                                    Header = header,
                                    RefFieldName = f.Name,
                                    RefMaskId = f.RefMaskId,
                                    RefTargetField = rf.Name
                                });

                                lvwData.Columns.Add(header, 160);
                            }
                        }
                    }
                }

                lvwData.Items.Clear();
            }
            finally
            {
                lvwData.EndUpdate();
            }
        }

        // ------------------------------------------------------------
        // Rendering
        // ------------------------------------------------------------
        private void RenderRecords(MaskDefinition def, MaskDataDocument doc)
        {
            lvwData.BeginUpdate();
            try
            {
                lvwData.Items.Clear();

                for (int i = 0; i < doc.Records.Count; i++)
                {
                    var rec = doc.Records[i];
                    var it = new ListViewItem((i + 1).ToString());

                    // for each ColumnSpec -> create subitems
                    foreach (var col in _columns)
                    {
                        if (col.Editable)
                        {
                            rec.Data.TryGetValue(col.BaseFieldName ?? "", out var v);
                            it.SubItems.Add(v ?? string.Empty);
                        }
                        else
                        {
                            it.SubItems.Add(ResolveDerivedValue(rec, col) ?? string.Empty);
                        }
                    }

                    it.Tag = i; // global record index
                    lvwData.Items.Add(it);
                }
            }
            finally
            {
                lvwData.EndUpdate();
            }
        }

        private string? ResolveDerivedValue(MaskRecord rec, ColumnSpec col)
        {
            if (col.RefFieldName == null || col.RefMaskId == null || col.RefTargetField == null)
                return null;

            if (!rec.Data.TryGetValue(col.RefFieldName, out var id) || string.IsNullOrWhiteSpace(id))
                return null;

            if (!_refLookup.TryGetValue(col.RefMaskId, out var lookup))
                return null;

            if (!lookup.TryGetValue(id, out var refRec))
                return null;

            refRec.TryGetValue(col.RefTargetField, out var v);
            return v;
        }

        private void RefreshDerivedColumnsForRow(ListViewItem item, MaskRecord rec)
        {
            // subIndex 0 is "#"
            for (int i = 0; i < _columns.Count; i++)
            {
                int subIndex = i + 1;

                var col = _columns[i];
                if (col.Editable)
                    continue;

                string newVal = ResolveDerivedValue(rec, col) ?? string.Empty;

                if (subIndex < item.SubItems.Count)
                    item.SubItems[subIndex].Text = newVal;
            }
        }

        // ------------------------------------------------------------
        // Cell edit (only for editable base fields)
        // ------------------------------------------------------------
        private void UiBeginCellEditAtMouse(MouseEventArgs e)
        {
            if (_currentDef == null) return;
            if (_currentDataBlockIndices.Count == 0) return;

            var hit = lvwData.HitTest(e.Location);
            if (hit.Item == null || hit.SubItem == null) return;

            int subIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
            BeginCellEdit(hit.Item, subIndex);
        }

        private void UiBeginCellEditOnSelection()
        {
            if (_currentDef == null) return;
            if (_currentDataBlockIndices.Count == 0) return;
            if (lvwData.SelectedItems.Count == 0) return;

            var item = lvwData.SelectedItems[0];
            BeginCellEdit(item, 1);
        }

        private void BeginCellEdit(ListViewItem item, int subIndex)
        {
            if (_currentDef == null) return;
            if (_currentDataBlockIndices.Count == 0) return;

            if (item?.Tag is not int globalRecIndex) return;
            if (globalRecIndex < 0 || globalRecIndex >= _currentDoc.Records.Count) return;

            if (subIndex <= 0) return;

            int colSpecIndex = subIndex - 1;
            if (colSpecIndex < 0 || colSpecIndex >= _columns.Count) return;

            var col = _columns[colSpecIndex];
            if (!col.Editable)
                return; // derived reference columns are read-only

            CancelCellEdit();

            _cellEditItem = item;
            _cellEditSubIndex = subIndex;

            var bounds = item.SubItems[subIndex].Bounds;

            _cellEditor = new TextBox
            {
                Bounds = bounds,
                BorderStyle = BorderStyle.FixedSingle,
                Text = item.SubItems[subIndex].Text ?? string.Empty
            };

            _cellEditor.Leave += (_, __) =>
            {
                if (_cellEditClosing) return;
                CommitCellEdit();
            };

            _cellEditor.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    CommitCellEdit();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    CancelCellEdit();
                }
            };

            lvwData.Controls.Add(_cellEditor);
            _cellEditor.BringToFront();
            _cellEditor.Focus();
            _cellEditor.SelectAll();
        }

        private void CommitCellEdit()
        {
            if (_cellEditClosing) return;

            var editor = _cellEditor;
            var item = _cellEditItem;
            int subIndex = _cellEditSubIndex;

            if (editor == null || item == null)
            {
                CancelCellEdit();
                return;
            }

            if (_currentDef == null || _currentDataBlockIndices.Count == 0)
            {
                CancelCellEdit();
                return;
            }

            if (item.Tag is not int globalRecIndex || globalRecIndex < 0 || globalRecIndex >= _currentDoc.Records.Count)
            {
                CancelCellEdit();
                return;
            }

            int colSpecIndex = subIndex - 1;
            if (colSpecIndex < 0 || colSpecIndex >= _columns.Count)
            {
                CancelCellEdit();
                return;
            }

            var col = _columns[colSpecIndex];
            if (!col.Editable || string.IsNullOrWhiteSpace(col.BaseFieldName))
            {
                CancelCellEdit();
                return;
            }

            string fieldName = col.BaseFieldName!;
            string newValue = editor.Text ?? string.Empty;

            // update UI
            item.SubItems[subIndex].Text = newValue;

            // update data
            var rec = _currentDoc.Records[globalRecIndex];
            rec.Data[fieldName] = newValue;

            // save only that record's source block
            SaveSingleBlockBackToContainer(rec.SourceBlockIndex);
            ContainerChanged = true;

            // If this was a reference field change -> refresh derived columns for the row
            if (!string.IsNullOrWhiteSpace(col.BaseRefMaskId))
            {
                RefreshDerivedColumnsForRow(item, rec);
            }

            CancelCellEdit();
        }

        private void CancelCellEdit()
        {
            if (_cellEditClosing)
                return;

            _cellEditClosing = true;
            try
            {
                var editor = _cellEditor;
                _cellEditor = null;

                if (editor != null)
                {
                    try
                    {
                        if (!editor.IsDisposed)
                            lvwData.Controls.Remove(editor);
                    }
                    catch { }

                    try
                    {
                        if (!editor.IsDisposed)
                            editor.Dispose();
                    }
                    catch { }
                }

                _cellEditItem = null;
                _cellEditSubIndex = -1;
            }
            finally
            {
                _cellEditClosing = false;
            }
        }

        // ------------------------------------------------------------
        // Unknown keys handling (only base fields)
        // ------------------------------------------------------------
        private bool AskAndOptionallyRemoveUnknownKeys(MaskDefinition def, MaskDataDocument doc)
        {
            var allowed = def.Fields.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unknown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rec in doc.Records)
            {
                foreach (var k in rec.Data.Keys)
                {
                    if (!allowed.Contains(k))
                        unknown.Add(k);
                }
            }

            if (unknown.Count == 0)
                return false;

            string list = string.Join(", ", unknown.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

            var res = MessageBox.Show(
                this,
                "The data blocks contain fields that are not present in the selected model.\n\n" +
                $"Unknown fields:\n{list}\n\n" +
                "Do you want to delete these fields from ALL records now?",
                "Model/Data mismatch",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (res != DialogResult.Yes)
                return false;

            foreach (var rec in doc.Records)
            {
                var keysToRemove = rec.Data.Keys.Where(k => unknown.Contains(k)).ToList();
                foreach (var k in keysToRemove)
                    rec.Data.Remove(k);
            }

            return true;
        }

        // ------------------------------------------------------------
        // Add / Remove
        // ------------------------------------------------------------
        private void UiAddRecord()
        {
            if (_currentDef == null) return;
            if (_currentDataBlockIndices.Count == 0)
            {
                MessageBox.Show(this, "No data block available.", "Add Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int targetBlock = ChooseTargetDataBlockIndex();
            if (targetBlock < 0) return;

            using var dlg = new DynamicRecordDialog(_currentDef, record: null);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            var newRec = dlg.ResultRecord ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!_recordsByBlock.TryGetValue(targetBlock, out var list))
            {
                list = new List<Dictionary<string, string>>();
                _recordsByBlock[targetBlock] = list;
            }

            list.Add(newRec);

            _currentDoc.Records.Add(new MaskRecord
            {
                SourceBlockIndex = targetBlock,
                Data = newRec
            });

            SaveSingleBlockBackToContainer(targetBlock);

            ContainerChanged = true;
            RenderRecords(_currentDef, _currentDoc);
        }

        private int ChooseTargetDataBlockIndex()
        {
            if (_currentDataBlockIndices.Count == 1)
                return _currentDataBlockIndices[0];

            using var dlg = new DataBlockPickDialog(_container, _currentDataBlockIndices);
            return dlg.ShowDialog(this) == DialogResult.OK ? dlg.SelectedBlockIndex : -1;
        }

        private void UiRemoveSelectedRecord()
        {
            if (_currentDef == null) return;
            if (_currentDataBlockIndices.Count == 0) return;

            if (lvwData.SelectedItems.Count == 0)
                return;

            var it = lvwData.SelectedItems[0];
            if (it.Tag is not int globalRecIndex) return;
            if (globalRecIndex < 0 || globalRecIndex >= _currentDoc.Records.Count) return;

            var res = MessageBox.Show(
                this,
                "Remove the selected record?",
                "Remove Data",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (res != DialogResult.Yes)
                return;

            var rec = _currentDoc.Records[globalRecIndex];
            int srcBlock = rec.SourceBlockIndex;

            if (_recordsByBlock.TryGetValue(srcBlock, out var list))
            {
                int idx = list.IndexOf(rec.Data);
                if (idx >= 0)
                    list.RemoveAt(idx);
            }

            _currentDoc.Records.RemoveAt(globalRecIndex);

            SaveSingleBlockBackToContainer(srcBlock);

            ContainerChanged = true;
            RenderRecords(_currentDef, _currentDoc);
        }

        private void SaveSingleBlockBackToContainer(int blockIndex)
        {
            if (_currentDef == null) return;
            if (blockIndex < 0) return;

            CancelCellEdit();

            if (!_recordsByBlock.TryGetValue(blockIndex, out var records))
                records = new List<Dictionary<string, string>>();

            string serialized = MaskedDataParsers.SerializeDataRecords(_currentDef, records);
            string rtf = RtfUtil.PlainTextToRtf(serialized);

            _container.UpdateRtfDocument(blockIndex, rtf);
        }

        private void SaveAllCurrentBlocksBackToContainer()
        {
            if (_currentDef == null) return;

            CancelCellEdit();

            foreach (var kv in _recordsByBlock)
            {
                int blockIndex = kv.Key;
                var records = kv.Value;

                string serialized = MaskedDataParsers.SerializeDataRecords(_currentDef, records);
                string rtf = RtfUtil.PlainTextToRtf(serialized);

                _container.UpdateRtfDocument(blockIndex, rtf);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            CancelCellEdit();
            base.OnFormClosed(e);
        }

        // ------------------------------------------------------------
        // Helpers: block access + RTF -> Text
        // ------------------------------------------------------------
        private string GetPlainTextFromBlockIndex(int index)
        {
            string rtf = _container.GetRtfDocument(index) ?? string.Empty;
            return RtfUtil.RtfToPlainText(rtf);
        }

        // ------------------------------------------------------------
        // Minimal helper dialog for adding a record
        // ------------------------------------------------------------
        private sealed class DynamicRecordDialog : Form
        {
            private readonly MaskDefinition _def;
            private readonly Dictionary<string, TextBox> _boxes = new(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, string>? ResultRecord { get; private set; }

            public DynamicRecordDialog(MaskDefinition def, Dictionary<string, string>? record)
            {
                _def = def;

                Text = "Add Data";
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false;
                MaximizeBox = false;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                Width = 520;
                Height = 120 + (def.Fields.Count * 30);

                var panel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = def.Fields.Count + 1,
                    Padding = new Padding(12),
                    AutoSize = true,
                    AutoScroll = true
                };
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                int r = 0;
                foreach (var f in def.Fields)
                {
                    panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

                    var lbl = new Label
                    {
                        Text = f.Name,
                        Dock = DockStyle.Fill,
                        TextAlign = System.Drawing.ContentAlignment.MiddleLeft
                    };

                    var tb = new TextBox { Dock = DockStyle.Fill };
                    if (record != null && record.TryGetValue(f.Name, out var v))
                        tb.Text = v ?? string.Empty;

                    _boxes[f.Name] = tb;

                    panel.Controls.Add(lbl, 0, r);
                    panel.Controls.Add(tb, 1, r);
                    r++;
                }

                var btnPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(12),
                    Height = 44
                };

                var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90 };
                var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };

                btnPanel.Controls.Add(btnOk);
                btnPanel.Controls.Add(btnCancel);

                Controls.Add(panel);
                Controls.Add(btnPanel);

                AcceptButton = btnOk;
                CancelButton = btnCancel;

                FormClosing += (_, __) =>
                {
                    if (DialogResult != DialogResult.OK) return;

                    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var f in def.Fields)
                        dict[f.Name] = _boxes[f.Name].Text ?? string.Empty;

                    ResultRecord = dict;
                };
            }
        }

        private sealed class DataBlockPickDialog : Form
        {
            private readonly ListBox _list = new ListBox();
            private readonly Button _ok = new Button();
            private readonly Button _cancel = new Button();

            public int SelectedBlockIndex { get; private set; } = -1;

            public DataBlockPickDialog(HSTRYContainer container, List<int> blockIndices)
            {
                Text = "Select data block";
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false;
                MaximizeBox = false;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                Width = 520;
                Height = 320;

                _list.Dock = DockStyle.Fill;

                foreach (int idx in blockIndices)
                {
                    string title = container.Blocks[idx].Title ?? $"<block {idx}>";
                    _list.Items.Add(new Item { Index = idx, Text = title });
                }

                _list.DisplayMember = nameof(Item.Text);

                var panel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    FlowDirection = FlowDirection.RightToLeft,
                    Height = 44,
                    Padding = new Padding(12)
                };

                _ok.Text = "OK";
                _ok.Width = 90;
                _ok.Enabled = false;

                _cancel.Text = "Cancel";
                _cancel.Width = 90;

                panel.Controls.Add(_ok);
                panel.Controls.Add(_cancel);

                Controls.Add(_list);
                Controls.Add(panel);

                _list.SelectedIndexChanged += (_, __) => _ok.Enabled = _list.SelectedItem != null;

                _ok.Click += (_, __) =>
                {
                    if (_list.SelectedItem is Item it)
                    {
                        SelectedBlockIndex = it.Index;
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                };

                _cancel.Click += (_, __) =>
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                };
            }

            private sealed class Item
            {
                public int Index { get; init; }
                public string Text { get; init; } = string.Empty;
            }
        }
    }

    internal static class RtfUtil
    {
        public static string RtfToPlainText(string rtf)
        {
            using var rtb = new RichTextBox();
            rtb.Rtf = string.IsNullOrWhiteSpace(rtf) ? @"{\rtf1\ansi}" : rtf;
            return rtb.Text ?? string.Empty;
        }

        public static string PlainTextToRtf(string text)
        {
            using var rtb = new RichTextBox();
            rtb.Text = text ?? string.Empty;
            return rtb.Rtf ?? string.Empty;
        }
    }
}
