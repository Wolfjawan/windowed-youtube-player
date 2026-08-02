namespace WindowedYouTubePlayer;

internal static class FullscreenScripts
{
    public static string Source { get; } =
        FullscreenInjection.Source
        + Environment.NewLine
        + FullscreenSafetyInjection.Source;
}
