using System.Text.Json;

namespace WorkhorseFuel;

internal static class SettingsStore
{
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WorkhorseFuel");

    private static readonly string FilePath = Path.Combine(Folder, "settings.json");

    public static WorkSettings Load()
    {
        try
        {
            if (File.Exists(FilePath) &&
                JsonSerializer.Deserialize<WorkSettings>(File.ReadAllText(FilePath)) is { IsValid: true } settings)
                return settings;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException) { }

        return WorkSettings.Default;
    }

    public static void Save(WorkSettings settings)
    {
        Directory.CreateDirectory(Folder);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings));
        File.Move(temporary, FilePath, overwrite: true);
    }
}
