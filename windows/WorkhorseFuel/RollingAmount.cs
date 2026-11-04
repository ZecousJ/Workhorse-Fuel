using System.Diagnostics;
using System.Globalization;

namespace WorkhorseFuel;

internal sealed class RollingAmount : Control
{
    private const int DurationMilliseconds = 180;
    private const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine |
        TextFormatFlags.PreserveGraphicsClipping;
    private readonly System.Windows.Forms.Timer animation = new() { Interval = 15 };
    private string previous = "";
    private string displayed = "";
    private long started;
    private bool increasing = true;

    public RollingAmount()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        AutoSize = true;
        BackColor = Color.Transparent;
        AccessibleRole = AccessibleRole.StaticText;
        animation.Tick += (_, _) =>
        {
            if (Progress >= 1) animation.Stop();
            Invalidate();
        };
    }

    private double Progress => Math.Min(1, Stopwatch.GetElapsedTime(started).TotalMilliseconds / DurationMilliseconds);

    public override Size GetPreferredSize(Size proposedSize)
    {
        var width = Text.Sum(character => GlyphWidth(character));
        return new Size(Math.Max(1, width), TextRenderer.MeasureText("0", Font, Size.Empty, Flags).Height);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        previous = displayed;
        displayed = Text;
        decimal.TryParse(previous.Replace("¥", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var oldValue);
        decimal.TryParse(displayed.Replace("¥", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var newValue);
        increasing = newValue >= oldValue;
        started = Stopwatch.GetTimestamp();
        base.OnTextChanged(e);
        if (AutoSize) Size = GetPreferredSize(Size.Empty);
        if (Visible && previous.Length > 0 && previous != displayed && WindowsShell.AnimationsEnabled)
            animation.Start();
        else
        {
            previous = displayed;
            animation.Stop();
        }
        Invalidate();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible)
        {
            animation.Stop();
            previous = displayed;
        }
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (AutoSize) Size = GetPreferredSize(Size.Empty);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var progress = animation.Enabled ? 1 - Math.Pow(1 - Progress, 3) : 1;
        var x = 0;
        for (var index = 0; index < displayed.Length; index++)
        {
            var next = displayed[index];
            var width = GlyphWidth(next);
            var oldIndex = previous.Length - displayed.Length + index;
            var old = oldIndex >= 0 ? previous[oldIndex] : ' ';
            var cell = new Rectangle(x, 0, width, Height);
            var state = e.Graphics.Save();
            e.Graphics.SetClip(cell);
            if (char.IsDigit(next) && old != next && progress < 1)
            {
                var direction = increasing ? -1 : 1;
                DrawGlyph(e.Graphics, old, x, (int)Math.Round(direction * progress * Height));
                DrawGlyph(e.Graphics, next, x, (int)Math.Round(direction * (progress - 1) * Height));
            }
            else DrawGlyph(e.Graphics, next, x, 0);
            e.Graphics.Restore(state);
            x += width;
        }
    }

    private int GlyphWidth(char character) => TextRenderer.MeasureText(
        character == ' ' ? "\u00a0" : character.ToString(), Font, Size.Empty, Flags).Width;

    private void DrawGlyph(Graphics graphics, char character, int x, int y) =>
        TextRenderer.DrawText(graphics, character.ToString(), Font, new Point(x, y), ForeColor, Flags);

    protected override void Dispose(bool disposing)
    {
        if (disposing) animation.Dispose();
        base.Dispose(disposing);
    }
}
