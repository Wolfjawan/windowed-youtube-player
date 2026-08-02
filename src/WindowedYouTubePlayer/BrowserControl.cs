using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace WindowedYouTubePlayer;

internal sealed class BrowserSessionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, BrowserSession> sessions =
        new(StringComparer.OrdinalIgnoreCase);

    public Task OpenAsync(AppSettings settings, SiteChoice site)
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
    private readonly SemaphoreSlim launchLock = new(1, 1);

    private CancellationTokenSource? controllerCancellation;
    private Task? controllerTask;
    private Process? rootProcess;
    private int debuggingPort;
    private bool disposed;

    public BrowserSession(string browserKey, string selectedBrowserName, string selectedBrowserPath)
    {
        browserName = selectedBrowserName;
        browserPath = Path.GetFullPath(selectedBrowserPath);
        profileDirectory = Path.Combine(
            AppPaths.ProfilesDirectory,
            SafeDirectoryName(browserKey));
        Directory.CreateDirectory(profileDirectory);
    }

    public async Task OpenAsync(string siteUrl)
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(BrowserSession));
        }

        await launchLock.WaitAsync();

        try
        {
            bool endpointAvailable = debuggingPort > 0
                && await DevToolsController.IsAvailableAsync(debuggingPort);

            if (!endpointAvailable)
            {
                await StartSessionAsync(siteUrl);
                return;
            }

            Process? process = StartBrowser(siteUrl, debuggingPort);
            if (process is null)
            {
                throw new InvalidOperationException($"{browserName} did not open a new window.");
            }

            await Task.Delay(250);
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
        controllerCancellation?.Cancel();

        try
        {
            if (rootProcess is not null && !rootProcess.HasExited)
            {
                rootProcess.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // The dedicated browser process may already be closing.
        }

        controllerCancellation?.Dispose();
        rootProcess?.Dispose();
        launchLock.Dispose();
    }

    private async Task StartSessionAsync(string siteUrl)
    {
        controllerCancellation?.Cancel();
        controllerCancellation?.Dispose();

        debuggingPort = ReserveLoopbackPort();
        rootProcess = StartBrowser(siteUrl, debuggingPort)
            ?? throw new InvalidOperationException($"{browserName} did not return a running process.");

        await DevToolsController.WaitUntilAvailableAsync(rootProcess, debuggingPort);

        controllerCancellation = new CancellationTokenSource();
        controllerTask = Task.Run(
            () => DevToolsController.MonitorAsync(debuggingPort, controllerCancellation.Token),
            controllerCancellation.Token);
    }

    private Process? StartBrowser(string siteUrl, int port)
    {
        ProcessStartInfo startInfo = new(browserPath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(browserPath) ?? Environment.CurrentDirectory
        };

        startInfo.ArgumentList.Add($"--user-data-dir={profileDirectory}");
        startInfo.ArgumentList.Add($"--remote-debugging-port={port}");
        startInfo.ArgumentList.Add("--remote-debugging-address=127.0.0.1");
        startInfo.ArgumentList.Add("--remote-allow-origins=http://127.0.0.1");
        startInfo.ArgumentList.Add($"--app={siteUrl}");
        startInfo.ArgumentList.Add("--new-window");
        startInfo.ArgumentList.Add("--window-size=1280,720");
        startInfo.ArgumentList.Add("--no-first-run");
        startInfo.ArgumentList.Add("--no-default-browser-check");
        startInfo.ArgumentList.Add("--disable-session-crashed-bubble");
        startInfo.ArgumentList.Add("--disable-background-mode");

        return Process.Start(startInfo);
    }

    private static int ReserveLoopbackPort()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string SafeDirectoryName(string value)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string safe = new(value.Where(character => !invalid.Contains(character)).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "custom" : safe;
    }
}

internal sealed class DevToolsTarget
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Url { get; set; }
    public string? WebSocketDebuggerUrl { get; set; }
}

