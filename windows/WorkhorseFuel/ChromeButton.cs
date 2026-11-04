using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace WorkhorseFuel;

internal sealed class ChromeButton : Control
{
    private bool hovered;
    private bool pressed;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal Color FillColor { get; set; } = Color.Transparent;

    public ChromeButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor |
            ControlStyles.Selectable | ControlStyles.StandardClick, true);
        BackColor = Color.Transparent;
        ForeColor = Color.White;
        AutoSize = true;
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
        Padding = new Padding(6, 4, 6, 4);
        MinimumSize = new Size(28, 28);
        Cursor = Cursors.Hand;
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var text = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding);
        return new Size(Math.Max(MinimumSize.Width, text.Width + Padding.Horizontal),
            Math.Max(MinimumSize.Height, text.Height + Padding.Vertical));
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; pressed = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left) { pressed = true; Focus(); Invalidate(); }
    }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); pressed = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        var diameter = Math.Min(12f * DeviceDpi / 96, Math.Min(bounds.Width, bounds.Height));
        using var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        if (FillColor.A > 0)
        {
            using var fill = new SolidBrush(FillColor);
            e.Graphics.FillPath(fill, path);
        }
        if (hovered || pressed || (Focused && ShowFocusCues))
        {
            using var highlight = new SolidBrush(Color.FromArgb(pressed ? 45 : 24, Color.White));
            e.Graphics.FillPath(highlight, path);
        }
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.NoPrefix | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    protected override AccessibleObject CreateAccessibilityInstance() => new ButtonAccessibility(this);

    private sealed class ButtonAccessibility(ChromeButton button) : ControlAccessibleObject(button)
    {
        public override string DefaultAction => "Press";
        public override void DoDefaultAction() => button.OnClick(EventArgs.Empty);
    }
}
