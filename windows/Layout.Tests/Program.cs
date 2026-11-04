using WorkhorseFuel;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetColorMode(SystemColorMode.Dark);
        Application.SetCompatibleTextRenderingDefault(false);
        var area = new Rectangle(0, 0, 1920, 1040);
        var overflow = new Rectangle(1700, 800, 200, 220);
        var popup = new Rectangle(WindowsShell.PlacePopup(new Size(366, 350), area, overflow, 10), new Size(366, 350));
        if (!area.Contains(popup) || overflow.IntersectsWith(popup))
            throw new InvalidOperationException("Popup overlaps the taskbar overflow.");
        var taskbarPoint = new Rectangle(1890, 1060, 1, 1);
        popup.Location = WindowsShell.PlacePopup(popup.Size, area, taskbarPoint, 10);
        if (!area.Contains(popup))
            throw new InvalidOperationException("Popup extends into the taskbar.");

        var folder = args.Length > 0 ? args[0] : "layout";
        Directory.CreateDirectory(folder);
        var settings = WorkSettings.Default with { Salary = 25000, Schedule = Workweek.Alternating };
        foreach (var scale in new[] { 1f, 1.5f, 2f })
        {
            using var dashboard = new Dashboard(settings);
            dashboard.ShowNear(new Point(Screen.PrimaryScreen!.WorkingArea.Right, Screen.PrimaryScreen.WorkingArea.Bottom));
            dashboard.Hide();
            EnlargeFonts(dashboard, scale);
            dashboard.PerformLayout();
            CaptureAndCheck(dashboard, Path.Combine(folder, $"dashboard-{scale:0.0}.png"));

            dashboard.OpenSettings();
            EnlargeFonts(dashboard.Controls.OfType<SettingsPanel>().Single(), scale);
            if (Application.OpenForms.Count != 1)
                throw new InvalidOperationException("Settings opened another window.");
            dashboard.PerformLayout();
            CaptureAndCheck(dashboard, Path.Combine(folder, $"settings-{scale:0.0}.png"));
        }
        VerifyAnimation(folder);
        File.WriteAllText(Path.Combine(folder, "result.txt"), "Layout checks passed at 100%, 150%, and 200% font sizes.");
    }

    private static void VerifyAnimation(string folder)
    {
        using var window = new Form { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        using var amount = new RollingAmount
        {
            Text = "¥ 1,234.56", Font = new Font("Segoe UI", 38), ForeColor = Color.White, BackColor = Color.Black
        };
        window.Controls.Add(amount);
        window.Show();
        Application.DoEvents();
        amount.Text = "¥ 1,234.57";
        Thread.Sleep(60);
        Application.DoEvents();
        using var middle = new Bitmap(amount.Width, amount.Height);
        amount.DrawToBitmap(middle, amount.ClientRectangle);
        middle.Save(Path.Combine(folder, "animation-middle.png"));
        Thread.Sleep(200);
        Application.DoEvents();
        using var finished = new Bitmap(amount.Width, amount.Height);
        amount.DrawToBitmap(finished, amount.ClientRectangle);
        finished.Save(Path.Combine(folder, "animation-finished.png"));
        if (WindowsShell.AnimationsEnabled)
        {
            var changed = false;
            for (var y = 0; y < middle.Height && !changed; y++)
                for (var x = 0; x < middle.Width && !changed; x++)
                    changed = middle.GetPixel(x, y) != finished.GetPixel(x, y);
            if (!changed) throw new InvalidOperationException("The amount did not animate.");
        }
        window.Hide();
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (parent is Dashboard && child is not SettingsPanel && parent.Controls.OfType<SettingsPanel>().Any()) continue;
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
        foreach (var label in Descendants(form).Where(control => control is Label or RollingAmount))
        {
            var preferred = label.GetPreferredSize(Size.Empty);
            if (label.Width < preferred.Width || label.Height < preferred.Height)
                throw new InvalidOperationException($"Clipped label '{label.Text}': {label.Size}, required {preferred}");
        }
    }
}
