# Windowed Streaming Player

Windowed Streaming Player is an installable Windows application for opening YouTube, Crunchyroll, Prime Video, Netflix and other websites in separate, resizable Chromium app windows.

It is designed for ultrawide and large monitors where ordinary fullscreen takes over the entire display. A video can fill only its current app window while the window remains movable, resizable and compatible with Windows Snap.

## Responsive control center

The persistent control center uses responsive streaming-service cards instead of fixed-width buttons. Depending on the available width, the launcher automatically arranges the cards into one, two or three columns. Cards never extend beyond the application window; smaller windows use vertical scrolling.

Each service has a branded colour treatment and recognisable logo mark for YouTube, Crunchyroll, Prime Video, Netflix, Disney+ and BBC iPlayer. A custom-website card is also available.

The control center includes:

- branded quick-launch cards
- the currently selected browser and preferred website
- File, Edit and Help menus
- support for opening multiple streaming windows
- keyboard-accessible cards with visible focus states

Launching the application again brings the existing control center to the front.

## Application menus

### File

- **New Window…** (`Ctrl+N`) — choose a preset or custom website and open it in another streaming window
- **Open Preferred Website** (`Ctrl+Shift+N`)
- **Exit**

### Edit

- **Change Browser…**
- **Change Preferred Website…**

### Help

- **About**

## Window-only fullscreen

Every new streaming window follows this sequence:

1. Open a blank Chromium app window.
2. Connect the local DevTools controller.
3. Install the window-fullscreen controller before website JavaScript runs.
4. Navigate to the selected streaming service only after protection is active.

If the controller cannot be attached, the app refuses to load the website rather than opening an unprotected window that could take over the monitor.

The Chromium Fullscreen API remains available to the player, but its methods are replaced before page load. A player fullscreen request is converted into the app's window-only mode rather than native monitor-wide fullscreen. Version 0.5.5 also repairs profiles written by v0.5.4 that had fullscreen capability disabled.

Fullscreen control is installed in the top page and embedded player frames. The app watches both page and iframe DevTools targets, including cross-origin frames used by streaming services.

Clicking the website's fullscreen button, pressing `F`, or double-clicking a visible video expands the player only to the edges of its current app window. The Windows title bar and the window's position on the monitor remain under Windows control. Press `Esc`, click the fullscreen button again, or press `F` again to restore the page.

The player remains in its original document structure and is temporarily pinned over a black backdrop. This avoids relocating YouTube's live video element, which can disrupt hardware-rendered video surfaces.

Fullscreen buttons toggle on the completed click. The app does not consume the initial pointer-down event, which allows streaming controls to complete their normal interaction while the fullscreen API remains redirected to window-only mode.

## Browser connection behaviour

Each dedicated browser profile stores a stable local debugging port. New windows are prepared through that controller before navigation. Existing streaming sign-ins and preferences remain saved in the dedicated profile.

## Private self-signed installation

Version 0.5.5 signs both `WindowedStreamingPlayer.exe` and the final setup executable with Authenticode. Because this is a private self-signed build, Windows does not trust the certificate automatically.

Use this order:

1. Download `WindowedStreamingPlayer-PrivateTrust.zip` from the same release.
2. Extract the ZIP.
3. Right-click `Trust-WindowedStreamingPlayer-Certificate.cmd` and choose **Run as administrator**.
4. Confirm that the certificate was added to the Windows Trusted Root and Trusted Publishers stores.
5. Download `WindowedStreamingPlayer-Setup-0.5.5.exe` after installing the certificate.
6. Run the installer.

The release also includes the public `.cer` file and SHA-256 checksums.

A self-signed signature identifies builds signed by that private certificate on machines where the certificate is trusted. It does not provide public publisher reputation, and it does not disable Microsoft Defender malware scanning.

## Signing builds

The GitHub workflow supports two modes:

- **Persistent private certificate:** configure encrypted repository secrets named `WSP_SIGNING_PFX_BASE64` and `WSP_SIGNING_PFX_PASSWORD`.
- **Build-specific certificate:** when those secrets are absent, the workflow generates a new private self-signed certificate and publishes its public certificate with that release.

The private key is never committed to the repository. The workflow signs and inspects the inner application first, builds the installer, then signs and inspects the final setup executable.

## Installation details

The installer:

- installs under `Program Files\Windowed Streaming Player`
- creates Start-menu shortcuts
- creates a desktop shortcut by default
- includes an application and uninstaller icon
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

Firefox is not currently supported because the application relies on Chromium app-window and DevTools interfaces.

## Automated builds and releases

GitHub Actions uses a GitHub-hosted Windows runner. Pull requests compile and sign a validation installer. When a pull request is merged into `main`, a separate merge-triggered job rebuilds the signed package, creates or updates the declared version release and verifies that the installer, trust ZIP, public certificate and checksum file are all present.

## Logo sources

The launcher draws its brand marks locally and does not download artwork while running. Brand names and logos remain trademarks of their respective owners. Simple Icons was used as a reference for consistent brand identification; its repository provides SVG icons and asks users to review its trademark disclaimer.

## Licence

MIT
