using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

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
    private const string DebuggingPortFileName = "wsp-debugging-port.txt";

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

    public async Task<BrowserLaunchResult> OpenAsync(string siteUrl)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await launchLock.WaitAsync();

        try
        {
            debuggingPort = ResolveDebuggingPort();
            bool endpointAvailable = await DevToolsController.IsEndpointAvailableAsync(debuggingPort);

            Process? process = StartBrowser(siteUrl, debuggingPort);
            if (process is null)
            {
                throw new InvalidOperationException($"{browserName} did not open a new window.");
            }

            if (rootProcess is null || rootProcess.HasExited)
            {
                rootProcess?.Dispose();
                rootProcess = process;
            }

            if (endpointAvailable)
            {
                EnsureControllerMonitor(startWithRecovery: false);
                await Task.Delay(180);
                return new BrowserLaunchResult(true, true);
            }

            bool connected = await DevToolsController.TryWaitUntilAvailableAsync(
                debuggingPort,
                TimeSpan.FromSeconds(6),
                CancellationToken.None);

            EnsureControllerMonitor(startWithRecovery: !connected);
            return new BrowserLaunchResult(true, connected);
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

    private void EnsureControllerMonitor(bool startWithRecovery)
    {
        if (controllerTask is { IsCompleted: false })
        {
            return;
        }

        controllerCancellation?.Cancel();
        controllerCancellation?.Dispose();
        controllerCancellation = new CancellationTokenSource();
        CancellationToken token = controllerCancellation.Token;

        controllerTask = Task.Run(async () =>
        {
            if (startWithRecovery)
            {
                bool recovered = await DevToolsController.TryWaitUntilAvailableAsync(
                    debuggingPort,
                    TimeSpan.FromMinutes(2),
                    token);
                if (!recovered)
                {
                    return;
                }
            }

            await DevToolsController.MonitorAsync(debuggingPort, token);
        }, token);
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

    private int ResolveDebuggingPort()
    {
        string path = Path.Combine(profileDirectory, DebuggingPortFileName);
        try
        {
            if (File.Exists(path)
                && int.TryParse(File.ReadAllText(path), out int saved)
                && saved is >= 1024 and <= 65535)
            {
                return saved;
            }
        }
        catch
        {
            // Replace an unreadable port file below.
        }

        int port = ReserveLoopbackPort();
        try
        {
            File.WriteAllText(path, port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        catch
        {
            // A stable port is an optimisation; launching can continue without persistence.
        }
        return port;
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

    public static async Task<bool> IsEndpointAvailableAsync(int port)
    {
        using HttpClient client = CreateHttpClient();
        try
        {
            using HttpResponseMessage response = await client.GetAsync($"http://127.0.0.1:{port}/json/version");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> TryWaitUntilAvailableAsync(
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        DateTime deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            if (await IsEndpointAvailableAsync(port))
            {
                return true;
            }

            try
            {
                await Task.Delay(250, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        return false;
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
