using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Windows.Forms;

namespace WindowedYouTubePlayer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private readonly TextBox _urlTextBox = new();
    private readonly TextBox _bravePathTextBox = new();
    private readonly NumericUpDown _widthInput = new();
    private readonly NumericUpDown _heightInput = new();
    private readonly CheckBox _autoplayCheckBox = new();
    private readonly Label _statusLabel = new();

    public MainForm()
    {
        Text = "Windowed YouTube Player";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 390);
        Size = new Size(790, 440);
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildLayout();

        _widthInput.Value = 1280;
        _heightInput.Value = 720;
        _autoplayCheckBox.Checked = true;
        _bravePathTextBox.Text = FindBrave() ?? "Brave was not found. Click Browse…";
    }

    private void BuildLayout()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            ColumnCount = 1,
            RowCount = 7
        };

        for (int index = 0; index < 6; index++)
        {
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        Label title = new()
        {
            AutoSize = true,
            Text = "Windowed YouTube Player",
            Font = new Font(Font.FontFamily, 18F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 6)
        };
        root.Controls.Add(title);

        Label explanation = new()
        {
            AutoSize = true,
            MaximumSize = new Size(720, 0),
            Text = "Paste a YouTube link and open it in a clean, resizable Brave app window. The video stays inside that window instead of taking over your whole monitor.",
            Margin = new Padding(0, 0, 0, 16)
        };
        root.Controls.Add(explanation);

        TableLayoutPanel urlRow = CreateInputRow();
        _urlTextBox.Dock = DockStyle.Fill;
        _urlTextBox.PlaceholderText = "https://www.youtube.com/watch?v=…";
        _urlTextBox.Margin = new Padding(0, 0, 8, 0);
        _urlTextBox.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode == Keys.Enter)
            {
                eventArgs.SuppressKeyPress = true;
                LaunchPlayer();
            }
        };

        Button pasteButton = new() { Text = "Paste", AutoSize = true };
        pasteButton.Click += (_, _) =>
        {
            if (Clipboard.ContainsText())
            {
                _urlTextBox.Text = Clipboard.GetText().Trim();
            }
        };

        urlRow.Controls.Add(_urlTextBox, 0, 0);
        urlRow.Controls.Add(pasteButton, 1, 0);
        root.Controls.Add(urlRow);

        FlowLayoutPanel sizeRow = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 14, 0, 0)
        };

        ConfigureDimensionInput(_widthInput, 480, 7680);
        ConfigureDimensionInput(_heightInput, 270, 4320);

        sizeRow.Controls.Add(new Label { Text = "Width", AutoSize = true, Margin = new Padding(0, 7, 7, 0) });
        sizeRow.Controls.Add(_widthInput);
        sizeRow.Controls.Add(new Label { Text = "Height", AutoSize = true, Margin = new Padding(16, 7, 7, 0) });
        sizeRow.Controls.Add(_heightInput);

        Button hdButton = new() { Text = "1280 × 720", AutoSize = true, Margin = new Padding(16, 0, 0, 0) };
        hdButton.Click += (_, _) => SetDimensions(1280, 720);
        sizeRow.Controls.Add(hdButton);

        Button fullHdButton = new() { Text = "1920 × 1080", AutoSize = true };
        fullHdButton.Click += (_, _) => SetDimensions(1920, 1080);
        sizeRow.Controls.Add(fullHdButton);

        _autoplayCheckBox.Text = "Autoplay";
        _autoplayCheckBox.AutoSize = true;
        _autoplayCheckBox.Margin = new Padding(16, 5, 0, 0);
        sizeRow.Controls.Add(_autoplayCheckBox);
        root.Controls.Add(sizeRow);

        TableLayoutPanel braveRow = CreateInputRow();
        braveRow.Margin = new Padding(0, 14, 0, 0);
        _bravePathTextBox.Dock = DockStyle.Fill;
        _bravePathTextBox.ReadOnly = true;
        _bravePathTextBox.Margin = new Padding(0, 0, 8, 0);

        Button browseButton = new() { Text = "Browse…", AutoSize = true };
        browseButton.Click += (_, _) => BrowseForBrave();
        braveRow.Controls.Add(_bravePathTextBox, 0, 0);
        braveRow.Controls.Add(browseButton, 1, 0);
        root.Controls.Add(braveRow);

        Button launchButton = new()
        {
            Text = "Launch player",
            AutoSize = true,
            Padding = new Padding(14, 6, 14, 6),
            Margin = new Padding(0, 18, 0, 0)
        };
        launchButton.Click += (_, _) => LaunchPlayer();
        root.Controls.Add(launchButton);
        AcceptButton = launchButton;

        _statusLabel.AutoSize = true;
        _statusLabel.ForeColor = SystemColors.GrayText;
        _statusLabel.Text = "Brave app mode removes tabs, the address bar, comments and recommendations.";
        _statusLabel.Margin = new Padding(0, 18, 0, 0);
        root.Controls.Add(_statusLabel);

        Controls.Add(root);
    }

    private void LaunchPlayer()
    {
        if (!TryCreatePlayerUrl(_urlTextBox.Text, _autoplayCheckBox.Checked, out string? playerUrl, out string error))
        {
            SetStatus(error, true);
            return;
        }

        string? bravePath = FindBrave(_bravePathTextBox.Text);
        if (bravePath is null)
        {
            SetStatus("Brave was not found. Click Browse and select brave.exe.", true);
            return;
        }

        try
        {
            ProcessStartInfo startInfo = new(bravePath)
            {
                UseShellExecute = false
            };

            startInfo.ArgumentList.Add($"--app={playerUrl}");
            startInfo.ArgumentList.Add("--new-window");
            startInfo.ArgumentList.Add($"--window-size={(int)_widthInput.Value},{(int)_heightInput.Value}");
            startInfo.ArgumentList.Add("--disable-session-crashed-bubble");

            Process.Start(startInfo);
            SetStatus("Player opened. Move and resize the Brave app window anywhere on your monitor.", false);
        }
        catch (Exception exception)
        {
            SetStatus($"Could not start Brave: {exception.Message}", true);
        }
    }

    private void BrowseForBrave()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Select Brave Browser",
            Filter = "Brave Browser|brave.exe|Applications|*.exe",
            CheckFileExists = true,
            FileName = "brave.exe"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!string.Equals(Path.GetFileName(dialog.FileName), "brave.exe", StringComparison.OrdinalIgnoreCase))
        {
            SetStatus("Please select Brave's brave.exe file.", true);
            return;
        }

        _bravePathTextBox.Text = dialog.FileName;
        SetStatus("Brave selected.", false);
    }

    private static bool TryCreatePlayerUrl(string input, bool autoplay, out string? playerUrl, out string error)
    {
        playerUrl = null;
        error = string.Empty;
        input = input.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Paste a YouTube URL first.";
            return false;
        }

        string? videoId = null;
        string? playlistId = null;

        if (Regex.IsMatch(input, "^[A-Za-z0-9_-]{11}$", RegexOptions.CultureInvariant))
        {
            videoId = input;
        }
        else
        {
            string normalized = input.Contains("://", StringComparison.Ordinal) ? input : $"https://{input}";
            if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri))
            {
                error = "That is not a valid URL.";
                return false;
            }

            string host = uri.Host.ToLowerInvariant();
            bool isYouTube = host is "youtube.com" or "www.youtube.com" or "m.youtube.com" or "music.youtube.com"
                             || host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase);
            bool isShortHost = host is "youtu.be" or "www.youtu.be";

            if (!isYouTube && !isShortHost)
            {
                error = "Only youtube.com and youtu.be links are supported.";
                return false;
            }

            Dictionary<string, string> query = ParseQuery(uri.Query);
            query.TryGetValue("list", out playlistId);

            string[] segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (isShortHost && segments.Length > 0)
            {
                videoId = segments[0];
            }
            else if (segments.Length >= 2 && (segments[0] == "shorts" || segments[0] == "embed" || segments[0] == "live"))
            {
                videoId = segments[1];
            }
            else if (query.TryGetValue("v", out string? value))
            {
                videoId = value;
            }
        }

        if (!string.IsNullOrWhiteSpace(videoId)
            && !Regex.IsMatch(videoId, "^[A-Za-z0-9_-]{11}$", RegexOptions.CultureInvariant))
        {
            error = "The YouTube video ID is invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(videoId) && string.IsNullOrWhiteSpace(playlistId))
        {
            error = "The link does not contain a video or playlist ID.";
            return false;
        }

        string baseUrl = string.IsNullOrWhiteSpace(videoId)
            ? "https://www.youtube.com/embed/videoseries"
            : $"https://www.youtube.com/embed/{Uri.EscapeDataString(videoId)}";

        List<string> parameters =
        [
            $"autoplay={(autoplay ? 1 : 0)}",
            "controls=1",
            "fs=0",
            "playsinline=1",
            "rel=0"
        ];

        if (!string.IsNullOrWhiteSpace(playlistId))
        {
            parameters.Add($"list={Uri.EscapeDataString(playlistId)}");
        }

        playerUrl = $"{baseUrl}?{string.Join("&", parameters)}";
        return true;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);

        foreach (string pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            string key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
            string value = parts.Length > 1
                ? Uri.UnescapeDataString(parts[1].Replace('+', ' '))
                : string.Empty;
            result[key] = value;
        }

        return result;
    }

    private static string? FindBrave(string? preferredPath = null)
    {
        if (IsBrave(preferredPath))
        {
            return Path.GetFullPath(preferredPath!);
        }

        string[] candidates =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BraveSoftware", "Brave-Browser", "Application", "brave.exe")
        ];

        string? candidate = candidates.FirstOrDefault(IsBrave);
        if (candidate is not null)
        {
            return candidate;
        }

        foreach (RegistryKey root in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            try
            {
                using RegistryKey? key = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\brave.exe");
                string? registeredPath = key?.GetValue(null) as string;
                if (IsBrave(registeredPath))
                {
                    return registeredPath;
                }
            }
            catch
            {
                // Continue to the next installation location.
            }
        }

        return null;
    }

    private static bool IsBrave(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && File.Exists(path)
        && string.Equals(Path.GetFileName(path), "brave.exe", StringComparison.OrdinalIgnoreCase);

    private static TableLayoutPanel CreateInputRow()
    {
        TableLayoutPanel row = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Margin = Padding.Empty
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return row;
    }

    private static void ConfigureDimensionInput(NumericUpDown input, int minimum, int maximum)
    {
        input.Minimum = minimum;
        input.Maximum = maximum;
        input.Increment = 10;
        input.Width = 105;
    }

    private void SetDimensions(int width, int height)
    {
        _widthInput.Value = width;
        _heightInput.Value = height;
    }

    private void SetStatus(string message, bool error)
    {
        _statusLabel.Text = message;
        _statusLabel.ForeColor = error ? Color.Firebrick : SystemColors.GrayText;
    }
}
