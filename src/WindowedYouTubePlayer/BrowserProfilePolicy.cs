using System.Text.Json;
using System.Text.Json.Nodes;

namespace WindowedYouTubePlayer;

internal static class BrowserProfilePolicy
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    // Keep Chromium's Fullscreen API available so streaming players continue to
    // expose and invoke their fullscreen controls. The injected controller replaces
    // requestFullscreen before the website loads and converts it to window-only mode.
    // This also repairs profiles created by v0.5.4, which persisted fullscreen=false.
    public static void EnforceWindowOnlyFullscreen(string userDataDirectory)
    {
        string defaultProfileDirectory = Path.Combine(userDataDirectory, "Default");
        string preferencesPath = Path.Combine(defaultProfileDirectory, "Preferences");
        string temporaryPath = preferencesPath + ".wsp.tmp";

        try
        {
            Directory.CreateDirectory(defaultProfileDirectory);

            JsonObject root = ReadPreferences(preferencesPath);
            SetBoolean(root, true, "fullscreen", "allowed");
            SetBoolean(root, true, "apps", "fullscreen", "allowed");

            File.WriteAllText(temporaryPath, root.ToJsonString(JsonOptions));
            File.Move(temporaryPath, preferencesPath, overwrite: true);

            JsonObject verified = ReadPreferences(preferencesPath);
            if (ReadBoolean(verified, "fullscreen", "allowed") is not true
                || ReadBoolean(verified, "apps", "fullscreen", "allowed") is not true)
            {
                throw new InvalidOperationException(
                    "The dedicated browser profile did not restore fullscreen controls.");
            }
        }
        catch (Exception exception) when (exception is IOException
                                          or UnauthorizedAccessException
                                          or JsonException
                                          or InvalidOperationException)
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch
            {
                // Preserve the original preference-writing error.
            }

            throw new InvalidOperationException(
                "The app could not prepare fullscreen controls in its dedicated browser profile. "
                + "Close all streaming windows and try again.",
                exception);
        }
    }

    private static JsonObject ReadPreferences(string path)
    {
        if (!File.Exists(path))
        {
            return new JsonObject();
        }

        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(json) as JsonObject ?? new JsonObject();
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

    private static bool? ReadBoolean(JsonObject root, params string[] path)
    {
        JsonNode? current = root;
        foreach (string segment in path)
        {
            current = current?[segment];
            if (current is null)
            {
                return null;
            }
        }

        return current is JsonValue value && value.TryGetValue(out bool result)
            ? result
            : null;
    }
}
