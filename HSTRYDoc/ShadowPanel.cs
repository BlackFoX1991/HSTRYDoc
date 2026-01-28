using System.ComponentModel;
using System.Drawing.Drawing2D;

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
        // Schatten auf allen Seiten: links/oben mind. ShadowSize,
        // rechts/unten ShadowSize + Offset (weil du den Schatten nach rechts/unten schiebst)
        Padding = new Padding(
            left: _shadowSize,
            top: _shadowSize,
            right: _shadowSize + _shadowOffsetX,
            bottom: _shadowSize + _shadowOffsetY
        );
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_shadowSize <= 0) return;

        // Content-Rechteck: dort liegen die Child-Controls
        var content = new Rectangle(
            Padding.Left,
            Padding.Top,
            ClientSize.Width - Padding.Horizontal,
            ClientSize.Height - Padding.Vertical
        );

        if (content.Width <= 0 || content.Height <= 0) return;

        // Schattenbasis = Content, um Offset verschoben (Drop-Shadow nach rechts/unten)
        var shadowBase = content;
        shadowBase.Offset(_shadowOffsetX, _shadowOffsetY);

        // Für saubere 1px-Ringe
        var oldSmoothing = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.None;

        // Schatten als konzentrische Rechtecke (1px pro Schritt)
        // i=1..ShadowSize => außen heller, innen dunkler
        for (int i = 1; i <= _shadowSize; i++)
        {
            float t = 1f - (i / (float)_shadowSize); // ~1..0
            int a = (int)(_maxAlpha * t * t);

            using var pen = new Pen(Color.FromArgb(a, 0, 0, 0), 1f);

            var r = shadowBase;
            r.Inflate(i, i);

            // -1, damit das Rechteck sauber innerhalb der Pixelgrenzen liegt
            r.Width -= 1;
            r.Height -= 1;

            e.Graphics.DrawRectangle(pen, r);
        }

        e.Graphics.SmoothingMode = oldSmoothing;
    }
}
