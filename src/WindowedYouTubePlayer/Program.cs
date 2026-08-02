using System.Diagnostics;
using System.Drawing;
using System.Text;
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

        try
        {
            ChromiumYouTubeApp.Launch();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Windowed YouTube Player could not start.\n\n{exception.Message}",
                "Windowed YouTube Player",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}

internal sealed record BrowserDefinition(string Id, string DisplayName, string ExecutablePath)
{
    public string DisplayText => $"{DisplayName} — {ExecutablePath}";
}

internal static class ChromiumYouTubeApp
{
    private const string StartUrl = "https://www.youtube.com/";
    private const string ProductFolderName = "WindowedYouTubePlayer";

    public static void Launch()
    {
        bool forceBrowserSelection = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
        BrowserDefinition? browser = forceBrowserSelection ? null : BrowserLocator.FindSavedBrowser();

        if (browser is null)
        {
            IReadOnlyList<BrowserDefinition> installedBrowsers = BrowserLocator.FindInstalledBrowsers();
            BrowserDefinition? defaultBrowser = BrowserLocator.FindWindowsDefaultBrowser();

            using BrowserPickerForm picker = new(installedBrowsers, defaultBrowser);
            if (picker.ShowDialog() != DialogResult.OK || picker.SelectedBrowser is null)
            {
                return;
            }

            browser = picker.SelectedBrowser;
            BrowserLocator.SaveBrowser(browser);
        }

        string appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ProductFolderName);
        string profileDirectory = Path.Combine(appDataDirectory, "BrowserProfiles", browser.Id);
        string extensionDirectory = Path.Combine(appDataDirectory, "WindowFullscreenExtension");

        Directory.CreateDirectory(profileDirectory);
        YouTubeWindowExtension.Install(extensionDirectory);

        ProcessStartInfo startInfo = new(browser.ExecutablePath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(browser.ExecutablePath) ?? Environment.CurrentDirectory
        };

        startInfo.ArgumentList.Add($"--user-data-dir={profileDirectory}");
        startInfo.ArgumentList.Add($"--load-extension={extensionDirectory}");
        startInfo.ArgumentList.Add($"--app={StartUrl}");
        startInfo.ArgumentList.Add("--new-window");
        startInfo.ArgumentList.Add("--window-size=1280,720");
        startInfo.ArgumentList.Add("--no-first-run");
        startInfo.ArgumentList.Add("--no-default-browser-check");
        startInfo.ArgumentList.Add("--disable-session-crashed-bubble");
        startInfo.ArgumentList.Add("--disable-background-mode");

        if (Process.Start(startInfo) is null)
        {
            throw new InvalidOperationException($"{browser.DisplayName} did not return a running process.");
        }
    }
}

internal sealed class BrowserPickerForm : Form
{
    private readonly ListBox _browserList = new();
    private readonly Button _useButton = new();

    public BrowserDefinition? SelectedBrowser { get; private set; }

    public BrowserPickerForm(
        IReadOnlyList<BrowserDefinition> installedBrowsers,
        BrowserDefinition? defaultBrowser)
    {
        Text = "Choose browser";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(650, 430);
        Size = new Size(760, 500);
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildLayout();

        foreach (BrowserDefinition browser in installedBrowsers)
        {
            _browserList.Items.Add(browser);
        }

        BrowserDefinition? preferred = FindMatchingItem(defaultBrowser)
            ?? installedBrowsers.FirstOrDefault();

        if (preferred is not null)
        {
            _browserList.SelectedItem = preferred;
        }

        UpdateUseButton();
    }

