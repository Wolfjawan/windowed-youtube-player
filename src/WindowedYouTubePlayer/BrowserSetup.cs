using System.Drawing;
using Microsoft.Win32;
using System.Windows.Forms;

namespace WindowedYouTubePlayer;

internal sealed record BrowserDefinition(
    string Key,
    string DisplayName,
    string[] ExecutableNames,
    Func<IEnumerable<string>> StandardPaths);

internal static class BrowserLocator
{
    private static readonly BrowserDefinition[] Definitions =
    [
        new(
            "brave",
            "Brave",
            ["brave.exe"],
            () => BrowserPaths(
                Path.Combine("BraveSoftware", "Brave-Browser", "Application", "brave.exe"))),
        new(
            "chrome",
            "Google Chrome",
            ["chrome.exe"],
            () => BrowserPaths(
                Path.Combine("Google", "Chrome", "Application", "chrome.exe"))),
        new(
            "edge",
            "Microsoft Edge",
            ["msedge.exe"],
            () => BrowserPaths(
                Path.Combine("Microsoft", "Edge", "Application", "msedge.exe"))),
        new(
            "vivaldi",
            "Vivaldi",
            ["vivaldi.exe"],
            () => BrowserPaths(
                Path.Combine("Vivaldi", "Application", "vivaldi.exe"))),
        new(
            "opera",
            "Opera",
            ["opera.exe", "launcher.exe"],
            () =>
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Opera", "launcher.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Opera GX", "launcher.exe")
            ]),
        new(
            "chromium",
            "Chromium",
            ["chromium.exe", "chrome.exe"],
            () => BrowserPaths(
                Path.Combine("Chromium", "Application", "chrome.exe")))
    ];

    public static IReadOnlyList<BrowserChoice> FindInstalled()
    {
        List<BrowserChoice> choices = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        BrowserChoice? defaultBrowser = FindSupportedWindowsDefault();
        if (defaultBrowser is not null && seen.Add(defaultBrowser.ExecutablePath))
        {
            choices.Add(defaultBrowser with { DisplayName = $"{defaultBrowser.DisplayName} (Windows default)" });
        }

        foreach (BrowserDefinition definition in Definitions)
        {
            foreach (string path in definition.StandardPaths().Concat(RegisteredPaths(definition)))
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                string fullPath = Path.GetFullPath(path);
                if (!seen.Add(fullPath))
                {
                    continue;
                }

                choices.Add(new BrowserChoice(definition.Key, definition.DisplayName, fullPath));
            }
        }

        return choices;
    }

    public static BrowserChoice Identify(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string fileName = Path.GetFileName(fullPath);
        BrowserDefinition? definition = Definitions.FirstOrDefault(item =>
            item.ExecutableNames.Contains(fileName, StringComparer.OrdinalIgnoreCase)
            && PathLooksLikeDefinition(fullPath, item));

        if (definition is not null)
        {
            return new BrowserChoice(definition.Key, definition.DisplayName, fullPath);
        }

        return new BrowserChoice(
            Key: SanitizeKey(Path.GetFileNameWithoutExtension(fileName)),
            DisplayName: $"Custom Chromium browser ({Path.GetFileNameWithoutExtension(fileName)})",
            ExecutablePath: fullPath);
    }

    public static bool IsLikelyChromiumBrowser(string path)
    {
        if (!File.Exists(path) || !string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string name = Path.GetFileName(path);
        if (name.Equals("firefox.exe", StringComparison.OrdinalIgnoreCase)
            || name.Equals("iexplore.exe", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static BrowserChoice? FindSupportedWindowsDefault()
    {
        try
        {
            using RegistryKey? userChoice = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice");
            string? progId = userChoice?.GetValue("ProgId") as string;
            if (string.IsNullOrWhiteSpace(progId))
            {
                return null;
            }

            string? command = ReadOpenCommand(Registry.CurrentUser, progId)
                ?? ReadOpenCommand(Registry.LocalMachine, progId)
                ?? ReadClassesRootOpenCommand(progId);
            string? executable = ExtractExecutablePath(command);

            return executable is not null
                && File.Exists(executable)
                && IsLikelyChromiumBrowser(executable)
                    ? Identify(executable)
                    : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadOpenCommand(RegistryKey hive, string progId)
    {
        using RegistryKey? key = hive.OpenSubKey($@"Software\Classes\{progId}\shell\open\command");
        return key?.GetValue(null) as string;
    }

    private static string? ReadClassesRootOpenCommand(string progId)
    {
        using RegistryKey? key = Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command");
        return key?.GetValue(null) as string;
    }

    private static string? ExtractExecutablePath(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        string trimmed = command.Trim();
        if (trimmed.StartsWith('"'))
        {
            int endQuote = trimmed.IndexOf('"', 1);
            return endQuote > 1 ? trimmed[1..endQuote] : null;
        }

        int exeEnd = trimmed.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        return exeEnd >= 0 ? trimmed[..(exeEnd + 4)] : null;
    }

    private static IEnumerable<string> BrowserPaths(string relativePath)
    {
        string[] roots =
        [
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        ];

        return roots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(root => Path.Combine(root, relativePath));
    }

    private static IEnumerable<string> RegisteredPaths(BrowserDefinition definition)
    {
        foreach (string executableName in definition.ExecutableNames)
        {
            foreach (RegistryHive hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            {
                foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                {
                    string? path = ReadRegisteredPath(hive, view, executableName);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        yield return path;
                    }
                }
            }
        }
    }

    private static string? ReadRegisteredPath(RegistryHive hive, RegistryView view, string executableName)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
            using RegistryKey? key = baseKey.OpenSubKey(
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{executableName}");
            return key?.GetValue(null) as string;
        }
        catch
        {
            return null;
        }
    }

    private static bool PathLooksLikeDefinition(string path, BrowserDefinition definition)
    {
        string normalized = path.Replace('/', '\\');
        return definition.Key switch
        {
            "chrome" => normalized.Contains(@"\Google\Chrome\", StringComparison.OrdinalIgnoreCase),
            "chromium" => normalized.Contains(@"\Chromium\", StringComparison.OrdinalIgnoreCase),
            "opera" => normalized.Contains(@"\Opera", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private static string SanitizeKey(string value)
    {
        string key = new(value
            .ToLowerInvariant()
            .Where(character => char.IsLetterOrDigit(character))
            .ToArray());
        return string.IsNullOrWhiteSpace(key) ? "custom" : key;
    }
}

internal sealed class BrowserPickerForm : Form
{
    private readonly ListBox browserList = new();
    private BrowserChoice? selectedBrowser;

    private BrowserPickerForm(string? preferredPath)
    {
        Text = "Choose a browser";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 390);
        AutoScaleMode = AutoScaleMode.Dpi;
        Icon = AppIcon.TryLoad();

        Label heading = new()
        {
            AutoSize = true,
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            Location = new Point(24, 22),
            Text = "Choose the browser to use"
        };

        Label description = new()
        {
            AutoSize = false,
            Location = new Point(25, 58),
            Size = new Size(510, 52),
            Text = "The app uses a separate browser profile, so streaming logins and preferences remain saved. Chromium-based browsers are supported."
        };

        browserList.Location = new Point(25, 116);
        browserList.Size = new Size(510, 180);
        browserList.DisplayMember = nameof(BrowserChoice.DisplayName);
        browserList.DoubleClick += (_, _) => AcceptSelected();

        IReadOnlyList<BrowserChoice> installed = BrowserLocator.FindInstalled();
        foreach (BrowserChoice choice in installed)
        {
            browserList.Items.Add(choice);
        }

        if (!string.IsNullOrWhiteSpace(preferredPath))
        {
            int preferredIndex = installed
                .Select((choice, index) => new { choice, index })
                .FirstOrDefault(item => string.Equals(
                    item.choice.ExecutablePath,
                    preferredPath,
                    StringComparison.OrdinalIgnoreCase))?.index ?? -1;
            browserList.SelectedIndex = preferredIndex >= 0 ? preferredIndex : installed.Count > 0 ? 0 : -1;
        }
        else
        {
            browserList.SelectedIndex = installed.Count > 0 ? 0 : -1;
        }

        Button browseButton = new()
        {
            Text = "Browse…",
            Location = new Point(25, 316),
            Size = new Size(110, 36)
        };
        browseButton.Click += (_, _) => BrowseForBrowser();

        Button cancelButton = new()
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(315, 316),
            Size = new Size(105, 36)
        };

        Button useButton = new()
        {
            Text = "Use browser",
            Location = new Point(430, 316),
            Size = new Size(105, 36)
        };
        useButton.Click += (_, _) => AcceptSelected();

        Controls.AddRange([heading, description, browserList, browseButton, cancelButton, useButton]);
        AcceptButton = useButton;
        CancelButton = cancelButton;
    }

    public static BrowserChoice? SelectBrowser(string? preferredPath)
    {
        using BrowserPickerForm form = new(preferredPath);
        return form.ShowDialog() == DialogResult.OK ? form.selectedBrowser : null;
    }

    private void AcceptSelected()
    {
        if (browserList.SelectedItem is not BrowserChoice choice)
        {
            MessageBox.Show(
                "Select a browser or use Browse to locate one.",
                "Windowed Streaming Player",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        selectedBrowser = choice;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void BrowseForBrowser()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Select a Chromium-based browser",
            Filter = "Browser applications (*.exe)|*.exe",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!BrowserLocator.IsLikelyChromiumBrowser(dialog.FileName))
        {
            MessageBox.Show(
                "That application is not a supported Chromium-based browser.",
                "Windowed Streaming Player",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        BrowserChoice choice = BrowserLocator.Identify(dialog.FileName);
        browserList.Items.Add(choice);
        browserList.SelectedItem = choice;
    }
}
