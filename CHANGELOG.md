# Changelog

## 0.6.0

- Restored direct Chromium app-mode launching from the last known-good pre-installer architecture.
- Streaming sites now open directly with `--app=<site>` instead of opening `about:blank` and navigating later through DevTools.
- Removed the v0.4-v0.5 fail-closed navigation controller and profile-level fullscreen policy from the runtime.
- Restored the proven v0.3.0 YouTube window-fullscreen implementation for the fullscreen button, `F`, double-click and `Esc`.
- Keeps local DevTools only as a background script injector so current Chrome versions remain supported without the removed `--load-extension` command-line flag.
- Keeps the custom per-browser profile and existing streaming sign-ins.
- Repairs the legacy fullscreen preference written by v0.5.4 before opening a window.
- Keeps the responsive branded control centre, multi-service launcher, installer and self-signing pipeline.
- Updated the private signed installer to version 0.6.0.

## 0.5.5

- Restored the streaming players' fullscreen controls after v0.5.4 prevented them from filling the app window.
- Repairs dedicated browser profiles that persisted `fullscreen=false` in v0.5.4.
- Keeps Chromium's Fullscreen API available while replacing it before page load with the app's window-only implementation.
- Initializes the window-fullscreen controller before installing fallback safety hooks.
- Changed fullscreen controls to toggle on the completed click instead of prematurely on pointer-down.
- Preserves pre-navigation injection, page and iframe monitoring, and fail-closed controller attachment.
- Updated the private signed installer to version 0.5.5.

## 0.5.4

- Enforced window-only fullscreen in the dedicated Chromium profile by disabling native monitor-wide fullscreen before the browser starts.
- Changed streaming windows to open at `about:blank`, install fullscreen protection first and only then navigate to the selected service.
- Refuses to load the streaming website when the local controller cannot be attached safely.
- Added early pointer, mouse, click and keyboard interception for player fullscreen controls.
- Keeps fullscreen controls visible by exposing a compatible synthetic fullscreen API while native fullscreen remains blocked.
- Expanded DevTools monitoring to both page and iframe targets for embedded streaming players.
- Reduced the controller target-discovery interval for faster attachment to newly created player frames.
- Updated the private signed installer to version 0.5.4.

## 0.5.3

- Reworked window-only fullscreen so the live video player stays in its original DOM position instead of being moved into another container.
- Fixed YouTube showing a black player after entering window fullscreen.
- Added fullscreen interception inside existing and future cross-origin player frames.
- Fixed Crunchyroll and similar embedded players escaping into monitor-wide native fullscreen.
- Added capture-phase handling for common fullscreen buttons, labels and keyboard shortcuts.
- Added synthetic fullscreen state and change events so streaming players can update their controls without entering native fullscreen.
- Preserved Escape, double-click and F-key window-fullscreen controls.

## 0.5.2

- Replaced fixed-width launcher buttons with a responsive one-, two- or three-column card layout.
- Added automatic vertical scrolling without allowing cards to extend beyond the application window.
- Added a polished streaming-themed background, branded gradients, hover states and recognisable service logo treatments.
- Improved keyboard focus and accessibility for service cards.
- Separated successful browser launch from optional DevTools controller attachment.
- Removed the false blocking error when a website window opens before the local controller becomes available.
- Added quiet background reconnection for delayed DevTools endpoints.
- Added a persistent debugging port per browser profile to improve reconnection across application launches.

## 0.5.1

- Added Authenticode signing for both the application executable and the final installer.
- Added signature verification to the GitHub Actions build.
- Added a private-build self-signed certificate trust package.
- Added a certificate installation helper for Windows Trusted Root and Trusted Publishers stores.
- Added release checksums covering the installer, certificate and trust package.
- Supports an optional persistent signing PFX through encrypted GitHub secrets; otherwise each build creates a build-specific private certificate.

## 0.5.0

- Added a persistent control-center window instead of immediately opening the last website.
- Added quick-launch buttons for common streaming services and custom websites.
- Added File, Edit and Help menus.
- Added File → New Window for choosing and opening another streaming site.
- Added File → Open Preferred Website.
- Added Edit options for changing the browser and preferred website.
- Added Help → About with application version information.
- Changed a second application launch to bring the existing control center forward.
- Added support for controlling and injecting fullscreen behaviour into every newly opened streaming window.
- Preserved browser and website settings from version 0.4.0.

## 0.4.0

- Renamed the installed product to Windowed Streaming Player.
- Added a Windows installer that installs under Program Files.
- Added Start-menu and desktop shortcuts, an uninstaller and a proper application icon.
- Added a first-run browser-and-website setup flow.
- Added built-in selections for YouTube, Crunchyroll, Prime Video, Netflix, Disney+ and BBC iPlayer.
- Added support for entering any custom HTTP or HTTPS website URL.
- Added Start-menu, command-line and Shift-at-startup ways to change browser or website later.
- Replaced unpacked-extension launching with local Chromium DevTools injection.
- Generalised video-only window fullscreen for HTML5 streaming sites.
- Added persistent per-browser application profiles.

## 0.3.0

- Added a browser picker for Brave, Google Chrome, Microsoft Edge, Vivaldi and Chromium.
- Changed window fullscreen to a top-level video-only overlay.

## 0.2.1

- Fixed window fullscreen leaving the video at its previous calculated size against a black background.

## 0.2.0

- Removed the separate URL-launcher window.
- Added normal YouTube browsing in a single visible app window.
