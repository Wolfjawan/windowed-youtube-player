using System.Diagnostics;
using System.Text;
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
            BraveYouTubeApp.Launch();
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

internal static class BraveYouTubeApp
{
    private const string StartUrl = "https://www.youtube.com/";
    private const string ProductFolderName = "WindowedYouTubePlayer";
    private const string BraveExecutableName = "brave.exe";

    public static void Launch()
    {
        string? bravePath = BraveLocator.Find();
        if (bravePath is null)
        {
            bravePath = SelectBraveExecutable();
            if (bravePath is null)
            {
                return;
            }

            BraveLocator.SavePreferredPath(bravePath);
        }

        string appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ProductFolderName);
        string profileDirectory = Path.Combine(appDataDirectory, "BraveProfile");
        string extensionDirectory = Path.Combine(appDataDirectory, "WindowFullscreenExtension");

        Directory.CreateDirectory(profileDirectory);
        YouTubeWindowExtension.Install(extensionDirectory);

        ProcessStartInfo startInfo = new(bravePath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(bravePath) ?? Environment.CurrentDirectory
        };

        startInfo.ArgumentList.Add($"--user-data-dir={profileDirectory}");
        startInfo.ArgumentList.Add($"--disable-extensions-except={extensionDirectory}");
        startInfo.ArgumentList.Add($"--load-extension={extensionDirectory}");
        startInfo.ArgumentList.Add($"--app={StartUrl}");
        startInfo.ArgumentList.Add("--new-window");
        startInfo.ArgumentList.Add("--window-size=1280,720");
        startInfo.ArgumentList.Add("--no-first-run");
        startInfo.ArgumentList.Add("--no-default-browser-check");
        startInfo.ArgumentList.Add("--disable-session-crashed-bubble");
        startInfo.ArgumentList.Add("--disable-background-mode");

        Process? process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Brave did not return a running process.");
        }
    }

    private static string? SelectBraveExecutable()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Select Brave Browser",
            Filter = "Brave Browser (brave.exe)|brave.exe|Applications (*.exe)|*.exe",
            CheckFileExists = true,
            FileName = BraveExecutableName
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return null;
        }

        if (!BraveLocator.IsBraveExecutable(dialog.FileName))
        {
            MessageBox.Show(
                "Please select Brave Browser's brave.exe file.",
                "Windowed YouTube Player",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return null;
        }

        return Path.GetFullPath(dialog.FileName);
    }
}

internal static class BraveLocator
{
    private const string ProductFolderName = "WindowedYouTubePlayer";
    private const string BraveExecutableName = "brave.exe";

    private static string PreferredPathFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ProductFolderName,
        "brave-path.txt");

    public static string? Find()
    {
        string? savedPath = ReadPreferredPath();
        if (IsBraveExecutable(savedPath))
        {
            return Path.GetFullPath(savedPath!);
        }

        foreach (string candidate in StandardCandidates())
        {
            if (IsBraveExecutable(candidate))
            {
                SavePreferredPath(candidate);
                return candidate;
            }
        }

        foreach (RegistryHive hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        {
            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                string? registeredPath = ReadRegisteredPath(hive, view);
                if (IsBraveExecutable(registeredPath))
                {
                    SavePreferredPath(registeredPath!);
                    return registeredPath;
                }
            }
        }

        return null;
    }

    public static bool IsBraveExecutable(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && File.Exists(path)
        && string.Equals(Path.GetFileName(path), BraveExecutableName, StringComparison.OrdinalIgnoreCase);

    public static void SavePreferredPath(string path)
    {
        try
        {
            string? directory = Path.GetDirectoryName(PreferredPathFile);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(PreferredPathFile, Path.GetFullPath(path), new UTF8Encoding(false));
        }
        catch
        {
            // Brave can still launch if the preferred path cannot be persisted.
        }
    }

    private static string? ReadPreferredPath()
    {
        try
        {
            return File.Exists(PreferredPathFile)
                ? File.ReadAllText(PreferredPathFile).Trim()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> StandardCandidates()
    {
        string[] roots =
        [
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        ];

        foreach (string root in roots.Where(root => !string.IsNullOrWhiteSpace(root)))
        {
            yield return Path.Combine(
                root,
                "BraveSoftware",
                "Brave-Browser",
                "Application",
                BraveExecutableName);
        }
    }

    private static string? ReadRegisteredPath(RegistryHive hive, RegistryView view)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
            using RegistryKey? key = baseKey.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\brave.exe");
            return key?.GetValue(null) as string;
        }
        catch
        {
            return null;
        }
    }
}

