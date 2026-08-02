using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows.Forms;

namespace WindowedYouTubePlayer;

internal static class Program
{
    private const string ProductName = "Windowed Streaming Player";

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        using Mutex singleInstance = new(true, @"Local\WindowedStreamingPlayer", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "Windowed Streaming Player is already running.",
                ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            bool openSettings = args.Any(argument =>
                    string.Equals(argument, "--settings", StringComparison.OrdinalIgnoreCase))
                || (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

            AppSettings? settings = SetupCoordinator.Resolve(openSettings);
            if (settings is null)
            {
                return;
            }

            BrowserLauncher.LaunchAndControl(settings);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Windowed Streaming Player could not start.\n\n{exception.Message}",
                ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}

internal static class AppPaths
{
    public const string ProductFolderName = "WindowedYouTubePlayer";

    public static string RootDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ProductFolderName);

    public static string SettingsFile => Path.Combine(RootDirectory, "launcher-settings-v4.json");

    public static string ProfilesDirectory => Path.Combine(RootDirectory, "BrowserProfiles");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(ProfilesDirectory);
    }
}

internal sealed record AppSettings(
    int SchemaVersion,
    string BrowserKey,
    string BrowserName,
    string BrowserPath,
    string SiteName,
    string SiteUrl);

internal sealed record BrowserChoice(string Key, string DisplayName, string ExecutablePath)
{
    public override string ToString() => DisplayName;
}

internal sealed record SiteChoice(string DisplayName, string Url)
{
    public override string ToString() => DisplayName;
}

internal static class SetupCoordinator
{
    public static AppSettings? Resolve(bool forceSettings)
    {
        AppPaths.EnsureCreated();
        AppSettings? saved = SettingsStore.Load();

        if (!forceSettings && SettingsStore.IsUsable(saved))
        {
            return saved;
        }

        BrowserChoice? browser = BrowserPickerForm.SelectBrowser(saved?.BrowserPath);
        if (browser is null)
        {
            return null;
        }

        SiteChoice? site = SitePickerForm.SelectSite(saved?.SiteUrl);
        if (site is null)
        {
            return null;
        }

        AppSettings settings = new(
            SchemaVersion: 4,
            BrowserKey: browser.Key,
            BrowserName: browser.DisplayName,
            BrowserPath: Path.GetFullPath(browser.ExecutablePath),
            SiteName: site.DisplayName,
            SiteUrl: site.Url);

        SettingsStore.Save(settings);
        return settings;
    }
}

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static AppSettings? Load()
    {
        try
        {
            if (!File.Exists(AppPaths.SettingsFile))
            {
                return null;
            }

            return JsonSerializer.Deserialize<AppSettings>(
                File.ReadAllText(AppPaths.SettingsFile),
                JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static void Save(AppSettings settings)
    {
        AppPaths.EnsureCreated();
        File.WriteAllText(
            AppPaths.SettingsFile,
            JsonSerializer.Serialize(settings, JsonOptions),
            new UTF8Encoding(false));
    }

    public static bool IsUsable(AppSettings? settings)
    {
        if (settings is null || settings.SchemaVersion < 4)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.BrowserPath)
            || !File.Exists(settings.BrowserPath)
            || !BrowserLocator.IsLikelyChromiumBrowser(settings.BrowserPath))
        {
            return false;
        }

        return SiteUrl.TryNormalize(settings.SiteUrl, out _);
    }
}
