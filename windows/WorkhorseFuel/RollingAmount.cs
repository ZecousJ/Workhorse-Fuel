using System.Diagnostics;
using System.Globalization;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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
    private readonly Dictionary<(char Digit, int Radius), Bitmap> glyphs = [];

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
        ClearGlyphs();
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
                var radius = (int)Math.Round(3 * DeviceDpi / 96d * Math.Sin(Math.PI * progress));
                DrawMovingGlyph(e.Graphics, old, x, (int)Math.Round(direction * progress * Height), radius, 1 - progress);
                DrawMovingGlyph(e.Graphics, next, x, (int)Math.Round(direction * (progress - 1) * Height), radius, progress);
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

    private void DrawMovingGlyph(Graphics graphics, char digit, int x, int y, int radius, double opacity)
    {
        if (!char.IsDigit(digit)) return;
        if (!glyphs.TryGetValue((digit, radius), out var bitmap))
        {
            bitmap = CreateGlyph(digit, radius);
            glyphs.Add((digit, radius), bitmap);
        }
        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(new ColorMatrix { Matrix33 = (float)opacity });
        graphics.DrawImage(bitmap, new Rectangle(x - radius, y - radius, bitmap.Width, bitmap.Height),
            0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, attributes);
    }

    private Bitmap CreateGlyph(char digit, int radius)
    {
        var width = GlyphWidth(digit) + radius * 2;
        var height = Height + radius * 2;
        using var mask = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(mask))
        {
            graphics.Clear(Color.Black);
            TextRenderer.DrawText(graphics, digit.ToString(), Font,
                new Point(radius, radius), Color.White, Flags);
        }
        var area = new Rectangle(0, 0, width, height);
        var data = mask.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var pixels = new byte[data.Stride * height];
        Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
        mask.UnlockBits(data);
        var alpha = new float[width * height];
        for (var index = 0; index < alpha.Length; index++)
            alpha[index] = Math.Max(pixels[index * 4], Math.Max(pixels[index * 4 + 1], pixels[index * 4 + 2]));

        if (radius > 0)
        {
            var kernel = new float[radius * 2 + 1];
            var sigma = Math.Max(0.7, radius / 1.5);
            for (var offset = -radius; offset <= radius; offset++)
                kernel[offset + radius] = (float)Math.Exp(-offset * offset / (2 * sigma * sigma));
            var total = kernel.Sum();
            for (var index = 0; index < kernel.Length; index++) kernel[index] /= total;
            var horizontal = new float[alpha.Length];
            var blurred = new float[alpha.Length];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    for (var offset = -radius; offset <= radius; offset++)
                    {
                        var sample = x + offset;
                        if (sample >= 0 && sample < width)
                            horizontal[y * width + x] += alpha[y * width + sample] * kernel[offset + radius];
                    }
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    for (var offset = -radius; offset <= radius; offset++)
                    {
                        var sample = y + offset;
                        if (sample >= 0 && sample < height)
                            blurred[y * width + x] += horizontal[sample * width + x] * kernel[offset + radius];
                    }
            alpha = blurred;
        }

        for (var index = 0; index < alpha.Length; index++)
        {
            var opacity = (byte)Math.Clamp((int)Math.Round(alpha[index]), 0, 255);
            pixels[index * 4] = (byte)(ForeColor.B * opacity / 255);
            pixels[index * 4 + 1] = (byte)(ForeColor.G * opacity / 255);
            pixels[index * 4 + 2] = (byte)(ForeColor.R * opacity / 255);
            pixels[index * 4 + 3] = opacity;
        }
        var result = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        data = result.LockBits(area, ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
        Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        result.UnlockBits(data);
        return result;
    }

    private void ClearGlyphs()
    {
        foreach (var bitmap in glyphs.Values) bitmap.Dispose();
        glyphs.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            animation.Dispose();
            ClearGlyphs();
        }
        base.Dispose(disposing);
    }
}
