using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace WindowedYouTubePlayer;

internal static class BrowserLauncher
{
    public static void LaunchAndControl(AppSettings settings)
    {
        int debuggingPort = ReserveLoopbackPort();
        string profileDirectory = Path.Combine(
            AppPaths.ProfilesDirectory,
            SafeDirectoryName(settings.BrowserKey));
        Directory.CreateDirectory(profileDirectory);

        ProcessStartInfo startInfo = new(settings.BrowserPath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(settings.BrowserPath) ?? Environment.CurrentDirectory
        };

        startInfo.ArgumentList.Add($"--user-data-dir={profileDirectory}");
        startInfo.ArgumentList.Add($"--remote-debugging-port={debuggingPort}");
        startInfo.ArgumentList.Add("--remote-debugging-address=127.0.0.1");
        startInfo.ArgumentList.Add("--remote-allow-origins=http://127.0.0.1");
        startInfo.ArgumentList.Add($"--app={settings.SiteUrl}");
        startInfo.ArgumentList.Add("--new-window");
        startInfo.ArgumentList.Add("--window-size=1280,720");
        startInfo.ArgumentList.Add("--no-first-run");
        startInfo.ArgumentList.Add("--no-default-browser-check");
        startInfo.ArgumentList.Add("--disable-session-crashed-bubble");
        startInfo.ArgumentList.Add("--disable-background-mode");

        Process? process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException($"{settings.BrowserName} did not return a running process.");
        }

        DevToolsController.RunAsync(process, debuggingPort).GetAwaiter().GetResult();
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

    public static async Task RunAsync(Process launchedProcess, int port)
    {
        using HttpClient httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        DateTime deadline = DateTime.UtcNow.AddSeconds(20);
        DevToolsTarget? target = null;
        while (DateTime.UtcNow < deadline)
        {
            target = await FindPageTargetAsync(httpClient, port);
            if (target?.WebSocketDebuggerUrl is not null)
            {
                break;
            }

            if (launchedProcess.HasExited && DateTime.UtcNow > deadline.AddSeconds(-15))
            {
                break;
            }

            await Task.Delay(250);
        }

        if (target?.WebSocketDebuggerUrl is null)
        {
            throw new InvalidOperationException(
                "The selected browser opened, but the app could not connect to its local control interface. Try another Chromium-based browser.");
        }

        await RunSessionAsync(new Uri(target.WebSocketDebuggerUrl), launchedProcess);
    }

    private static async Task<DevToolsTarget?> FindPageTargetAsync(HttpClient client, int port)
    {
        try
        {
            string json = await client.GetStringAsync($"http://127.0.0.1:{port}/json/list");
            List<DevToolsTarget>? targets = JsonSerializer.Deserialize<List<DevToolsTarget>>(json, JsonOptions);
            return targets?.FirstOrDefault(target =>
                string.Equals(target.Type, "page", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(target.WebSocketDebuggerUrl)
                && target.Url is not null
                && !target.Url.StartsWith("devtools://", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }

    private static async Task RunSessionAsync(Uri webSocketUri, Process launchedProcess)
    {
        using ClientWebSocket socket = new();
        socket.Options.SetRequestHeader("Origin", "http://127.0.0.1");
        await socket.ConnectAsync(webSocketUri, CancellationToken.None);

        int id = 0;
        await SendCommandAsync(socket, ++id, "Page.enable", null);
        await SendCommandAsync(socket, ++id, "Runtime.enable", null);
        await SendCommandAsync(
            socket,
            ++id,
            "Page.addScriptToEvaluateOnNewDocument",
            new { source = FullscreenInjection.Source });
        await SendCommandAsync(
            socket,
            ++id,
            "Runtime.evaluate",
            new
            {
                expression = FullscreenInjection.Source,
                awaitPromise = false,
                returnByValue = false
            });

        byte[] buffer = new byte[64 * 1024];
        while (socket.State == WebSocketState.Open)
        {
            if (launchedProcess.HasExited)
            {
                await Task.Delay(800);
            }

            WebSocketReceiveResult result;
            try
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            catch (WebSocketException)
            {
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }
        }
    }

    private static async Task SendCommandAsync(
        ClientWebSocket socket,
        int id,
        string method,
        object? parameters)
    {
        object payload = parameters is null
            ? new { id, method }
            : new { id, method, @params = parameters };
        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
