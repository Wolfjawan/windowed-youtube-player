# Windowed Streaming Player

Windowed Streaming Player is an installable Windows application that opens a streaming website in a clean, resizable Chromium-browser app window.

It is designed for ultrawide and large monitors where normal fullscreen takes over the whole display. The video can fill only the current window while the window remains movable, resizable and compatible with Windows Snap.

## Supported setup flow

The first launch after installation guides you through two steps:

1. Choose a Chromium-based browser.
2. Choose a streaming website or enter its main URL.

Detected browsers include Brave, Google Chrome, Microsoft Edge, Vivaldi, Opera and Chromium. A **Browse…** option allows another Chromium-based browser executable to be selected manually.

Built-in website choices include:

- YouTube
- Crunchyroll
- Prime Video
- Netflix
- Disney+
- BBC iPlayer
- Any custom HTTP or HTTPS website

The browser and website choices are saved. Open **Change browser or website** from the Start menu, run the app with `--settings`, or hold **Shift** while launching to change them.

## Video-only window fullscreen

The app injects a small controller through the selected browser's local DevTools interface. When a website requests fullscreen, or when you press `F` or double-click a visible video, the active player is moved into a top-level black overlay that fills only the app window.

This hides the website header, search bar, title, channel or programme details, comments, recommendations and other page content. Press `Esc` to restore the player to its original page position.

## Installation

Download `WindowedStreamingPlayer-Setup-0.4.0.exe` from the GitHub release and run it.

The installer:

- installs under `Program Files\Windowed Streaming Player`
- creates a Start-menu shortcut
- creates a desktop shortcut by default
- adds a **Change browser or website** Start-menu shortcut
- includes a proper application and uninstaller icon
- adds a normal Windows uninstaller
- launches the first-run setup after installation

The application is self-contained and does not require a separate .NET installation.

## Browser data

The app keeps separate persistent browser profiles under:

```text
%LOCALAPPDATA%\WindowedYouTubePlayer\BrowserProfiles
```

Streaming-service sign-ins and preferences remain available on later launches. These profiles are separate from the browser's normal profile.

## Requirements

- Windows 10 or Windows 11, 64-bit
- A Chromium-based browser
- Internet access for the selected streaming service

Firefox is not currently supported because this application relies on Chromium's app-window and DevTools interfaces.

## Build from source

Install .NET 8 SDK and Inno Setup 6, then run:

```powershell
.\scripts\publish.ps1
```

The installer is written to `artifacts\installer`.

## Automated builds

GitHub Actions uses the GitHub-hosted `windows-latest` runner. Pull requests build an installer artifact. A push to `main` publishes the version declared in the project file unless that release already exists.

## Licence

MIT
