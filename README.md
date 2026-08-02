# Windowed YouTube Player

A small Windows application that opens YouTube as one clean, resizable Chromium app window.

It is designed for ultrawide and large monitors where normal fullscreen takes over too much screen space. You can browse YouTube normally, search for videos, and then make only the video fill the current window.

## What it does

- Opens directly on the normal YouTube home page.
- Shows only one visible browser window after browser selection.
- Lets you search, browse, sign in, use subscriptions, history, playlists, comments and recommendations normally.
- Supports Brave, Google Chrome, Microsoft Edge, Vivaldi and Chromium.
- Uses Chromium app mode, so there are no browser tabs or address bar around YouTube.
- Replaces YouTube's monitor-wide fullscreen behaviour with **video-only window fullscreen**.
- Hides the YouTube header, search bar, video title, channel information, comments and recommendations while window fullscreen is active.
- Keeps the browser app window movable and resizable while the video fills its complete content area.
- Supports the normal YouTube fullscreen button, the `F` key and double-clicking the video.
- Uses `Esc` to restore the complete YouTube page.

## Browser selection

The first time v0.3.0 starts, it displays a browser picker. The app detects supported installed browsers and preselects the Windows default browser when that browser is supported.

You can also select **Browse…** and choose the browser executable manually. Supported executable names are:

- `brave.exe`
- `chrome.exe`
- `msedge.exe`
- `vivaldi.exe`
- `chromium.exe`

The selection is saved under:

```text
%LOCALAPPDATA%\WindowedYouTubePlayer\browser-path.txt
```

To choose a different browser later, hold **Shift** while starting `WindowedYouTubePlayer.exe`.

Firefox and other non-Chromium browsers are not supported because the app relies on Chromium app mode and an unpacked extension.

## How video-only window fullscreen works

The executable generates a small local browser extension that runs only on YouTube.

When window fullscreen is activated, the extension temporarily moves the live YouTube player into a top-level overlay covering the browser window. This prevents YouTube's header and the content below the video from remaining visible. Pressing `Esc` restores the player to its original place on the YouTube page.

The physical display is never placed into fullscreen mode.

## Dedicated browser profiles

Each supported browser receives its own persistent profile under:

```text
%LOCALAPPDATA%\WindowedYouTubePlayer\BrowserProfiles\<browser>
```

You may sign in to YouTube once in the selected browser window. Cookies, login state and YouTube preferences remain available on later launches using that browser.

The generated extension is stored at:

```text
%LOCALAPPDATA%\WindowedYouTubePlayer\WindowFullscreenExtension
```

## Requirements

- Windows 10 or Windows 11
- Brave, Google Chrome, Microsoft Edge, Vivaldi or Chromium

The GitHub release is self-contained and does not require a separate .NET installation.

## Usage

1. Run `WindowedYouTubePlayer.exe`.
2. Select the browser on first launch.
3. Search or browse directly inside YouTube.
4. Open a video.
5. Click YouTube's fullscreen button, press `F`, or double-click the video.
6. Only the video and its playback controls fill the resizable browser app window.
7. Press `Esc` to return to the normal YouTube page.

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

Pull requests and normal pushes compile and upload workflow artifacts. A merged pull request or manual workflow run can publish the configured GitHub Release.

Release files:

- `WindowedYouTubePlayer-win-x64.zip`
- `WindowedYouTubePlayer-win-x64.sha256`

## Current limitations

- Only Chromium-based browsers that support app mode and unpacked extensions are supported.
- Each browser uses a dedicated profile separate from the user's normal browser profile.
- Browser-level fullscreen such as `F11` is controlled by the browser itself. Use YouTube's fullscreen button, `F`, or video double-click for window fullscreen.
- YouTube may change its page structure, requiring updates to the window-fullscreen extension selectors.

## Licence

MIT
