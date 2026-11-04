namespace WorkhorseFuel;

internal sealed class TrayApp : ApplicationContext
{
    private readonly Icon icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!)!;
    private readonly Dashboard dashboard;
    private readonly ContextMenuStrip menu = new();
    private readonly NotifyIcon tray;
    private WorkSettings settings = SettingsStore.Load();

    public TrayApp()
    {
        dashboard = new Dashboard(settings);
        dashboard.SettingsChanged += updated => settings = updated;
        menu.Items.Add("打开计数器", null, (_, _) => dashboard.ShowNear(Cursor.Position));
        menu.Items.Add("设置", null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitThread());

        tray = new NotifyIcon
        {
            Icon = icon,
            Text = "牛马兴奋剂 · Workhorse Fuel",
            ContextMenuStrip = menu,
            Visible = true
        };
        Application.Idle += ShowOnLaunch;
        tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) dashboard.ShowNear(Cursor.Position);
        };
    }

    private void ShowOnLaunch(object? sender, EventArgs e)
    {
        Application.Idle -= ShowOnLaunch;
        var area = Screen.PrimaryScreen!.WorkingArea;
        dashboard.ShowNear(new Point(area.Right, area.Bottom));
    }

    private void ShowSettings()
    {
        if (!dashboard.Visible) dashboard.ShowNear(Cursor.Position);
        dashboard.OpenSettings();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Application.Idle -= ShowOnLaunch;
            tray.Dispose();
            menu.Dispose();
            dashboard.Dispose();
            icon.Dispose();
        }
        base.Dispose(disposing);
    }
}
