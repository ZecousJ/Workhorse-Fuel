namespace WorkhorseFuel;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var instance = new Mutex(true, @"Local\WorkhorseFuel", out var firstInstance);
        if (!firstInstance) return;
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.Dark);
        using var app = new TrayApp();
        Application.Run(app);
    }
}
