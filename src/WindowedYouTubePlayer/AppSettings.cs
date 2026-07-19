using System.Text.Json;

namespace WindowedYouTubePlayer;

internal sealed class AppSettings
{
    public string LastUrl { get; set; } = string.Empty;
    public int PlayerWidth { get; set; } = 1280;
    public int PlayerHeight { get; set; } = 720;
    public bool AutoPlay { get; set; } = true;
    public bool StartBorderless { get; set; }
    public string BravePath { get; set; } = string.Empty;
}

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WindowedYouTubePlayer",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions)
                   ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            string? directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch
        {
            // Saving preferences must never prevent video playback.
        }
    }
}
