# Changelog

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