    private void BuildLayout()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            ColumnCount = 1,
            RowCount = 5
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label title = new()
        {
            AutoSize = true,
            Text = "Choose the browser used by Windowed YouTube Player",
            Font = new Font(Font.FontFamily, 16F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };
        root.Controls.Add(title);

        Label explanation = new()
        {
            AutoSize = true,
            MaximumSize = new Size(700, 0),
            Text = "Select an installed Chromium-based browser. Your choice is saved and reused. Hold Shift while starting this app to choose a different browser later.",
            Margin = new Padding(0, 0, 0, 14)
        };
        root.Controls.Add(explanation);

        _browserList.Dock = DockStyle.Fill;
        _browserList.IntegralHeight = false;
        _browserList.DisplayMember = nameof(BrowserDefinition.DisplayText);
        _browserList.SelectedIndexChanged += (_, _) => UpdateUseButton();
        _browserList.DoubleClick += (_, _) => UseSelectedBrowser();
        root.Controls.Add(_browserList);

        Label supportNote = new()
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Text = "Supported: Brave, Google Chrome, Microsoft Edge, Vivaldi and Chromium. Firefox is not supported because this app relies on Chromium app mode and an unpacked extension.",
            MaximumSize = new Size(700, 0),
            Margin = new Padding(0, 12, 0, 8)
        };
        root.Controls.Add(supportNote);

        FlowLayoutPanel buttons = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0, 8, 0, 0)
        };

        _useButton.Text = "Use selected browser";
        _useButton.AutoSize = true;
        _useButton.Padding = new Padding(12, 5, 12, 5);
        _useButton.Click += (_, _) => UseSelectedBrowser();

        Button cancelButton = new()
        {
            Text = "Cancel",
            AutoSize = true,
            Padding = new Padding(10, 5, 10, 5),
            DialogResult = DialogResult.Cancel
        };

        Button browseButton = new()
        {
            Text = "Browse…",
            AutoSize = true,
            Padding = new Padding(10, 5, 10, 5)
        };
        browseButton.Click += (_, _) => BrowseForBrowser();

        buttons.Controls.Add(_useButton);
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(browseButton);
        root.Controls.Add(buttons);

        AcceptButton = _useButton;
        CancelButton = cancelButton;
        Controls.Add(root);
    }

    private BrowserDefinition? FindMatchingItem(BrowserDefinition? browser)
    {
        if (browser is null)
        {
            return null;
        }

        return _browserList.Items
            .OfType<BrowserDefinition>()
            .FirstOrDefault(item => string.Equals(
                item.ExecutablePath,
                browser.ExecutablePath,
                StringComparison.OrdinalIgnoreCase));
    }

    private void UpdateUseButton()
    {
        _useButton.Enabled = _browserList.SelectedItem is BrowserDefinition;
    }

    private void UseSelectedBrowser()
    {
        if (_browserList.SelectedItem is not BrowserDefinition browser)
        {
            return;
        }

        SelectedBrowser = browser;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void BrowseForBrowser()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Select a Chromium-based browser",
            Filter = "Supported browsers|brave.exe;chrome.exe;msedge.exe;vivaldi.exe;chromium.exe|Applications (*.exe)|*.exe",
            CheckFileExists = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!BrowserLocator.TryCreateBrowser(dialog.FileName, out BrowserDefinition? browser, out string error))
        {
            MessageBox.Show(
                this,
                error,
                "Unsupported browser",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        BrowserDefinition? existing = FindMatchingItem(browser);
        if (existing is null)
        {
            _browserList.Items.Add(browser);
            existing = browser;
        }

        _browserList.SelectedItem = existing;
        _browserList.TopIndex = Math.Max(0, _browserList.SelectedIndex);
    }
}

internal static class BrowserLocator
{
    private const string ProductFolderName = "WindowedYouTubePlayer";

