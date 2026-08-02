namespace WindowedYouTubePlayer;

internal static class FullscreenScripts
{
    // Create the window-fullscreen controller first, then install the safety hooks
    // that route player APIs and controls through that ready controller.
    public static string Source { get; } =
        FullscreenInjection.Source
        + Environment.NewLine
        + FullscreenSafetyInjection.Source;
}
