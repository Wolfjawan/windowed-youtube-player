using System.Windows.Forms;
using WindowedYouTubePlayer;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();

            AppSettings settings = new(
                SchemaVersion: 5,
                BrowserKey: "startup-smoke",
                BrowserName: "Startup smoke browser",
                BrowserPath: Environment.ProcessPath ?? "smoke.exe",
                SiteName: "YouTube",
                SiteUrl: "https://www.youtube.com/");

            using BrowserSessionManager sessions = new();
            using MainForm form = new(settings, sessions);

            form.CreateControl();
            _ = form.Handle;
            form.PerformLayout();

            foreach (Control control in form.Controls)
            {
                control.CreateControl();
                control.PerformLayout();
            }

            Console.WriteLine("MainForm startup smoke test passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
