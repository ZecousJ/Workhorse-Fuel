using WorkhorseFuel;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var folder = args.Length > 0 ? args[0] : "layout";
        Directory.CreateDirectory(folder);
        var settings = WorkSettings.Default with { Salary = 25000, Schedule = Workweek.Alternating };
        foreach (var scale in new[] { 1f, 1.5f, 2f })
        {
            using var dashboard = new Dashboard(settings, () => {});
            dashboard.ShowNear(new Point(Screen.PrimaryScreen!.WorkingArea.Right, Screen.PrimaryScreen.WorkingArea.Bottom));
            dashboard.Hide();
            EnlargeFonts(dashboard, scale);
            dashboard.PerformLayout();
            CaptureAndCheck(dashboard, Path.Combine(folder, $"dashboard-{scale:0.0}.png"));

            using var dialog = new SettingsDialog(settings);
            dialog.Show();
            dialog.Hide();
            EnlargeFonts(dialog, scale);
            dialog.PerformLayout();
            CaptureAndCheck(dialog, Path.Combine(folder, $"settings-{scale:0.0}.png"));
        }
        File.WriteAllText(Path.Combine(folder, "result.txt"), "Layout checks passed at 100%, 150%, and 200% font sizes.");
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    private static void EnlargeFonts(Control root, float scale)
    {
        var controls = Descendants(root).Prepend(root).Select(control => (control, control.Font)).ToArray();
        foreach (var (control, font) in controls)
            control.Font = new Font(font.FontFamily, font.Size * scale, font.Style);
    }

    private static void CaptureAndCheck(Form form, string file)
    {
        form.PerformLayout();
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(file);
        foreach (var control in Descendants(form))
        {
            if (control.Right > control.Parent!.ClientSize.Width || control.Bottom > control.Parent.ClientSize.Height)
                throw new InvalidOperationException($"Control outside parent: {control.GetType().Name} {control.Text}");
        }
        foreach (var label in Descendants(form).OfType<Label>())
        {
            var preferred = label.GetPreferredSize(Size.Empty);
            if (label.Width < preferred.Width || label.Height < preferred.Height)
                throw new InvalidOperationException($"Clipped label '{label.Text}': {label.Size}, required {preferred}");
        }
    }
}
