using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace HSTRYDoc;

public sealed class ShadowPanel : Panel
{
    private int _shadowSize = 12;
    private int _shadowOffsetX = 5;
    private int _shadowOffsetY = 5;
    private int _maxAlpha = 44;
    private int _cornerRadius = 0;
    private Color _pageBackColor = Color.White;
    private Color _pageBorderColor = Color.FromArgb(226, 229, 234);

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

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = Math.Max(0, value); Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color PageBackColor
    {
        get => _pageBackColor;
        set { _pageBackColor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color PageBorderColor
    {
        get => _pageBorderColor;
        set { _pageBorderColor = value; Invalidate(); }
    }

    public ShadowPanel()
    {
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);

        BackColor = Color.Transparent;
        UpdatePadding();
    }

    private void UpdatePadding()
    {
        Padding = new Padding(
            left: 1,
            top: 1,
            right: _shadowSize + _shadowOffsetX,
            bottom: _shadowSize + _shadowOffsetY);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Rectangle pageRect = new(
            Padding.Left,
            Padding.Top,
            ClientSize.Width - Padding.Horizontal,
            ClientSize.Height - Padding.Vertical);

        if (pageRect.Width <= 0 || pageRect.Height <= 0)
            return;

        e.Graphics.SmoothingMode = _cornerRadius > 0 ? SmoothingMode.AntiAlias : SmoothingMode.None;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        DrawShadow(e.Graphics, pageRect);

        using (GraphicsPath pagePath = CreatePath(pageRect))
        using (SolidBrush pageBrush = new(_pageBackColor))
        using (Pen borderPen = new(_pageBorderColor))
        {
            e.Graphics.FillPath(pageBrush, pagePath);
            e.Graphics.DrawPath(borderPen, pagePath);
        }

        base.OnPaint(e);
    }

    private void DrawShadow(Graphics graphics, Rectangle pageRect)
    {
        if (_shadowSize <= 0 || _maxAlpha <= 0)
            return;

        Rectangle rightShadow = new(
            pageRect.Right + _shadowOffsetX - 1,
            pageRect.Top + 2,
            _shadowSize,
            pageRect.Height + _shadowOffsetY - 2);

        Rectangle bottomShadow = new(
            pageRect.Left + 2,
            pageRect.Bottom + _shadowOffsetY - 1,
            pageRect.Width + _shadowOffsetX - 2,
            _shadowSize);

        Rectangle cornerShadow = new(
            pageRect.Right + _shadowOffsetX - 1,
            pageRect.Bottom + _shadowOffsetY - 1,
            _shadowSize,
            _shadowSize);

        using (LinearGradientBrush rightBrush = new(
            rightShadow,
            Color.FromArgb(_maxAlpha, 128, 134, 145),
            Color.FromArgb(0, 128, 134, 145),
            LinearGradientMode.Horizontal))
        {
            graphics.FillRectangle(rightBrush, rightShadow);
        }

        using (LinearGradientBrush bottomBrush = new(
            bottomShadow,
            Color.FromArgb(_maxAlpha, 128, 134, 145),
            Color.FromArgb(0, 128, 134, 145),
            LinearGradientMode.Vertical))
        {
            graphics.FillRectangle(bottomBrush, bottomShadow);
        }

        using (GraphicsPath cornerPath = new())
        {
            cornerPath.AddRectangle(cornerShadow);
            using PathGradientBrush cornerBrush = new(cornerPath)
            {
                CenterColor = Color.FromArgb(_maxAlpha, 128, 134, 145),
                SurroundColors = new[] { Color.FromArgb(0, 128, 134, 145) }
            };
            graphics.FillRectangle(cornerBrush, cornerShadow);
        }
    }

    private GraphicsPath CreatePath(Rectangle rect)
    {
        GraphicsPath path = new();

        if (_cornerRadius <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        int diameter = _cornerRadius * 2;
        Rectangle arc = new(rect.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        return path;
    }
}
