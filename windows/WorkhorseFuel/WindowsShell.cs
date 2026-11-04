using System.Runtime.InteropServices;
using System.Text;

namespace WorkhorseFuel;

internal static class WindowsShell
{
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
        public readonly Rectangle Bounds => Rectangle.FromLTRB(Left, Top, Right, Bottom);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins { public int Left, Right, Top, Bottom; }

    private delegate bool EnumWindow(nint window, nint parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindow callback, nint parameter);
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint window, out NativeRect rect);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint window, StringBuilder name, int capacity);
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);
    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(nint window, ref Margins margins);

    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(uint action, uint parameter,
        [MarshalAs(UnmanagedType.Bool)] ref bool value, uint flags);

    public static bool AnimationsEnabled
    {
        get
        {
            var enabled = true;
            SystemParametersInfo(0x1042, 0, ref enabled, 0);
            return enabled;
        }
    }

    public static bool ApplyBackdrop(nint window)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621)) return false;
        var dark = 1;
        var rounded = 2;
        var acrylic = 3;
        DwmSetWindowAttribute(window, 20, ref dark, sizeof(int));
        DwmSetWindowAttribute(window, 33, ref rounded, sizeof(int));
        if (DwmSetWindowAttribute(window, 38, ref acrylic, sizeof(int)) != 0) return false;
        var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
        return DwmExtendFrameIntoClientArea(window, ref margins) == 0;
    }

    public static (Rectangle WorkArea, Rectangle Anchor) PlacementArea(Point point)
    {
        var area = Screen.FromPoint(point).WorkingArea;
        var anchor = new Rectangle(point, new Size(1, 1));
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window) || !GetWindowRect(window, out var rect)) return true;
            var name = new StringBuilder(128);
            GetClassName(window, name, name.Capacity);
            var bounds = rect.Bounds;
            switch (name.ToString())
            {
                case "TopLevelWindowForOverflowXamlIsland":
                case "NotifyIconOverflowWindow":
                    if (bounds.Contains(point)) anchor = bounds;
                    break;
                case "Shell_TrayWnd":
                case "Shell_SecondaryTrayWnd":
                    if (!area.IntersectsWith(bounds)) break;
                    if (bounds.Width > bounds.Height)
                    {
                        if (bounds.Top > area.Top) area.Height = Math.Max(0, bounds.Top - area.Top);
                        else area = Rectangle.FromLTRB(area.Left, bounds.Bottom, area.Right, area.Bottom);
                    }
                    else if (bounds.Left > area.Left) area.Width = Math.Max(0, bounds.Left - area.Left);
                    else area = Rectangle.FromLTRB(bounds.Right, area.Top, area.Right, area.Bottom);
                    break;
            }
            return true;
        }, 0);
        return (area, anchor);
    }

    public static Point PlacePopup(Size size, Rectangle area, Rectangle anchor, int gap)
    {
        var x = Math.Clamp(anchor.Left + anchor.Width / 2 - size.Width / 2,
            area.Left + gap, Math.Max(area.Left + gap, area.Right - size.Width - gap));
        Point[] candidates =
        [
            new(x, anchor.Top - size.Height - gap),
            new(x, anchor.Bottom + gap),
            new(anchor.Left - size.Width - gap, Math.Max(area.Top + gap, area.Bottom - size.Height - gap)),
            new(anchor.Right + gap, Math.Max(area.Top + gap, area.Bottom - size.Height - gap))
        ];
        foreach (var point in candidates)
            if (area.Contains(new Rectangle(point, size))) return point;
        return new Point(x, Math.Clamp(candidates[0].Y, area.Top + gap,
            Math.Max(area.Top + gap, area.Bottom - size.Height - gap)));
    }
}
