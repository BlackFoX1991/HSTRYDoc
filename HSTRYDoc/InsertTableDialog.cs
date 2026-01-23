// InsertTableDialog.cs
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HSTRYDoc
{
    public partial class InsertTableDialog : Form
    {
        private readonly int _editorClientWidthPx;
        private readonly float _dpiX;

        // Use preview selection (hover when unlocked, locked selection when locked)
        public int SelectedRows => sizePicker.PreviewRows;
        public int SelectedCols => sizePicker.PreviewCols;

        public string ResultRtf { get; private set; } = string.Empty;

        private int _lastColsForWidths = -1;

        public InsertTableDialog(RichTextBox editor)
        {
            InitializeComponent();

            if (editor == null) throw new ArgumentNullException(nameof(editor));

            _editorClientWidthPx = Math.Max(1, editor.ClientSize.Width);
            using (var g = editor.CreateGraphics())
                _dpiX = g.DpiX <= 0 ? 96f : g.DpiX;

            // defaults
            chkFitToEditor.Checked = true;
            dgvWidths.Enabled = false;
            nudDefaultWidthPx.Enabled = false;
            btnEqualize.Enabled = false;

            sizePicker.MaxCols = 12;
            sizePicker.MaxRows = 10;

            // events
            sizePicker.SelectionChanged += (_, __) => OnPickerSelectionChanged();
            chkFitToEditor.CheckedChanged += (_, __) => OnWidthModeChanged();
            btnEqualize.Click += (_, __) => EqualizeWidths();
            nudDefaultWidthPx.ValueChanged += (_, __) => ApplyDefaultWidthToEmptyCells();

            dgvWidths.CellValidating += DgvWidths_CellValidating;

            btnOk.Click += (_, __) => OnInsert();
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            OnPickerSelectionChanged();
        }

        private void OnPickerSelectionChanged()
        {
            // Real-time display (hover when unlocked)
            lblSelected.Text = $"Selection: {SelectedCols} x {SelectedRows}";

            // Only rebuild width list when manual widths are enabled
            if (!chkFitToEditor.Checked)
                EnsureWidthRowsForColumns(SelectedCols);
        }

        private void OnWidthModeChanged()
        {
            bool fit = chkFitToEditor.Checked;

            dgvWidths.Enabled = !fit;
            nudDefaultWidthPx.Enabled = !fit;
            btnEqualize.Enabled = !fit;

            if (!fit)
            {
                _lastColsForWidths = -1; // force refresh
                EnsureWidthRowsForColumns(SelectedCols);
                ApplyDefaultWidthToEmptyCells();
            }
        }

        private void EnsureWidthRowsForColumns(int cols)
        {
            if (cols <= 0)
            {
                dgvWidths.Rows.Clear();
                _lastColsForWidths = 0;
                return;
            }

            if (cols == _lastColsForWidths)
                return;

            // Preserve existing entered widths (by column index)
            int oldCount = dgvWidths.Rows.Count;
            string[] oldWidths = new string[Math.Max(oldCount, 0)];
            for (int i = 0; i < oldCount; i++)
                oldWidths[i] = (dgvWidths.Rows[i].Cells[1].Value?.ToString() ?? "").Trim();

            dgvWidths.SuspendLayout();
            try
            {
                dgvWidths.Rows.Clear();

                for (int i = 0; i < cols; i++)
                {
                    int row = dgvWidths.Rows.Add();
                    dgvWidths.Rows[row].Cells[0].Value = (i + 1).ToString(CultureInfo.InvariantCulture);

                    // restore if possible
                    string restored = (i < oldWidths.Length) ? oldWidths[i] : "";
                    dgvWidths.Rows[row].Cells[1].Value = restored;
                }
            }
            finally
            {
                dgvWidths.ResumeLayout();
            }

            _lastColsForWidths = cols;

            ApplyDefaultWidthToEmptyCells();
        }

        private void EqualizeWidths()
        {
            if (SelectedCols <= 0) return;

            int w = (int)nudDefaultWidthPx.Value;
            for (int i = 0; i < dgvWidths.Rows.Count; i++)
                dgvWidths.Rows[i].Cells[1].Value = w.ToString(CultureInfo.InvariantCulture);
        }

        private void ApplyDefaultWidthToEmptyCells()
        {
            if (SelectedCols <= 0) return;

            int w = (int)nudDefaultWidthPx.Value;
            for (int i = 0; i < dgvWidths.Rows.Count; i++)
            {
                var val = dgvWidths.Rows[i].Cells[1].Value?.ToString();
                if (string.IsNullOrWhiteSpace(val))
                    dgvWidths.Rows[i].Cells[1].Value = w.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void DgvWidths_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex != 1) return;

            string raw = (e.FormattedValue?.ToString() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw)) return;

            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int px) || px < 20 || px > 2000)
            {
                e.Cancel = true;
                MessageBox.Show(this, "Please enter a width between 20 and 2000 (pixels).", "Invalid width",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnInsert()
        {
            if (SelectedCols <= 0 || SelectedRows <= 0)
            {
                MessageBox.Show(this, "Please select a table size (rows and columns).", "Insert table",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int[] colEdgesTwips = chkFitToEditor.Checked
                ? BuildEdgesFitToEditor(SelectedCols)
                : BuildEdgesFromGrid(SelectedCols);

            ResultRtf = BuildRtfTable(
                rows: SelectedRows,
                cols: SelectedCols,
                colRightEdgesTwips: colEdgesTwips,
                cellTexts: null);

            DialogResult = DialogResult.OK;
            Close();
        }

        private int[] BuildEdgesFitToEditor(int cols)
        {
            int usablePx = Math.Max(60, _editorClientWidthPx - 24);
            int perColPx = Math.Max(20, usablePx / cols);

            int[] widthsPx = Enumerable.Repeat(perColPx, cols).ToArray();
            return WidthsPxToRightEdgesTwips(widthsPx);
        }

        private int[] BuildEdgesFromGrid(int cols)
        {
            int[] widthsPx = new int[cols];
            int defaultPx = (int)nudDefaultWidthPx.Value;

            for (int i = 0; i < cols; i++)
            {
                string raw = dgvWidths.Rows[i].Cells[1].Value?.ToString()?.Trim() ?? "";
                if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int px) || px < 20 || px > 2000)
                    px = defaultPx;

                widthsPx[i] = px;
            }

            return WidthsPxToRightEdgesTwips(widthsPx);
        }

        private int[] WidthsPxToRightEdgesTwips(int[] widthsPx)
        {
            int sum = 0;
            int[] edges = new int[widthsPx.Length];

            for (int i = 0; i < widthsPx.Length; i++)
            {
                int tw = (int)Math.Round(widthsPx[i] * 1440f / _dpiX);
                if (tw < 120) tw = 120;
                sum += tw;
                edges[i] = sum;
            }

            return edges;
        }

        // ============================================================
        // RTF table generator
        // ============================================================
        private static string BuildRtfTable(int rows, int cols, int[] colRightEdgesTwips, string[,]? cellTexts)
        {
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
            if (cols <= 0) throw new ArgumentOutOfRangeException(nameof(cols));
            if (colRightEdgesTwips == null) throw new ArgumentNullException(nameof(colRightEdgesTwips));
            if (colRightEdgesTwips.Length != cols) throw new ArgumentException("Column edge array length must match cols.");

            var sb = new StringBuilder();
            sb.Append(@"{\rtf1\ansi\deff0");
            sb.Append(@"{\fonttbl{\f0 Segoe UI;}}");
            sb.Append(@"\fs20 ");

            for (int r = 0; r < rows; r++)
            {
                sb.Append(@"\trowd\trgaph108\trleft0 ");

                for (int c = 0; c < cols; c++)
                {
                    sb.Append(@"\cellx");
                    sb.Append(colRightEdgesTwips[c]);
                    sb.Append(' ');
                }

                for (int c = 0; c < cols; c++)
                {
                    sb.Append(@"\intbl ");

                    string text = cellTexts != null ? (cellTexts[r, c] ?? string.Empty) : string.Empty;
                    sb.Append(EscapeRtfText(text));

                    sb.Append(@"\cell ");
                }

                sb.Append(@"\row ");
            }

            sb.Append("}");
            return sb.ToString();
        }

        private static string EscapeRtfText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var sb = new StringBuilder(text.Length);
            foreach (char ch in text)
            {
                switch (ch)
                {
                    case '\\': sb.Append(@"\\"); break;
                    case '{': sb.Append(@"\{"); break;
                    case '}': sb.Append(@"\}"); break;
                    case '\r': break;
                    case '\n': sb.Append(@"\line "); break;
                    default:
                        if (ch <= 0x7f) sb.Append(ch);
                        else sb.Append(@"\u").Append((short)ch).Append('?');
                        break;
                }
            }
            return sb.ToString();
        }
    }

    // ============================================================
    // TableSizePicker (mouse-based selection grid)
    // - Hover updates selection in real time (PreviewCols/PreviewRows)
    // - Click toggles lock/unlock (no Esc required)
    // ============================================================
    public sealed class TableSizePicker : Control
    {
        public event EventHandler? SelectionChanged;

        private int _maxCols = 10;
        private int _maxRows = 8;

        private int _hoverCols = 0;
        private int _hoverRows = 0;

        private int _lockedCols = 0;
        private int _lockedRows = 0;

        private bool _locked = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int MaxCols
        {
            get => _maxCols;
            set { _maxCols = Math.Max(1, value); Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int MaxRows
        {
            get => _maxRows;
            set { _maxRows = Math.Max(1, value); Invalidate(); }
        }

        public bool IsLocked => _locked;

        // Preview = what the user currently sees (hover if unlocked, locked if locked)
        public int PreviewCols => _locked ? _lockedCols : _hoverCols;
        public int PreviewRows => _locked ? _lockedRows : _hoverRows;

        public TableSizePicker()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw, true);

            TabStop = true;

            MouseMove += TableSizePicker_MouseMove;

            MouseLeave += (_, __) =>
            {
                if (_locked) return;

                if (_hoverCols != 0 || _hoverRows != 0)
                {
                    _hoverCols = 0;
                    _hoverRows = 0;
                    Invalidate();
                    RaiseChanged();
                }
            };
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Toggle lock/unlock with click (no Esc required)
            if (_locked)
            {
                _locked = false;
                Invalidate();
                RaiseChanged();
                return;
            }

            // Lock current hover selection
            _locked = true;
            _lockedCols = _hoverCols;
            _lockedRows = _hoverRows;
            Invalidate();
            RaiseChanged();
        }

        private void TableSizePicker_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_locked)
                return;

            GetCellFromPoint(e.Location, out int c, out int r);

            if (c != _hoverCols || r != _hoverRows)
            {
                _hoverCols = c;
                _hoverRows = r;
                Invalidate();
                RaiseChanged();
            }
        }

        private void GetCellFromPoint(Point p, out int cols, out int rows)
        {
            cols = 0;
            rows = 0;

            int pad = 6;
            int gap = 4;

            int w = Math.Max(1, ClientSize.Width - pad * 2);
            int h = Math.Max(1, ClientSize.Height - pad * 2);

            int cellW = Math.Max(8, (w - (_maxCols - 1) * gap) / _maxCols);
            int cellH = Math.Max(8, (h - (_maxRows - 1) * gap) / _maxRows);

            int x = p.X - pad;
            int y = p.Y - pad;

            if (x < 0 || y < 0) return;

            for (int c = 1; c <= _maxCols; c++)
            {
                int right = c * cellW + (c - 1) * gap;
                if (x < right) { cols = c; break; }
            }

            for (int r = 1; r <= _maxRows; r++)
            {
                int bottom = r * cellH + (r - 1) * gap;
                if (y < bottom) { rows = r; break; }
            }

            if (cols < 0) cols = 0;
            if (rows < 0) rows = 0;
            if (cols > _maxCols) cols = _maxCols;
            if (rows > _maxRows) rows = _maxRows;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(SystemColors.Window);

            int pad = 6;
            int gap = 4;

            int w = Math.Max(1, ClientSize.Width - pad * 2);
            int h = Math.Max(1, ClientSize.Height - pad * 2);

            int cellW = Math.Max(8, (w - (_maxCols - 1) * gap) / _maxCols);
            int cellH = Math.Max(8, (h - (_maxRows - 1) * gap) / _maxRows);

            int hiCols = PreviewCols;
            int hiRows = PreviewRows;

            using var pen = new Pen(SystemColors.ControlDark);
            using var fill = new SolidBrush(Color.FromArgb(60, SystemColors.Highlight));
            using var fillLocked = new SolidBrush(Color.FromArgb(90, SystemColors.Highlight));

            for (int r = 0; r < _maxRows; r++)
            {
                for (int c = 0; c < _maxCols; c++)
                {
                    int x = pad + c * (cellW + gap);
                    int y = pad + r * (cellH + gap);

                    var rect = new Rectangle(x, y, cellW, cellH);

                    bool selected = (c + 1) <= hiCols && (r + 1) <= hiRows;
                    if (selected)
                        e.Graphics.FillRectangle(_locked ? fillLocked : fill, rect);

                    e.Graphics.DrawRectangle(pen, rect);
                }
            }

            using var f = new Font(Font.FontFamily, Math.Max(8f, Font.Size - 1f));
            string status = _locked
                ? "Locked. Click to unlock and change the selection."
                : "Hover to preview. Click to lock the selection.";
            var sz = e.Graphics.MeasureString(status, f);
            e.Graphics.DrawString(status, f, SystemBrushes.GrayText, new PointF(2, ClientSize.Height - sz.Height - 2));
        }

        private void RaiseChanged()
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
