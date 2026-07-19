# Windowed YouTube Player

A small Windows launcher that plays YouTube in a clean, resizable Brave app window.

It is designed for ultrawide and large monitors where normal YouTube fullscreen is too large, but the surrounding browser and YouTube interface are distracting.

## What it does

- Accepts normal YouTube, `youtu.be`, Shorts, live-video, embed, and playlist links.
- Converts the link into YouTube's focused embed player.
- Launches the player using the installed Brave browser's Chromium engine.
- Removes tabs, the address bar, comments, recommendations, and the normal YouTube page.
- Lets you choose the initial player-window dimensions.
- Disables YouTube's monitor-wide fullscreen button so playback remains windowed.
- Can remove or restore the Brave window frame after launch.
- Reuses the normal Brave profile, including its cookies, hardware acceleration, and Shields settings.

## How it works

The application launches Brave with its Chromium `--app=<URL>` mode. Brave remains a separate process; it is not copied or embedded into this repository.

## Requirements

- Windows 10 or Windows 11
- Brave Browser installed
- .NET 8 Desktop Runtime when running a framework-dependent development build

The release workflow creates a self-contained Windows executable that does not require a separate .NET installation.

## Run from source

```powershell
dotnet run --project .\src\WindowedYouTubePlayer\WindowedYouTubePlayer.csproj
```

## Create a standalone Windows build

```powershell
.\scripts\publish.ps1
```

The output is written to `artifacts\win-x64`.

## Automated Windows builds and releases

The GitHub Actions workflow builds the app on pushes to `main`, pull requests, version tags, and manual runs.

Every successful run uploads a self-contained `win-x64` ZIP as a workflow artifact. A GitHub Release is created when either:

- a tag beginning with `v` is pushed, such as `v0.1.0`; or
- **Run workflow** is selected on the Actions page and a release version is entered.

The release contains:

- `WindowedYouTubePlayer-win-x64.zip`
- `WindowedYouTubePlayer-win-x64.sha256`

## Usage

1. Paste a YouTube video or playlist URL.
2. Select the starting width and height.
3. Select **Start borderless** when you want only the video area visible.
4. Click **Launch player**.
5. Resize the Brave app window to use only the part of the monitor you want.
6. Use **Toggle player frame** to switch between a normal movable frame and the clean borderless presentation.

In borderless mode, Windows snap shortcuts such as `Win + Left` and `Win + Right` are useful for repositioning the player.

## Current limitations

- Some YouTube videos are configured by their publisher to block embedded playback.
- Brave must already be installed.
- Brave is launched externally rather than being embedded as a reusable Brave SDK, because Brave does not provide an official embeddable desktop web-view component.
- When Brave already has many windows open, Windows may occasionally prevent the launcher from identifying the newly created window. Video playback still opens, but frame-control buttons may be unavailable for that launch.

## Planned improvements

- Remember exact player position and size.
- Always-on-top option.
- Video queue and recent-link history.
- Global keyboard shortcuts.
- Aspect-ratio lock while resizing.

## Licence

MIT
