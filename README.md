# Windowed Streaming Player

Windowed Streaming Player is an installable Windows application for opening YouTube, Crunchyroll, Prime Video, Netflix and other websites in separate, resizable Chromium app windows.

It is designed for ultrawide and large monitors where normal fullscreen takes over the whole display. A video can fill only its current app window while the window remains movable, resizable and compatible with Windows Snap.

## Control center

Version 0.5.0 introduces a persistent control-center window. Opening the desktop or Start-menu shortcut shows a service chooser instead of immediately opening the previously saved website.

The control center includes:

- quick buttons for YouTube, Crunchyroll, Prime Video, Netflix, Disney+ and BBC iPlayer
- a custom-website option
- the currently selected browser and preferred website
- status information when a streaming window is opened

Launching the application again brings the existing control center to the front.

## Application menus

### File

- **New Window…** (`Ctrl+N`) — choose a preset or custom website and open it in another streaming window
- **Open Preferred Website** (`Ctrl+Shift+N`)
- **Exit**

### Edit

- **Change Browser…** — choose Brave, Chrome, Edge, Vivaldi, Opera, Chromium or browse to another Chromium executable
- **Change Preferred Website…** — change the website preselected by the New Window dialog

### Help

- **About** — display the application version and purpose

Existing streaming windows remain open when the selected browser or preferred website is changed. The new setting applies to later windows.

## Video-only window fullscreen

The app controls each streaming window through the selected browser's local DevTools interface. When a website requests fullscreen, or when you press `F` or double-click a visible video, the active player is moved into a top-level black overlay that fills only that app window.

This hides the website header, search bar, title, channel or programme details, comments, recommendations and other page content. Press `Esc` to restore the player to its original page position.

The controller now attaches to every new streaming window created from the control center, rather than only the first browser window.

## Installation

Download `WindowedStreamingPlayer-Setup-0.5.0.exe` from the GitHub release and run it.

The installer:

- installs under `Program Files\Windowed Streaming Player`
- creates a Start-menu shortcut
- creates a desktop shortcut by default
- adds a **Choose browser** Start-menu shortcut
- includes a proper application and uninstaller icon
- adds a normal Windows uninstaller
- launches the control center after installation

The application is self-contained and does not require a separate .NET installation.

## Browser data

The app keeps a separate persistent profile for each selected browser under:

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