    private static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ProductFolderName);

    private static string BrowserPathFile => Path.Combine(SettingsDirectory, "browser-path.txt");

    public static BrowserDefinition? FindSavedBrowser()
    {
        string? savedPath = ReadPath(BrowserPathFile);
        return TryCreateBrowser(savedPath, out BrowserDefinition? browser, out _)
            ? browser
            : null;
    }

    public static IReadOnlyList<BrowserDefinition> FindInstalledBrowsers()
    {
        Dictionary<string, BrowserDefinition> browsers = new(StringComparer.OrdinalIgnoreCase);

        BrowserDefinition? defaultBrowser = FindWindowsDefaultBrowser();
        AddBrowser(browsers, defaultBrowser);

        foreach (string executableName in new[] { "brave.exe", "chrome.exe", "msedge.exe", "vivaldi.exe", "chromium.exe" })
        {
            foreach (RegistryHive hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            {
                foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                {
                    AddBrowser(browsers, BrowserFromPath(ReadAppPath(executableName, hive, view)));
                }
            }
        }

        foreach (string candidate in StandardCandidates())
        {
            AddBrowser(browsers, BrowserFromPath(candidate));
        }

        return browsers.Values
            .OrderBy(browser => browser.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static BrowserDefinition? FindWindowsDefaultBrowser()
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

            using RegistryKey? commandKey = Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command");
            string? command = commandKey?.GetValue(null) as string;
            string? executablePath = ExtractExecutablePath(command);
            return BrowserFromPath(executablePath);
        }
        catch
        {
            return null;
        }
    }

    public static void SaveBrowser(BrowserDefinition browser)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(BrowserPathFile, browser.ExecutablePath, new UTF8Encoding(false));
        }
        catch
        {
            // The browser can still launch if the preference cannot be persisted.
        }
    }

    public static bool TryCreateBrowser(
        string? path,
        out BrowserDefinition? browser,
        out string error)
    {
        browser = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            error = "The selected browser executable does not exist.";
            return false;
        }

        string fullPath = Path.GetFullPath(path);
        string executableName = Path.GetFileName(fullPath).ToLowerInvariant();

        browser = executableName switch
        {
            "brave.exe" => new BrowserDefinition("brave", "Brave", fullPath),
            "msedge.exe" => new BrowserDefinition("edge", "Microsoft Edge", fullPath),
            "vivaldi.exe" => new BrowserDefinition("vivaldi", "Vivaldi", fullPath),
            "chromium.exe" => new BrowserDefinition("chromium", "Chromium", fullPath),
            "chrome.exe" when fullPath.Contains("Chromium", StringComparison.OrdinalIgnoreCase)
                => new BrowserDefinition("chromium", "Chromium", fullPath),
            "chrome.exe" => new BrowserDefinition("chrome", "Google Chrome", fullPath),
            _ => null
        };

        if (browser is null)
        {
            error = "Select Brave, Google Chrome, Microsoft Edge, Vivaldi or Chromium. Firefox and other non-Chromium browsers cannot load the required window-fullscreen extension.";
            return false;
        }

        return true;
    }

    private static BrowserDefinition? BrowserFromPath(string? path) =>
        TryCreateBrowser(path, out BrowserDefinition? browser, out _) ? browser : null;

    private static void AddBrowser(
        IDictionary<string, BrowserDefinition> browsers,
        BrowserDefinition? browser)
    {
        if (browser is null)
        {
            return;
        }

        browsers.TryAdd(browser.ExecutablePath, browser);
    }

    private static string? ReadPath(string filePath)
    {
        try
        {
            return File.Exists(filePath) ? File.ReadAllText(filePath).Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadAppPath(
        string executableName,
        RegistryHive hive,
        RegistryView view)
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

    private static string? ExtractExecutablePath(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        Match match = Regex.Match(
            command,
            "^\\s*(?:\"(?<quoted>[^\"]+\\.exe)\"|(?<plain>[^\\s]+\\.exe))",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            return null;
        }

        return match.Groups["quoted"].Success
            ? match.Groups["quoted"].Value
            : match.Groups["plain"].Value;
    }

    private static IEnumerable<string> StandardCandidates()
    {
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return new[]
        {
            Path.Combine(programFiles, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
            Path.Combine(programFilesX86, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
            Path.Combine(localAppData, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
            Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(localAppData, "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(localAppData, "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(programFiles, "Vivaldi", "Application", "vivaldi.exe"),
            Path.Combine(localAppData, "Vivaldi", "Application", "vivaldi.exe"),
            Path.Combine(programFiles, "Chromium", "Application", "chrome.exe"),
            Path.Combine(localAppData, "Chromium", "Application", "chrome.exe")
        };
    }
}

internal static class YouTubeWindowExtension
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    public static void Install(string extensionDirectory)
    {
        Directory.CreateDirectory(extensionDirectory);
        File.WriteAllText(Path.Combine(extensionDirectory, "manifest.json"), Manifest, Utf8WithoutBom);
        File.WriteAllText(Path.Combine(extensionDirectory, "content.css"), ContentCss, Utf8WithoutBom);
        File.WriteAllText(Path.Combine(extensionDirectory, "content.js"), ContentScript, Utf8WithoutBom);
    }

    private const string Manifest = """
        {
          "manifest_version": 3,
          "name": "Windowed YouTube Player",
          "version": "0.3.0",
          "description": "Makes YouTube fullscreen fill only its resizable Chromium app window.",
          "content_scripts": [{
            "matches": ["https://www.youtube.com/*", "https://youtube.com/*", "https://m.youtube.com/*"],
            "css": ["content.css"],
            "js": ["content.js"],
            "run_at": "document_start",
            "all_frames": false
          }]
        }
        """;

    private const string ContentCss = """
        html.wyp-window-fullscreen,
        html.wyp-window-fullscreen body {
          width: 100% !important;
          height: 100% !important;
          margin: 0 !important;
          overflow: hidden !important;
          background: #000 !important;
        }

        #wyp-player-overlay {
          position: fixed !important;
          inset: 0 !important;
          z-index: 2147483647 !important;
          width: 100vw !important;
          height: 100vh !important;
          margin: 0 !important;
          padding: 0 !important;
          overflow: hidden !important;
          background: #000 !important;
          isolation: isolate !important;
        }

        #wyp-player-overlay > #movie_player,
        #wyp-player-overlay > .html5-video-player {
          position: absolute !important;
          inset: 0 !important;
          z-index: 1 !important;
          box-sizing: border-box !important;
          width: 100% !important;
          height: 100% !important;
          min-width: 0 !important;
          min-height: 0 !important;
          max-width: none !important;
          max-height: none !important;
          margin: 0 !important;
          padding: 0 !important;
          transform: none !important;
          background: #000 !important;
        }

        #wyp-player-overlay .html5-video-container {
          position: absolute !important;
          inset: 0 !important;
          width: 100% !important;
          height: 100% !important;
          max-width: none !important;
          max-height: none !important;
          margin: 0 !important;
          padding: 0 !important;
          transform: none !important;
          overflow: hidden !important;
          background: #000 !important;
        }

        #wyp-player-overlay video.html5-main-video {
          position: absolute !important;
          inset: 0 !important;
          left: 0 !important;
          top: 0 !important;
          width: 100% !important;
          height: 100% !important;
          max-width: none !important;
          max-height: none !important;
          margin: 0 !important;
          transform: none !important;
          object-fit: contain !important;
          background: #000 !important;
        }

        #wyp-player-overlay .ytp-chrome-bottom {
          left: 12px !important;
          width: calc(100% - 24px) !important;
        }

        #wyp-player-overlay .ytp-gradient-bottom,
        #wyp-player-overlay .ytp-gradient-top {
          width: 100% !important;
        }

        #wyp-window-fullscreen-hint {
          position: fixed;
          left: 50%;
          bottom: 76px;
          z-index: 2147483647;
          transform: translateX(-50%);
          padding: 9px 14px;
          border-radius: 18px;
          color: #fff;
          background: rgba(20, 20, 20, 0.88);
          box-shadow: 0 4px 18px rgba(0, 0, 0, 0.35);
          font: 500 13px/1.2 Arial, sans-serif;
          pointer-events: none;
          opacity: 0;
          transition: opacity 140ms ease;
        }

        #wyp-window-fullscreen-hint.wyp-visible { opacity: 1; }
        """;

    private const string ContentScript = """
        (() => {
          'use strict';
          if (window.top !== window.self) return;

          const rootClass = 'wyp-window-fullscreen';
          const overlayId = 'wyp-player-overlay';
          const hintId = 'wyp-window-fullscreen-hint';
          let hintTimer = 0;
          let resizeFrame = 0;
          let originalParent = null;
          let originalNextSibling = null;
          let playerStyleSnapshot = null;
          let videoStyleSnapshot = null;

          const playerElement = () => document.querySelector(
            'ytd-watch-flexy #movie_player, ytd-watch-flexy .html5-video-player, #movie_player, .html5-video-player'
          );
          const isWatchPage = () => location.pathname === '/watch'
            || location.pathname.startsWith('/live/')
            || location.pathname.startsWith('/shorts/');
          const isWindowFullscreen = () => document.documentElement.classList.contains(rootClass);
          const isEditableTarget = target => target instanceof Element && (
            target.matches('input, textarea, select, [contenteditable="true"]')
            || Boolean(target.closest('input, textarea, select, [contenteditable="true"]'))
          );

          function updateFullscreenButtons() {
            const label = isWindowFullscreen()
              ? 'Exit window fullscreen (Esc)'
              : 'Fill this window (F)';
            document.querySelectorAll('.ytp-fullscreen-button').forEach(button => {
              button.setAttribute('title', label);
              button.setAttribute('aria-label', label);
            });
          }

          function snapshotStyle(element) {
            return element?.hasAttribute('style') ? element.getAttribute('style') : null;
          }

          function restoreStyle(element, snapshot) {
            if (!element) return;
            if (snapshot === null) element.removeAttribute('style');
            else element.setAttribute('style', snapshot);
          }

          function refreshPlayerSize() {
            window.cancelAnimationFrame(resizeFrame);
            resizeFrame = window.requestAnimationFrame(() => {
              if (!isWindowFullscreen()) return;
              const overlay = document.getElementById(overlayId);
              const player = overlay?.querySelector('#movie_player, .html5-video-player');
              if (!player) return;

              player.style.setProperty('width', `${window.innerWidth}px`, 'important');
              player.style.setProperty('height', `${window.innerHeight}px`, 'important');
              player.style.setProperty('left', '0', 'important');
              player.style.setProperty('top', '0', 'important');

              const video = player.querySelector('video.html5-main-video');
              if (video) {
                video.style.setProperty('width', '100%', 'important');
                video.style.setProperty('height', '100%', 'important');
                video.style.setProperty('left', '0', 'important');
                video.style.setProperty('top', '0', 'important');
                video.style.setProperty('transform', 'none', 'important');
              }

              if (typeof player.setSize === 'function') {
                try { player.setSize(window.innerWidth, window.innerHeight); } catch (_) {}
              }
            });
          }

          function showHint() {
            if (!document.body) return;
            let hint = document.getElementById(hintId);
            if (!hint) {
              hint = document.createElement('div');
              hint.id = hintId;
              hint.textContent = 'Video-only window fullscreen · Esc to return to YouTube';
              document.body.appendChild(hint);
            }
            hint.classList.add('wyp-visible');
            window.clearTimeout(hintTimer);
            hintTimer = window.setTimeout(() => hint?.classList.remove('wyp-visible'), 1800);
          }

          function enterWindowFullscreen() {
            const player = playerElement();
            if (!document.body || !isWatchPage() || !player) return;
            if (document.getElementById(overlayId)) {
              refreshPlayerSize();
              return;
            }

            originalParent = player.parentNode;
            originalNextSibling = player.nextSibling;
            playerStyleSnapshot = snapshotStyle(player);
            videoStyleSnapshot = snapshotStyle(player.querySelector('video.html5-main-video'));

            const overlay = document.createElement('div');
            overlay.id = overlayId;
            document.body.appendChild(overlay);
            overlay.appendChild(player);

            document.documentElement.classList.add(rootClass);
            document.body.classList.add(rootClass);
            updateFullscreenButtons();
            refreshPlayerSize();
            window.setTimeout(refreshPlayerSize, 80);
            window.setTimeout(refreshPlayerSize, 300);
            showHint();
          }

          function exitWindowFullscreen() {
            const overlay = document.getElementById(overlayId);
            const player = overlay?.querySelector('#movie_player, .html5-video-player');
            const video = player?.querySelector('video.html5-main-video');

            document.documentElement.classList.remove(rootClass);
            document.body?.classList.remove(rootClass);

            if (player && originalParent) {
              if (originalNextSibling && originalNextSibling.parentNode === originalParent) {
                originalParent.insertBefore(player, originalNextSibling);
              } else {
                originalParent.appendChild(player);
              }
            }

            restoreStyle(player, playerStyleSnapshot);
            restoreStyle(video, videoStyleSnapshot);
            overlay?.remove();

            originalParent = null;
            originalNextSibling = null;
            playerStyleSnapshot = null;
            videoStyleSnapshot = null;
            updateFullscreenButtons();
            window.setTimeout(() => window.dispatchEvent(new Event('resize')), 0);
          }

          function setWindowFullscreen(enabled) {
            if (enabled) enterWindowFullscreen();
            else exitWindowFullscreen();
          }

          const toggleWindowFullscreen = () => setWindowFullscreen(!isWindowFullscreen());

          document.addEventListener('click', event => {
            const button = event.target instanceof Element
              ? event.target.closest('.ytp-fullscreen-button')
              : null;
            if (!button) return;
            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
            toggleWindowFullscreen();
          }, true);

          document.addEventListener('dblclick', event => {
            const target = event.target instanceof Element ? event.target : null;
            if (!target?.closest('.html5-video-player') || target.closest('.ytp-chrome-controls')) return;
            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
            toggleWindowFullscreen();
          }, true);

          document.addEventListener('keydown', event => {
            if (event.key === 'Escape' && isWindowFullscreen()) {
              event.preventDefault();
              event.stopPropagation();
              event.stopImmediatePropagation();
              exitWindowFullscreen();
              return;
            }

            if (event.key.toLowerCase() === 'f'
              && !event.ctrlKey && !event.altKey && !event.metaKey
              && !isEditableTarget(event.target) && isWatchPage() && playerElement()) {
              event.preventDefault();
              event.stopPropagation();
              event.stopImmediatePropagation();
              toggleWindowFullscreen();
            }
          }, true);

          document.addEventListener('fullscreenchange', () => {
            if (!document.fullscreenElement) return;
            document.exitFullscreen().catch(() => {}).finally(enterWindowFullscreen);
          }, true);

          window.addEventListener('resize', () => {
            if (isWindowFullscreen()) refreshPlayerSize();
          });

          window.addEventListener('yt-navigate-start', () => {
            if (isWindowFullscreen()) exitWindowFullscreen();
          });

          window.addEventListener('yt-navigate-finish', () => {
            window.setTimeout(updateFullscreenButtons, 250);
          });

          const observer = new MutationObserver(() => {
            updateFullscreenButtons();
            if (isWindowFullscreen()) refreshPlayerSize();
          });

          const beginObserving = () => {
            if (!document.documentElement) {
              window.setTimeout(beginObserving, 20);
              return;
            }
            observer.observe(document.documentElement, { childList: true, subtree: true });
            updateFullscreenButtons();
          };

          beginObserving();
        })();
        """;
}
