using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HSTRYDoc
{
    public sealed class RtfAutoComplete : IDisposable
    {
        private readonly RichTextBox _rtb;
        private readonly ToolStripDropDown _dropDown;
        private readonly ToolStripControlHost _host;
        private readonly ListBox _list;

        private IEnumerable<string> _source = Array.Empty<string>();
        private bool _isInserting;
        private int _wordStart;
        private string _prefix = string.Empty;

        public int MaxItems { get; set; } = 20;
        public int MinPrefixLength { get; set; } = 1;

        public RtfAutoComplete(RichTextBox rtb, IEnumerable<string> source)
        {
            _rtb = rtb ?? throw new ArgumentNullException(nameof(rtb));
            SetSource(source);

            _list = new ListBox
            {
                IntegralHeight = true,
                BorderStyle = BorderStyle.None
            };

            _list.MouseDoubleClick += (_, __) => AcceptSelection();
            _list.KeyDown += List_KeyDown;

            _host = new ToolStripControlHost(_list)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false
            };

            _dropDown = new ToolStripDropDown
            {
                Padding = Padding.Empty,
                AutoClose = false
            };
            _dropDown.Items.Add(_host);

            // Hook editor events
            _rtb.KeyDown += Rtb_KeyDown;
            _rtb.TextChanged += Rtb_TextChanged;
            _rtb.LostFocus += (_, __) => Hide();
            _rtb.MouseDown += (_, __) => Hide();
            _rtb.VScroll += (_, __) => Hide();
            _rtb.HScroll += (_, __) => Hide();
        }

        public void SetSource(IEnumerable<string> source)
        {
            _source = (source ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public void Dispose()
        {
            Hide();

            _rtb.KeyDown -= Rtb_KeyDown;
            _rtb.TextChanged -= Rtb_TextChanged;

            _dropDown.Dispose();
            _host.Dispose();
            _list.Dispose();
        }

        private void Rtb_TextChanged(object? sender, EventArgs e)
        {
            if (_isInserting) return;
            UpdateSuggestions();
        }

        private void Rtb_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!_dropDown.Visible)
            {
                // Optional: Ctrl+Space to force open
                if (e.Control && e.KeyCode == Keys.Space)
                {
                    UpdateSuggestions(force: true);
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                }
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Down:
                    MoveSelection(+1);
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    break;

                case Keys.Up:
                    MoveSelection(-1);
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    break;

                case Keys.PageDown:
                    MoveSelection(+5);
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    break;

                case Keys.PageUp:
                    MoveSelection(-5);
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    break;

                case Keys.Enter:
                case Keys.Tab:
                    AcceptSelection();
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    break;

                case Keys.Escape:
                    Hide();
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    break;

                default:
                    // Let typing continue; TextChanged will filter.
                    // But hide on hard separators immediately (optional).
                    if (IsHardSeparatorKey(e.KeyCode))
                        Hide();
                    break;
            }
        }

        private void List_KeyDown(object? sender, KeyEventArgs e)
        {
            // If list somehow has focus
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
            {
                AcceptSelection();
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Hide();
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        private static bool IsHardSeparatorKey(Keys k)
        {
            return k == Keys.Space || k == Keys.OemPeriod || k == Keys.Oemcomma ||
                   k == Keys.OemSemicolon || k == Keys.OemQuestion || k == Keys.OemQuotes ||
                   k == Keys.OemOpenBrackets || k == Keys.OemCloseBrackets ||
                   k == Keys.OemPipe || k == Keys.OemMinus || k == Keys.Oemplus;
        }

        private void UpdateSuggestions(bool force = false)
        {
            if (_source == null) return;

            if (!TryGetCurrentWord(out _wordStart, out _prefix))
            {
                Hide();
                return;
            }

            if (!force && _prefix.Length < MinPrefixLength)
            {
                Hide();
                return;
            }

            var matches = _source
                .Where(s => s.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Take(MaxItems)
                .ToArray();

            if (matches.Length == 0)
            {
                Hide();
                return;
            }

            _list.BeginUpdate();
            try
            {
                _list.Items.Clear();
                _list.Items.AddRange(matches);
                _list.SelectedIndex = 0;
            }
            finally
            {
                _list.EndUpdate();
            }

            ShowAtCaret();
        }

        private bool TryGetCurrentWord(out int wordStart, out string prefix)
        {
            wordStart = 0;
            prefix = string.Empty;

            int caret = _rtb.SelectionStart;
            if (caret <= 0 || _rtb.TextLength == 0)
                return false;

            string text = _rtb.Text;

            int i = Math.Min(caret, text.Length);
            int start = i;

            // Scan left to find word start
            while (start > 0 && IsWordChar(text[start - 1]))
                start--;

            // If no word chars to the left, no prefix
            if (start == i)
                return false;

            wordStart = start;
            prefix = text.Substring(start, i - start);
            return true;
        }

        private static bool IsWordChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' ||
                   c == 'Ä' || c == 'Ö' || c == 'Ü' ||
                   c == 'ä' || c == 'ö' || c == 'ü' || c == 'ß';
        }

        private void ShowAtCaret()
        {

            SyncFontFromCaret();
            if (_dropDown.Visible)
            {
                Reposition();
                return;
            }

            Reposition();
            _dropDown.Show(_rtb, GetPopupLocationInRtbClient());
        }

        private void Reposition()
        {
            _host.Size = GetPopupSize();
            _dropDown.Size = _host.Size;
        }

        private Size GetPopupSize()
        {
            int width = 320;
            int itemHeight = _list.ItemHeight <= 0 ? 16 : _list.ItemHeight;
            int visibleItems = Math.Min(_list.Items.Count, MaxItems);
            int height = Math.Max(itemHeight * Math.Max(visibleItems, 1) + 2, itemHeight + 2);

            // Keep it reasonable
            height = Math.Min(height, 240);
            return new Size(width, height);
        }

        private void SyncFontFromCaret()
        {
            // Determine the font at the caret position (or nearest char)
            int origStart = _rtb.SelectionStart;
            int origLen = _rtb.SelectionLength;

            try
            {
                int probeStart = origStart;
                int probeLen = origLen;

                if (probeLen == 0)
                {
                    if (probeStart < _rtb.TextLength) probeLen = 1;
                    else if (probeStart > 0) { probeStart--; probeLen = 1; }
                    else probeLen = 0;
                }

                Font f = _rtb.Font;

                if (probeLen > 0)
                {
                    _rtb.Select(probeStart, probeLen);
                    f = _rtb.SelectionFont ?? _rtb.Font;
                }

                // Only apply if changed (avoids flicker + recalculations)
                if (_list.Font == null ||
                    _list.Font.FontFamily.Name != f.FontFamily.Name ||
                    Math.Abs(_list.Font.Size - f.Size) > 0.01f ||
                    _list.Font.Style != f.Style)
                {
                    _list.Font = f;
                }
            }
            finally
            {
                _rtb.Select(origStart, origLen);
            }
        }


        private Point GetPopupLocationInRtbClient()
        {
            int caret = _rtb.SelectionStart;
            Point p = _rtb.GetPositionFromCharIndex(caret);

            // place under caret
            int lineHeight = TextRenderer.MeasureText("Ag", _rtb.Font).Height;
            p.Y += lineHeight;

            // keep inside client width
            var size = GetPopupSize();
            if (p.X + size.Width > _rtb.ClientSize.Width)
                p.X = Math.Max(0, _rtb.ClientSize.Width - size.Width);

            if (p.Y + size.Height > _rtb.ClientSize.Height)
                p.Y = Math.Max(0, _rtb.ClientSize.Height - size.Height);

            return p;
        }

        private void MoveSelection(int delta)
        {
            if (_list.Items.Count == 0) return;
            int idx = _list.SelectedIndex;
            if (idx < 0) idx = 0;

            idx = Math.Max(0, Math.Min(_list.Items.Count - 1, idx + delta));
            _list.SelectedIndex = idx;
        }

        private void AcceptSelection()
        {
            if (_list.SelectedItem is not string selected)
            {
                Hide();
                return;
            }

            int caret = _rtb.SelectionStart;
            if (_wordStart < 0 || _wordStart > caret) { Hide(); return; }

            _isInserting = true;
            try
            {
                _rtb.Select(_wordStart, caret - _wordStart);
                _rtb.SelectedText = selected;
                _rtb.Select(_wordStart + selected.Length, 0);
            }
            finally
            {
                _isInserting = false;
            }

            Hide();
        }

        private void Hide()
        {
            if (_dropDown.Visible)
                _dropDown.Close();
        }
    }
}
