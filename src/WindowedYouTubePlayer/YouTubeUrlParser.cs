using System.Text.RegularExpressions;

namespace WindowedYouTubePlayer;

internal sealed record YouTubeTarget(string PlayerUrl, string? VideoId, string? PlaylistId);

internal static partial class YouTubeUrlParser
{
    public static bool TryCreatePlayerUrl(
        string input,
        bool autoPlay,
        out YouTubeTarget? target,
        out string error)
    {
        target = null;
        error = string.Empty;
        input = input.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Paste a YouTube video or playlist URL first.";
            return false;
        }

        string? videoId = null;
        string? playlistId = null;

        if (VideoIdRegex().IsMatch(input))
        {
            videoId = input;
        }
        else
        {
            if (!Uri.TryCreate(NormalizeUrl(input), UriKind.Absolute, out Uri? uri))
            {
                error = "That does not look like a valid YouTube URL.";
                return false;
            }

            string host = uri.Host.ToLowerInvariant();
            Dictionary<string, string> query = ParseQuery(uri.Query);
            query.TryGetValue("list", out playlistId);

            if (host is "youtu.be" or "www.youtu.be")
            {
                videoId = FirstPathSegment(uri.AbsolutePath);
            }
            else if (IsYouTubeHost(host))
            {
                string[] segments = uri.AbsolutePath
                    .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (segments.Length >= 2 && segments[0] is "shorts" or "embed" or "live")
                {
                    videoId = segments[1];
                }
                else if (query.TryGetValue("v", out string? queryVideoId))
                {
                    videoId = queryVideoId;
                }
                else if (segments.Length >= 2 && segments[0] == "playlist")
                {
                    playlistId ??= segments[1];
                }
            }
            else
            {
                error = "Only youtube.com and youtu.be links are supported.";
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(videoId) && !VideoIdRegex().IsMatch(videoId))
        {
            error = "The YouTube video ID is not valid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(videoId) && string.IsNullOrWhiteSpace(playlistId))
        {
            error = "I could not find a video or playlist ID in that link.";
            return false;
        }

        string baseUrl = videoId is not null
            ? $"https://www.youtube.com/embed/{Uri.EscapeDataString(videoId)}"
            : "https://www.youtube.com/embed/videoseries";

        List<string> parameters =
        [
            $"autoplay={(autoPlay ? 1 : 0)}",
            "controls=1",
            "fs=0",
            "playsinline=1",
            "rel=0",
            "iv_load_policy=3"
        ];

        if (!string.IsNullOrWhiteSpace(playlistId))
        {
            parameters.Add($"list={Uri.EscapeDataString(playlistId)}");
        }

        target = new YouTubeTarget($"{baseUrl}?{string.Join("&", parameters)}", videoId, playlistId);
        return true;
    }

    private static bool IsYouTubeHost(string host) => host is
        "youtube.com" or
        "www.youtube.com" or
        "m.youtube.com" or
        "music.youtube.com" or
        "youtube-nocookie.com" or
        "www.youtube-nocookie.com";

    private static string NormalizeUrl(string input) =>
        input.Contains("://", StringComparison.Ordinal) ? input : $"https://{input}";

    private static string? FirstPathSegment(string path) =>
        path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

    private static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

        foreach (string pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            string key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
            string value = parts.Length == 2
                ? Uri.UnescapeDataString(parts[1].Replace('+', ' '))
                : string.Empty;

            values[key] = value;
        }

        return values;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{11}$", RegexOptions.CultureInvariant)]
    private static partial Regex VideoIdRegex();
}