internal static class YouTubeWindowExtension
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    public static void Install(string extensionDirectory)
    {
        Directory.CreateDirectory(extensionDirectory);

        File.WriteAllText(
            Path.Combine(extensionDirectory, "manifest.json"),
            Manifest,
            Utf8WithoutBom);
        File.WriteAllText(
            Path.Combine(extensionDirectory, "content.css"),
            ContentCss,
            Utf8WithoutBom);
        File.WriteAllText(
            Path.Combine(extensionDirectory, "content.js"),
            ContentScript,
            Utf8WithoutBom);
    }

    private const string Manifest = """
        {
          "manifest_version": 3,
          "name": "Windowed YouTube Player",
          "version": "0.2.1",
          "description": "Makes YouTube fullscreen fill only its resizable Brave app window.",
          "content_scripts": [
            {
              "matches": [
                "https://www.youtube.com/*",
                "https://youtube.com/*",
                "https://m.youtube.com/*"
              ],
              "css": ["content.css"],
              "js": ["content.js"],
              "run_at": "document_start",
              "all_frames": false
            }
          ]
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

        html.wyp-window-fullscreen #movie_player,
        html.wyp-window-fullscreen .html5-video-player {
          position: fixed !important;
          inset: 0 !important;
          z-index: 2147483646 !important;
          box-sizing: border-box !important;
          width: 100vw !important;
          height: 100vh !important;
          min-width: 0 !important;
          min-height: 0 !important;
          max-width: none !important;
          max-height: none !important;
          margin: 0 !important;
          padding: 0 !important;
          transform: none !important;
          background: #000 !important;
        }

        html.wyp-window-fullscreen #movie_player .html5-video-container,
        html.wyp-window-fullscreen .html5-video-player .html5-video-container {
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

        html.wyp-window-fullscreen video.html5-main-video {
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

        html.wyp-window-fullscreen #movie_player .ytp-chrome-bottom {
          left: 12px !important;
          width: calc(100% - 24px) !important;
        }

        html.wyp-window-fullscreen #movie_player .ytp-gradient-bottom,
        html.wyp-window-fullscreen #movie_player .ytp-gradient-top {
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

        #wyp-window-fullscreen-hint.wyp-visible {
          opacity: 1;
        }
        """;

    private const string ContentScript = """
        (() => {
          'use strict';

          if (window.top !== window.self) {
            return;
          }

          const rootClass = 'wyp-window-fullscreen';
          const hintId = 'wyp-window-fullscreen-hint';
          let hintTimer = 0;
          let resizeFrame = 0;

          const playerElement = () =>
            document.querySelector('#movie_player, .html5-video-player');

          const isWatchPage = () =>
            location.pathname === '/watch'
            || location.pathname.startsWith('/live/')
            || location.pathname.startsWith('/shorts/');

          const isEditableTarget = target => {
            if (!(target instanceof Element)) {
              return false;
            }

            return target.matches('input, textarea, select, [contenteditable="true"]')
              || Boolean(target.closest('input, textarea, select, [contenteditable="true"]'));
          };

          const isWindowFullscreen = () =>
            document.documentElement.classList.contains(rootClass);

          function updateFullscreenButtons() {
            const label = isWindowFullscreen()
              ? 'Exit window fullscreen (Esc)'
              : 'Fill this window (F)';

            document.querySelectorAll('.ytp-fullscreen-button').forEach(button => {
              button.setAttribute('title', label);
              button.setAttribute('aria-label', label);
            });
          }

          function refreshPlayerSize() {
            window.cancelAnimationFrame(resizeFrame);
            resizeFrame = window.requestAnimationFrame(() => {
              const player = playerElement();
              if (!player || !isWindowFullscreen()) {
                return;
              }

              player.style.setProperty('width', `${window.innerWidth}px`, 'important');
              player.style.setProperty('height', `${window.innerHeight}px`, 'important');

              const video = player.querySelector('video.html5-main-video');
              if (video) {
                video.style.setProperty('width', '100%', 'important');
                video.style.setProperty('height', '100%', 'important');
                video.style.setProperty('left', '0', 'important');
                video.style.setProperty('top', '0', 'important');
                video.style.setProperty('transform', 'none', 'important');
              }

              window.dispatchEvent(new Event('resize'));
            });
          }

          function showHint() {
            if (!document.body) {
              return;
            }

            let hint = document.getElementById(hintId);
            if (!hint) {
              hint = document.createElement('div');
              hint.id = hintId;
              hint.textContent = 'Window fullscreen · Esc to return to YouTube';
              document.body.appendChild(hint);
            }

            hint.classList.add('wyp-visible');
            window.clearTimeout(hintTimer);
            hintTimer = window.setTimeout(() => {
              hint?.classList.remove('wyp-visible');
            }, 1800);
          }

          function setWindowFullscreen(enabled) {
            if (enabled && (!isWatchPage() || !playerElement())) {
              return;
            }

            document.documentElement.classList.toggle(rootClass, enabled);
            document.body?.classList.toggle(rootClass, enabled);
            updateFullscreenButtons();

            if (enabled) {
              refreshPlayerSize();
              window.setTimeout(refreshPlayerSize, 80);
              window.setTimeout(refreshPlayerSize, 300);
              showHint();
            }
          }

          function toggleWindowFullscreen() {
            setWindowFullscreen(!isWindowFullscreen());
          }

          document.addEventListener('click', event => {
            const target = event.target instanceof Element
              ? event.target.closest('.ytp-fullscreen-button')
              : null;

            if (!target) {
              return;
            }

            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
            toggleWindowFullscreen();
          }, true);

          document.addEventListener('dblclick', event => {
            const target = event.target instanceof Element ? event.target : null;
            const player = target?.closest('.html5-video-player');

            if (!player || target?.closest('.ytp-chrome-controls')) {
              return;
            }

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
              setWindowFullscreen(false);
              return;
            }

            if (
              event.key.toLowerCase() === 'f'
              && !event.ctrlKey
              && !event.altKey
              && !event.metaKey
              && !isEditableTarget(event.target)
              && isWatchPage()
              && playerElement()
            ) {
              event.preventDefault();
              event.stopPropagation();
              event.stopImmediatePropagation();
              toggleWindowFullscreen();
            }
          }, true);

          document.addEventListener('fullscreenchange', () => {
            if (!document.fullscreenElement) {
              return;
            }

            document.exitFullscreen()
              .catch(() => {})
              .finally(() => setWindowFullscreen(true));
          }, true);

          window.addEventListener('resize', () => {
            if (isWindowFullscreen()) {
              refreshPlayerSize();
            }
          });

          window.addEventListener('yt-navigate-finish', () => {
            if (!isWatchPage()) {
              setWindowFullscreen(false);
            }

            window.setTimeout(updateFullscreenButtons, 250);
          });

          const observer = new MutationObserver(() => {
            updateFullscreenButtons();

            if (isWindowFullscreen()) {
              if (!isWatchPage()) {
                setWindowFullscreen(false);
              } else {
                refreshPlayerSize();
              }
            }
          });

          const beginObserving = () => {
            if (!document.documentElement) {
              window.setTimeout(beginObserving, 20);
              return;
            }

            observer.observe(document.documentElement, {
              childList: true,
              subtree: true
            });
            updateFullscreenButtons();
          };

          beginObserving();
        })();
        """;
}
