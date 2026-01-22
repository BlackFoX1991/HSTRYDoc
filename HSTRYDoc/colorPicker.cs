// colorPicker.cs
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HSTRYDoc
{
    public partial class colorPicker : Form
    {
        private const int CS_DROPSHADOW = 0x00020000;

        private Bitmap? _wheelBitmap;
        private bool _mouseDown;
        private bool _updatingUi;
        private PointF _markerPos;
        private float _wheelRadius;
        private PointF _wheelCenter;

        // Popup Verhalten
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool CloseOnDeactivate { get; set; } = true;

        // Border Optik
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color BorderColor { get; set; } = SystemColors.ActiveBorder;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int BorderThickness { get; set; } = 1;

        /// <summary>Ausgewählte Farbe (inkl. Alpha).</summary>
        public Color SelectedColor { get; private set; } = Color.FromArgb(255, 255, 0, 0);

        /// <summary>Ausgewählter ARGB Wert (wie Color.ToArgb()).</summary>
        public int SelectedArgb => SelectedColor.ToArgb();

        /// <summary>Hex-String (#AARRGGBB).</summary>
        public string SelectedHex => $"#{SelectedColor.A:X2}{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;

                // klassischer Schatten für borderless Form
                cp.ClassStyle |= CS_DROPSHADOW;

                return cp;
            }
        }

        public colorPicker()
        {
            InitializeComponent();

            // für Popup: nicht im Taskbar, immer oben (falls nicht im Designer gesetzt)
            ShowInTaskbar = false;
            TopMost = true;

            Load += ColorPicker_Load;
            FormClosed += ColorPicker_FormClosed;

            picWheel.Paint += PicWheel_Paint;
            picWheel.MouseDown += PicWheel_MouseDown;
            picWheel.MouseMove += PicWheel_MouseMove;
            picWheel.MouseUp += PicWheel_MouseUp;
            picWheel.Resize += PicWheel_Resize;

            numR.ValueChanged += NumRGBA_ValueChanged;
            numG.ValueChanged += NumRGBA_ValueChanged;
            numB.ValueChanged += NumRGBA_ValueChanged;
            numA.ValueChanged += NumRGBA_ValueChanged;

            txtHex.KeyDown += TxtHex_KeyDown;
            txtHex.Leave += TxtHex_Leave;

            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            numA.Value = 255;

            // borderless Forms profitieren von explizitem BackColor
            BackColor = SystemColors.Window;
        }

        public colorPicker(Color initialColor) : this()
        {
            SelectedColor = initialColor;
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);

            if (!CloseOnDeactivate) return;
            if (!Visible || IsDisposed) return;

            // Fokus weg => wie Cancel schließen (keine Übernahme)
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // 1px Border innerhalb des ClientRect
            if (BorderThickness <= 0) return;

            Rectangle r = ClientRectangle;
            r.Width -= 1;
            r.Height -= 1;

            using var pen = new Pen(BorderColor, BorderThickness);
            e.Graphics.DrawRectangle(pen, r);
        }

        private void ColorPicker_Load(object? sender, EventArgs e)
        {
            BuildWheel();
            UpdateUiFromColor(SelectedColor, updateMarker: true);
        }

        private void ColorPicker_FormClosed(object? sender, FormClosedEventArgs e)
        {
            _wheelBitmap?.Dispose();
            _wheelBitmap = null;
        }

        private void PicWheel_Resize(object? sender, EventArgs e)
        {
            BuildWheel();
            UpdateMarkerFromColor(SelectedColor);
            picWheel.Invalidate();
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BuildWheel()
        {
            int size = Math.Min(picWheel.Width, picWheel.Height);
            if (size <= 10) return;

            _wheelRadius = (size - 2) / 2f;
            _wheelCenter = new PointF(picWheel.Width / 2f, picWheel.Height / 2f);

            _wheelBitmap?.Dispose();
            _wheelBitmap = CreateColorWheelBitmap(size);

            picWheel.Image = _wheelBitmap;
            picWheel.Invalidate();
        }

        private static Bitmap CreateColorWheelBitmap(int size)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);

            Rectangle rect = new Rectangle(0, 0, size, size);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            int stride = data.Stride;
            byte[] buffer = new byte[stride * size];

            float cx = (size - 1) / 2f;
            float cy = (size - 1) / 2f;
            float rMax = (size - 2) / 2f;

            for (int y = 0; y < size; y++)
            {
                float dy = y - cy;
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    int idx = (y * stride) + (x * 4);

                    if (dist <= rMax)
                    {
                        float sat = dist / rMax;
                        float hue = (float)(Math.Atan2(dy, dx) * (180.0 / Math.PI));
                        if (hue < 0) hue += 360f;

                        Color c = HsvToColor(hue, sat, 1f, 255);

                        buffer[idx + 0] = c.B;   // BGRA
                        buffer[idx + 1] = c.G;
                        buffer[idx + 2] = c.R;
                        buffer[idx + 3] = 255;
                    }
                    else
                    {
                        buffer[idx + 0] = 0;
                        buffer[idx + 1] = 0;
                        buffer[idx + 2] = 0;
                        buffer[idx + 3] = 0; // transparent
                    }
                }
            }

            Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
            bmp.UnlockBits(data);

            return bmp;
        }

        private void PicWheel_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _mouseDown = true;
            UpdateFromWheelPoint(e.Location);
        }

        private void PicWheel_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_mouseDown) return;
            UpdateFromWheelPoint(e.Location);
        }

        private void PicWheel_MouseUp(object? sender, MouseEventArgs e)
        {
            _mouseDown = false;
        }

        private void UpdateFromWheelPoint(Point p)
        {
            float dx = p.X - _wheelCenter.X;
            float dy = p.Y - _wheelCenter.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist > _wheelRadius) return;

            float sat = dist / _wheelRadius;
            float hue = (float)(Math.Atan2(dy, dx) * (180.0 / Math.PI));
            if (hue < 0) hue += 360f;

            int a = (int)numA.Value;
            Color rgb = HsvToColor(hue, sat, 1f, a);

            _markerPos = p;
            SetSelectedColor(rgb, updateMarker: false);
            picWheel.Invalidate();
        }

        private void PicWheel_Paint(object? sender, PaintEventArgs e)
        {
            float r = 6f;
            RectangleF rc = new RectangleF(_markerPos.X - r, _markerPos.Y - r, r * 2, r * 2);

            using (var pen1 = new Pen(Color.Black, 2f))
                e.Graphics.DrawEllipse(pen1, rc);

            RectangleF rc2 = new RectangleF(_markerPos.X - (r - 2f), _markerPos.Y - (r - 2f), (r - 2f) * 2, (r - 2f) * 2);
            using (var pen2 = new Pen(Color.White, 1f))
                e.Graphics.DrawEllipse(pen2, rc2);
        }

        private void NumRGBA_ValueChanged(object? sender, EventArgs e)
        {
            if (_updatingUi) return;

            int r = (int)numR.Value;
            int g = (int)numG.Value;
            int b = (int)numB.Value;
            int a = (int)numA.Value;

            SetSelectedColor(Color.FromArgb(a, r, g, b), updateMarker: true);
        }

        private void TxtHex_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                TryApplyHex();
            }
        }

        private void TxtHex_Leave(object? sender, EventArgs e) => TryApplyHex();

        private void TryApplyHex()
        {
            if (_updatingUi) return;

            string raw = (txtHex.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw)) return;

            if (raw.StartsWith("#", StringComparison.Ordinal))
                raw = raw.Substring(1);

            if (raw.Length != 6 && raw.Length != 8) return;

            if (!uint.TryParse(raw, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value))
                return;

            Color c;
            if (raw.Length == 6)
            {
                int r = (int)((value >> 16) & 0xFF);
                int g = (int)((value >> 8) & 0xFF);
                int b = (int)(value & 0xFF);
                c = Color.FromArgb(255, r, g, b);
            }
            else
            {
                int a = (int)((value >> 24) & 0xFF);
                int r = (int)((value >> 16) & 0xFF);
                int g = (int)((value >> 8) & 0xFF);
                int b = (int)(value & 0xFF);
                c = Color.FromArgb(a, r, g, b);
            }

            SetSelectedColor(c, updateMarker: true);
        }

        private void SetSelectedColor(Color c, bool updateMarker)
        {
            SelectedColor = c;
            UpdateUiFromColor(c, updateMarker);
        }

        private void UpdateUiFromColor(Color c, bool updateMarker)
        {
            _updatingUi = true;
            try
            {
                numR.Value = c.R;
                numG.Value = c.G;
                numB.Value = c.B;
                numA.Value = c.A;

                txtHex.Text = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
                pnlPreview.BackColor = Color.FromArgb(255, c.R, c.G, c.B);

                if (updateMarker)
                {
                    UpdateMarkerFromColor(c);
                    picWheel.Invalidate();
                }
            }
            finally
            {
                _updatingUi = false;
            }
        }

        private void UpdateMarkerFromColor(Color c)
        {
            ColorToHsv(c, out float h, out float s, out _);

            float angleRad = (float)(h * Math.PI / 180.0);
            float rr = s * _wheelRadius;

            float x = _wheelCenter.X + (float)(Math.Cos(angleRad) * rr);
            float y = _wheelCenter.Y + (float)(Math.Sin(angleRad) * rr);

            _markerPos = new PointF(x, y);
        }

        private static Color HsvToColor(float h, float s, float v, int a)
        {
            h = h % 360f;
            if (h < 0) h += 360f;

            s = Clamp01(s);
            v = Clamp01(v);

            float c = v * s;
            float x = c * (1f - Math.Abs(((h / 60f) % 2f) - 1f));
            float m = v - c;

            float r1, g1, b1;

            if (h < 60f) { r1 = c; g1 = x; b1 = 0; }
            else if (h < 120f) { r1 = x; g1 = c; b1 = 0; }
            else if (h < 180f) { r1 = 0; g1 = c; b1 = x; }
            else if (h < 240f) { r1 = 0; g1 = x; b1 = c; }
            else if (h < 300f) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }

            int rr = Clamp255((int)Math.Round((r1 + m) * 255f));
            int gg = Clamp255((int)Math.Round((g1 + m) * 255f));
            int bb = Clamp255((int)Math.Round((b1 + m) * 255f));
            a = Clamp255(a);

            return Color.FromArgb(a, rr, gg, bb);
        }

        private static void ColorToHsv(Color c, out float h, out float s, out float v)
        {
            float r = c.R / 255f;
            float g = c.G / 255f;
            float b = c.B / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            if (delta == 0) h = 0;
            else if (max == r) h = 60f * (((g - b) / delta) % 6f);
            else if (max == g) h = 60f * (((b - r) / delta) + 2f);
            else h = 60f * (((r - g) / delta) + 4f);

            if (h < 0) h += 360f;

            s = (max == 0) ? 0 : (delta / max);
            v = max;
        }

        private static float Clamp01(float x) => x < 0f ? 0f : (x > 1f ? 1f : x);
        private static int Clamp255(int x) => x < 0 ? 0 : (x > 255 ? 255 : x);
    }
}
