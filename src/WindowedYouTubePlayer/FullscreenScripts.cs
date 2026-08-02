namespace WindowedYouTubePlayer;

internal static class FullscreenScripts
{
    // Install the capture-phase safety layer before the controller adds its
    // compatibility listeners. This guarantees that fullscreen controls are
    // consumed before website or fallback click handlers can request native fullscreen.
    public static string Source { get; } =
        FullscreenSafetyInjection.Source
        + Environment.NewLine
        + FullscreenInjection.Source;
}
