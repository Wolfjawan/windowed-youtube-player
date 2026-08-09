using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace WindowedYouTubePlayer;

internal sealed record BrowserLaunchResult(bool BrowserWindowOpened, bool ControllerConnected);

internal sealed class BrowserSessionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, BrowserSession> sessions =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<BrowserLaunchResult> OpenAsync(AppSettings settings, SiteChoice site)
    {
        string key = Path.GetFullPath(settings.BrowserPath);
        BrowserSession session = sessions.GetOrAdd(
            key,
            _ => new BrowserSession(
                settings.BrowserKey,
                settings.BrowserName,
                settings.BrowserPath));

        return session.OpenAsync(site.Url);
    }

    public void Dispose()
    {
        foreach (BrowserSession session in sessions.Values)
        {
            session.Dispose();
        }

        sessions.Clear();
    }
}

internal sealed class BrowserSession : IDisposable
{
    private readonly string browserName;
    private readonly string browserPath;
    private readonly string profileDirectory;
    private readonly string extensionDirectory;
    private readonly SemaphoreSlim launchLock = new(1, 1);
    private readonly List<Process> launchedProcesses = [];
    private bool disposed;

    public BrowserSession(string browserKey, string selectedBrowserName, string selectedBrowserPath)
    {
        browserName = selectedBrowserName;
        browserPath = Path.GetFullPath(selectedBrowserPath);
        profileDirectory = Path.Combine(
            AppPaths.ProfilesDirectory,
            SafeDirectoryName(browserKey));
        extensionDirectory = Path.Combine(
            AppPaths.RootDirectory,
            "WindowFullscreenExtension");

        Directory.CreateDirectory(profileDirectory);
        RepairLegacyProfile();
        WindowFullscreenExtension.Install(extensionDirectory);
    }

    public async Task<BrowserLaunchResult> OpenAsync(string siteUrl)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await launchLock.WaitAsync();

        try
        {
            RepairLegacyProfile();
            WindowFullscreenExtension.Install(extensionDirectory);

            Process? process = StartBrowser(siteUrl);
            if (process is null)
            {
                throw new InvalidOperationException($"{browserName} did not open a new app window.");
            }

            launchedProcesses.RemoveAll(process => process.HasExited);
            launchedProcesses.Add(process);

            return new BrowserLaunchResult(true, true);
        }
        finally
        {
            launchLock.Release();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        foreach (Process process in launchedProcesses.ToArray())
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // The dedicated app window may already have been closed by the user.
            }
            finally
            {
                process.Dispose();
            }
        }

        launchedProcesses.Clear();
        launchLock.Dispose();
    }

    private Process? StartBrowser(string siteUrl)
    {
        ProcessStartInfo startInfo = new(browserPath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(browserPath) ?? Environment.CurrentDirectory
        };

        // Match the last known-good pre-installer runtime: direct app mode plus a
        // local unpacked extension. No remote-debugging flags and no about:blank hop.
        startInfo.ArgumentList.Add($"--user-data-dir={profileDirectory}");
        startInfo.ArgumentList.Add($"--load-extension={extensionDirectory}");
        startInfo.ArgumentList.Add($"--app={siteUrl}");
        startInfo.ArgumentList.Add("--new-window");
        startInfo.ArgumentList.Add("--window-size=1280,720");
        startInfo.ArgumentList.Add("--no-first-run");
        startInfo.ArgumentList.Add("--no-default-browser-check");
        startInfo.ArgumentList.Add("--disable-session-crashed-bubble");
        startInfo.ArgumentList.Add("--disable-background-mode");

        return Process.Start(startInfo);
    }

    private void RepairLegacyProfile()
    {
        string preferencesPath = Path.Combine(profileDirectory, "Default", "Preferences");
        if (!File.Exists(preferencesPath))
        {
            return;
        }

        try
        {
            JsonNode? parsed = JsonNode.Parse(File.ReadAllText(preferencesPath));
            if (parsed is not JsonObject root)
            {
                return;
            }

            SetBoolean(root, true, "fullscreen", "allowed");
            SetBoolean(root, true, "apps", "fullscreen", "allowed");
            File.WriteAllText(preferencesPath, root.ToJsonString());
        }
        catch
        {
            // Profile repair is only for v0.5.4/v0.5.5 compatibility. The direct
            // app-mode launch remains usable when Chromium owns or rewrites this file.
        }
    }

    private static void SetBoolean(JsonObject root, bool value, params string[] path)
    {
        JsonObject current = root;
        for (int index = 0; index < path.Length - 1; index++)
        {
            string segment = path[index];
            if (current[segment] is not JsonObject child)
            {
                child = new JsonObject();
                current[segment] = child;
            }
            current = child;
        }

        current[path[^1]] = value;
    }

    private static string SafeDirectoryName(string value)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string safe = new(value.Where(character => !invalid.Contains(character)).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "custom" : safe;
    }
}
