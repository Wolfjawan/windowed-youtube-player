using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace WindowedYouTubePlayer;

internal static class Program
{
    private const string ProductName = "Windowed Streaming Player";
    private const string ActivationEventName = @"Local\WindowedStreamingPlayer.Activate";

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        AppPaths.EnsureCreated();

        using EventWaitHandle activationEvent = new(
            false,
            EventResetMode.AutoReset,
            ActivationEventName,
            out bool createdNew);

        if (!createdNew)
        {
            activationEvent.Set();
            return;
        }

        try
        {
            bool chooseBrowser = args.Any(argument =>
                    string.Equals(argument, "--settings", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(argument, "--choose-browser", StringComparison.OrdinalIgnoreCase))
                || (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

            AppSettings? settings = SetupCoordinator.ResolveBrowser(chooseBrowser);
            if (settings is null)
            {
                return;
            }

            using BrowserSessionManager sessions = new();
            using MainForm mainForm = new(settings, sessions);
            using CancellationTokenSource activationCancellation = new();

            Task activationTask = Task.Run(
                () => ListenForActivation(activationEvent, mainForm, activationCancellation.Token),
                activationCancellation.Token);

            Application.Run(mainForm);

            activationCancellation.Cancel();
            activationEvent.Set();

            try
            {
                activationTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch (AggregateException)
            {
                // Application shutdown must not be blocked by the activation listener.
            }
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

    private static void ListenForActivation(
        EventWaitHandle activationEvent,
        Form mainForm,
        CancellationToken cancellationToken)
    {
        WaitHandle[] handles = [activationEvent, cancellationToken.WaitHandle];

        while (!cancellationToken.IsCancellationRequested)
        {
            int signalled = WaitHandle.WaitAny(handles);
            if (signalled != 0 || cancellationToken.IsCancellationRequested || mainForm.IsDisposed)
            {
                return;
            }

            try
            {
                mainForm.BeginInvoke(() =>
                {
                    if (mainForm.WindowState == FormWindowState.Minimized)
                    {
                        mainForm.WindowState = FormWindowState.Normal;
                    }

                    mainForm.Show();
                    mainForm.BringToFront();
                    mainForm.Activate();
                });
            }
            catch (InvalidOperationException)
            {
                return;
            }
        }
    }
}

internal static class AppPaths
{
    public const string ProductFolderName = "WindowedYouTubePlayer";

    public static string RootDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ProductFolderName);

    public static string SettingsFile => Path.Combine(RootDirectory, "launcher-settings-v5.json");

    public static string LegacySettingsFile => Path.Combine(RootDirectory, "launcher-settings-v4.json");

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
    public static AppSettings? ResolveBrowser(bool forceBrowserSelection)
    {
        AppPaths.EnsureCreated();
        AppSettings? saved = SettingsStore.Load();

        if (!forceBrowserSelection && SettingsStore.IsBrowserUsable(saved))
        {
            AppSettings upgraded = Upgrade(saved!);
            SettingsStore.Save(upgraded);
            return upgraded;
        }

        BrowserChoice? browser = BrowserPickerForm.SelectBrowser(saved?.BrowserPath);
        if (browser is null)
        {
            return null;
        }

        SiteChoice preferredSite = SettingsStore.IsSiteUsable(saved)
            ? new SiteChoice(saved!.SiteName, saved.SiteUrl)
            : SiteCatalog.Sites.First(site => !string.IsNullOrWhiteSpace(site.Url));

        AppSettings settings = new(
            SchemaVersion: 5,
            BrowserKey: browser.Key,
            BrowserName: browser.DisplayName,
            BrowserPath: Path.GetFullPath(browser.ExecutablePath),
            SiteName: preferredSite.DisplayName,
            SiteUrl: preferredSite.Url);

        SettingsStore.Save(settings);
        return settings;
    }

    private static AppSettings Upgrade(AppSettings settings) => settings with
    {
        SchemaVersion = 5,
        BrowserPath = Path.GetFullPath(settings.BrowserPath),
        SiteName = string.IsNullOrWhiteSpace(settings.SiteName)
            ? SiteUrl.FriendlyName(settings.SiteUrl)
            : settings.SiteName
    };
}

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static AppSettings? Load()
    {
        AppSettings? current = Read(AppPaths.SettingsFile);
        return current ?? Read(AppPaths.LegacySettingsFile);
    }

    public static void Save(AppSettings settings)
    {
        AppPaths.EnsureCreated();
        File.WriteAllText(
            AppPaths.SettingsFile,
            JsonSerializer.Serialize(settings, JsonOptions),
            new UTF8Encoding(false));
    }

    public static bool IsBrowserUsable(AppSettings? settings)
    {
        if (settings is null || settings.SchemaVersion < 4)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(settings.BrowserPath)
            && File.Exists(settings.BrowserPath)
            && BrowserLocator.IsLikelyChromiumBrowser(settings.BrowserPath);
    }

    public static bool IsSiteUsable(AppSettings? settings) =>
        settings is not null && SiteUrl.TryNormalize(settings.SiteUrl, out _);

    private static AppSettings? Read(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            return JsonSerializer.Deserialize<AppSettings>(
                File.ReadAllText(path),
                JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
