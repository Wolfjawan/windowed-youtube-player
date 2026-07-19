# Windowed YouTube Player

A small Windows application that opens YouTube as one clean, resizable Brave app window.

It is designed for ultrawide and large monitors where normal fullscreen takes over too much screen space. You can browse YouTube normally, search for videos, and then make a video fill only the current window.

## What it does

- Opens directly on the normal YouTube home page.
- Shows only one visible window; there is no separate URL launcher.
- Lets you search, browse, sign in, use subscriptions, history, playlists, comments and recommendations normally.
- Uses Brave's Chromium app mode, so there are no browser tabs or address bar around YouTube.
- Replaces YouTube's monitor-wide fullscreen behaviour with **window fullscreen**.
- Keeps the Brave app window movable and resizable while the video fills its complete content area.
- Supports the normal YouTube fullscreen button, the `F` key and double-clicking the video.
- Uses `Esc` to return to normal YouTube browsing.

## How it works

The executable starts Brave with a dedicated persistent profile and a small unpacked extension generated under the user's local application-data directory.

The extension runs only on YouTube. When window fullscreen is active, it hides the YouTube header, sidebars, comments and recommendations, and expands the video player to the complete Brave app window. It does not request physical-monitor fullscreen.

Brave remains a separate process; it is not copied or embedded as a reusable browser SDK.

## Dedicated Brave profile

The application uses this persistent profile directory:

```text
%LOCALAPPDATA%\WindowedYouTubePlayer\BraveProfile
```

You may sign in to YouTube once in that window. Cookies, login state and YouTube preferences remain available on later launches.

The generated extension is stored at:

```text
%LOCALAPPDATA%\WindowedYouTubePlayer\WindowFullscreenExtension
```

## Requirements

- Windows 10 or Windows 11
- Brave Browser installed

The GitHub release is self-contained and does not require a separate .NET installation.

## Usage

1. Run `WindowedYouTubePlayer.exe`.
2. Search or browse directly inside YouTube.
3. Open a video.
4. Click YouTube's fullscreen button, press `F`, or double-click the video.
5. The video fills only the resizable Brave app window.
6. Press `Esc` to return to the normal YouTube page.

Windows snap shortcuts such as `Win + Left` and `Win + Right` remain available because the physical display is never placed into fullscreen mode.

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

The GitHub Actions workflow uses GitHub's hosted `ubuntu-latest` runner to cross-publish a self-contained Windows x64 build.

Pull requests compile and upload a test artifact. Merging a pull request into `main`, pushing to `main`, or manually running the workflow builds the application and creates or updates the configured GitHub Release.

Release files:

- `WindowedYouTubePlayer-win-x64.zip`
- `WindowedYouTubePlayer-win-x64.sha256`

## Current limitations

- Brave must be installed.
- This is a dedicated Brave profile, separate from the user's normal Brave profile.
- Browser-level fullscreen such as `F11` is controlled by Brave itself. Use YouTube's fullscreen button, `F`, or video double-click for window fullscreen.
- YouTube may change its page structure, requiring updates to the window-fullscreen extension selectors.

## Licence

MIT
