using System.ComponentModel;

namespace HSTRYDoc;

public sealed class ShadowPanel : Panel
{
    private int _shadowSize = 8;
    private int _shadowOffsetX = 3;
    private int _shadowOffsetY = 3;
    private int _maxAlpha = 140;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int ShadowSize
    {
        get => _shadowSize;
        set { _shadowSize = Math.Max(0, value); UpdatePadding(); Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int ShadowOffsetX
    {
        get => _shadowOffsetX;
        set { _shadowOffsetX = Math.Max(0, value); UpdatePadding(); Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int ShadowOffsetY
    {
        get => _shadowOffsetY;
        set { _shadowOffsetY = Math.Max(0, value); UpdatePadding(); Invalidate(); }
    }

    // dunklerer Schatten (0..255)
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int MaxAlpha
    {
        get => _maxAlpha;
        set { _maxAlpha = Math.Clamp(value, 0, 255); Invalidate(); }
    }

    public ShadowPanel()
    {
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        BackColor = Color.Transparent;
        UpdatePadding();
    }

    private void UpdatePadding()
    {
        // Content liegt oben/links, Schatten braucht Platz rechts/unten
        Padding = new Padding(0, 0, _shadowSize + _shadowOffsetX, _shadowSize + _shadowOffsetY);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Content-Rechteck (da, wo deine Child-Controls liegen)
        Rectangle content = ClientRectangle;
        content.Width -= Padding.Right;
        content.Height -= Padding.Bottom;

        if (content.Width <= 0 || content.Height <= 0) return;
        if (_shadowSize <= 0) return;

        // Schatten: rechts + unten, mit Verlauf (innen dunkler, außen heller)
        for (int i = 0; i < _shadowSize; i++)
        {
            float t = 1f - (i / (float)_shadowSize);     // 1..0
            int a = (int)(_maxAlpha * t * t);            // quadratisch = schöner Verlauf

            using var b = new SolidBrush(Color.FromArgb(a, 0, 0, 0));

            // rechter Streifen
            int x = content.Right + _shadowOffsetX + i;
            int y = content.Top + _shadowOffsetY;
            int h = content.Height + (_shadowSize - i);  // nach unten auslaufend
            e.Graphics.FillRectangle(b, x, y, 1, h);

            // unterer Streifen
            int bx = content.Left + _shadowOffsetX;
            int by = content.Bottom + _shadowOffsetY + i;
            int w = content.Width + (_shadowSize - i);   // nach rechts auslaufend
            e.Graphics.FillRectangle(b, bx, by, w, 1);
        }
    }
}
