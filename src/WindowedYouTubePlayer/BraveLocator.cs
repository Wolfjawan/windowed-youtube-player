using Microsoft.Win32;

namespace WindowedYouTubePlayer;

internal static class BraveLocator
{
    public static string? Find(string? preferredPath = null)
    {
        if (IsBraveExecutable(preferredPath))
        {
            return Path.GetFullPath(preferredPath!);
        }

        foreach (string candidate in GetStandardCandidates())
        {
            if (IsBraveExecutable(candidate))
            {
                return candidate;
            }
        }

        foreach (RegistryHive hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        {
            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                string? path = ReadAppPath(hive, view);
                if (IsBraveExecutable(path))
                {
                    return path;
                }
            }
        }

        return null;
    }

    public static bool IsBraveExecutable(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && File.Exists(path)
        && string.Equals(Path.GetFileName(path), "brave.exe", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> GetStandardCandidates()
    {
        string? programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string? programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string? localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        foreach (string? root in new[] { programFiles, programFilesX86, localAppData })
        {
            if (!string.IsNullOrWhiteSpace(root))
            {
                yield return Path.Combine(root, "BraveSoftware", "Brave-Browser", "Application", "brave.exe");
            }
        }
    }

    private static string? ReadAppPath(RegistryHive hive, RegistryView view)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
            using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\brave.exe");
            return key?.GetValue(null) as string;
        }
        catch
        {
            return null;
        }
    }
}