internal static class DevToolsController
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<bool> IsAvailableAsync(int port)
    {
        using HttpClient client = CreateHttpClient();
        return (await GetPageTargetsAsync(client, port)).Count > 0;
    }

    public static async Task WaitUntilAvailableAsync(Process launchedProcess, int port)
    {
        using HttpClient client = CreateHttpClient();
        DateTime deadline = DateTime.UtcNow.AddSeconds(20);

        while (DateTime.UtcNow < deadline)
        {
            if ((await GetPageTargetsAsync(client, port)).Count > 0)
            {
                return;
            }

            if (launchedProcess.HasExited && DateTime.UtcNow > deadline.AddSeconds(-15))
            {
                break;
            }

            await Task.Delay(250);
        }

        throw new InvalidOperationException(
            "The selected browser opened, but the app could not connect to its local control interface. "
            + "Try another Chromium-based browser.");
    }

    public static async Task MonitorAsync(int port, CancellationToken cancellationToken)
    {
        using HttpClient client = CreateHttpClient();
        Dictionary<string, Task> activeTargets = new(StringComparer.Ordinal);

        while (!cancellationToken.IsCancellationRequested)
        {
            IReadOnlyList<DevToolsTarget> targets = await GetPageTargetsAsync(client, port);

            foreach (DevToolsTarget target in targets)
            {
                if (string.IsNullOrWhiteSpace(target.Id)
                    || string.IsNullOrWhiteSpace(target.WebSocketDebuggerUrl)
                    || activeTargets.ContainsKey(target.Id))
                {
                    continue;
                }

                string targetId = target.Id;
                Task sessionTask = RunTargetSessionAsync(
                    new Uri(target.WebSocketDebuggerUrl),
                    cancellationToken);
                activeTargets[targetId] = sessionTask;
            }

            foreach (string completedId in activeTargets
                         .Where(pair => pair.Value.IsCompleted)
                         .Select(pair => pair.Key)
                         .ToArray())
            {
                activeTargets.Remove(completedId);
            }

            try
            {
                await Task.Delay(500, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static HttpClient CreateHttpClient() => new()
    {
        Timeout = TimeSpan.FromSeconds(2)
    };

    private static async Task<IReadOnlyList<DevToolsTarget>> GetPageTargetsAsync(
        HttpClient client,
        int port)
    {
        try
        {
            string json = await client.GetStringAsync($"http://127.0.0.1:{port}/json/list");
            List<DevToolsTarget>? targets = JsonSerializer.Deserialize<List<DevToolsTarget>>(
                json,
                JsonOptions);

            return targets?
                .Where(target =>
                    string.Equals(target.Type, "page", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(target.Id)
                    && !string.IsNullOrWhiteSpace(target.WebSocketDebuggerUrl)
                    && target.Url is not null
                    && (target.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                        || target.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                .ToArray()
                ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static async Task RunTargetSessionAsync(
        Uri webSocketUri,
        CancellationToken cancellationToken)
    {
        using ClientWebSocket socket = new();
        socket.Options.SetRequestHeader("Origin", "http://127.0.0.1");

        try
        {
            await socket.ConnectAsync(webSocketUri, cancellationToken);

            int id = 0;
            await SendCommandAsync(socket, ++id, "Page.enable", null, cancellationToken);
            await SendCommandAsync(socket, ++id, "Runtime.enable", null, cancellationToken);
            await SendCommandAsync(
                socket,
                ++id,
                "Page.addScriptToEvaluateOnNewDocument",
                new { source = FullscreenInjection.Source },
                cancellationToken);
            await SendCommandAsync(
                socket,
                ++id,
                "Runtime.evaluate",
                new
                {
                    expression = FullscreenInjection.Source,
                    awaitPromise = false,
                    returnByValue = false
                },
                cancellationToken);

            byte[] buffer = new byte[64 * 1024];
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal application shutdown.
        }
        catch (WebSocketException)
        {
            // The browser target was closed or navigated away.
        }
    }

    private static async Task SendCommandAsync(
        ClientWebSocket socket,
        int id,
        string method,
        object? parameters,
        CancellationToken cancellationToken)
    {
        object payload = parameters is null
            ? new { id, method }
            : new { id, method, @params = parameters };

        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        await socket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            cancellationToken);
    }
}
