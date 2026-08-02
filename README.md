# Windowed Streaming Player

Windowed Streaming Player is an installable Windows application for opening YouTube, Crunchyroll, Prime Video, Netflix and other websites in separate, resizable Chromium app windows.

It is designed for ultrawide and large monitors where normal fullscreen takes over the whole display. A video can fill only its current app window while the window remains movable, resizable and compatible with Windows Snap.

## Control center

The persistent control-center window includes:

- quick buttons for YouTube, Crunchyroll, Prime Video, Netflix, Disney+ and BBC iPlayer
- a custom-website option
- the currently selected browser and preferred website
- File, Edit and Help menus
- support for opening multiple streaming windows

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

## Video-only window fullscreen

The app controls each streaming window through the selected Chromium browser's local DevTools interface. When a website requests fullscreen, or when you press `F` or double-click a visible video, the active player fills only that app window.

Press `Esc` to restore the player to its normal page position.

## Private self-signed installation

Version 0.5.1 signs both `WindowedStreamingPlayer.exe` and the final setup executable with Authenticode. Because this is a private self-signed build, Windows does not trust the certificate automatically.

Use this order:

1. Download `WindowedStreamingPlayer-PrivateTrust.zip` from the release.
2. Extract the ZIP.
3. Right-click `Trust-WindowedStreamingPlayer-Certificate.cmd` and choose **Run as administrator**.
4. Confirm that the certificate was added to the Windows Trusted Root and Trusted Publishers stores.
5. Download `WindowedStreamingPlayer-Setup-0.5.1.exe` after installing the certificate.
6. Run the installer.

The release also includes the public `.cer` file and SHA-256 checksums.

A self-signed signature identifies builds signed by that private certificate on machines where the certificate is trusted. It does not provide public publisher reputation, and it does not disable Microsoft Defender malware scanning.

## Signing builds

The GitHub workflow supports two modes:

- **Persistent private certificate:** configure encrypted repository secrets named `WSP_SIGNING_PFX_BASE64` and `WSP_SIGNING_PFX_PASSWORD`.
- **Build-specific certificate:** when those secrets are absent, the workflow generates a new private self-signed certificate and publishes its public certificate with that release.

The private key is never committed to the repository. The workflow signs and verifies the inner application first, builds the installer, then signs and verifies the final setup executable.

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

## Automated builds

GitHub Actions uses a GitHub-hosted Windows runner. Pull requests compile and sign a validation installer. A push to `main` publishes the version declared in the project file unless that release already exists.

## Licence

MIT
