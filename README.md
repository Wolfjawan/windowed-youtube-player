# Windowed Streaming Player

Windowed Streaming Player is an installable Windows application for opening YouTube, Crunchyroll, Prime Video, Netflix and other websites in separate, resizable Chromium **app windows**.

It is designed for ultrawide and large monitors where ordinary fullscreen takes over the entire display. A video can fill only its current app window while the window remains movable, resizable and compatible with Windows Snap.

## Clean streaming windows

Version 0.6.0 restores the direct app-window launch model used before the installer-era runtime rewrite.

When you choose a service, the app launches the selected Chromium browser directly with `--app=<service URL>`. The streaming window therefore opens as an app window rather than as a normal browser window: there is no normal tab strip, address bar or New Tab UI surrounding the service.

The app does not open `about:blank` first and does not use DevTools to navigate the window. Local DevTools access is used only after launch to install and maintain the window-fullscreen behaviour.

Each selected browser keeps a dedicated persistent profile, so streaming logins, cookies and preferences remain available between launches.

## Responsive control center

The persistent control center uses responsive streaming-service cards instead of fixed-width buttons. Depending on the available width, the launcher automatically arranges the cards into one, two or three columns. Cards never extend beyond the application window; smaller windows use vertical scrolling.

Each service has a branded colour treatment and recognisable logo mark for YouTube, Crunchyroll, Prime Video, Netflix, Disney+ and BBC iPlayer. A custom-website card is also available.

The control center includes:

- branded quick-launch cards
- the currently selected browser and preferred website
- File, Edit and Help menus
- support for opening multiple streaming app windows
- keyboard-accessible cards with visible focus states

Launching the application again brings the existing control center to the front.

## Application menus

### File

- **New Window…** (`Ctrl+N`) — choose a preset or custom website and open it in another streaming app window
- **Open Preferred Website** (`Ctrl+Shift+N`)
- **Exit**

### Edit

- **Change Browser…**
- **Change Preferred Website…**

### Help

- **About**

## Window-only fullscreen

The fullscreen button is intended to fill **the current streaming app window**, not the physical monitor.

Clicking the website's fullscreen button, pressing `F`, or double-clicking a visible video enters window-only fullscreen. `Esc`, `F` again, or the fullscreen button again restores the normal page.

For YouTube, version 0.6.0 uses a dedicated fullscreen path: the existing YouTube player stays in its original document structure and is fixed over the app window. The live video element is not moved to a replacement DOM container, avoiding the black-video regression caused by relocating GPU-rendered playback surfaces.

Other streaming services use a generic fullscreen handler installed in page and iframe targets. Fullscreen API requests are redirected to the window-only layout, and embedded player frames can propagate their fullscreen state to the containing page.

If the local fullscreen controller is briefly unavailable when a streaming app window opens, the website still opens normally and the controller retries quietly. Opening the streaming service is no longer dependent on a DevTools navigation step.

## Browser connection behaviour

Each dedicated browser profile stores a stable local debugging port. The browser is launched directly to the requested website in app mode; the local controller then attaches in the background solely to install fullscreen handling in current and future page/iframe targets.

Version 0.6.0 also repairs the fullscreen preference written by v0.5.4 when reusing an existing dedicated profile.

## Private self-signed installation

Version 0.6.0 signs both `WindowedStreamingPlayer.exe` and the final setup executable with Authenticode. Because this is a private self-signed build, Windows does not trust the certificate automatically.

Use this order:

1. Download `WindowedStreamingPlayer-PrivateTrust.zip` from the same release.
2. Extract it.
3. Right-click `Trust-WindowedStreamingPlayer-Certificate.cmd` and choose **Run as administrator**.
4. Confirm that the certificate was added to the Windows Trusted Root and Trusted Publishers stores.
5. Download `WindowedStreamingPlayer-Setup-0.6.0.exe` after installing the certificate.
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

GitHub Actions uses a GitHub-hosted Windows runner. Pull requests compile and sign a validation installer. When a pull request is merged into `main`, a merge-triggered job rebuilds the signed package, creates or updates the declared version release and verifies that the installer, trust ZIP, public certificate and checksum file are all present.

## Logo sources

The launcher draws its brand marks locally and does not download artwork while running. Brand names and logos remain trademarks of their respective owners. Simple Icons was used as a reference for consistent brand identification; its repository provides SVG icons and asks users to review its trademark disclaimer.

## Licence

MIT
