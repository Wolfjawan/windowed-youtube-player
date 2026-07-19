# Changelog

## 0.2.1

- Fixed window fullscreen leaving the video at its previous calculated size against a black background.
- Applies fullscreen positioning only to the actual YouTube player instead of every nested page container.
- Forces the video surface and controls to follow the current Brave window dimensions.
- Recalculates the player after entering fullscreen and whenever the window is resized.

## 0.2.0

- Removed the separate URL-launcher window.
- Opened one persistent Brave app window directly on the normal YouTube website.
- Added normal YouTube browsing, search, sign-in, subscriptions, history, playlists, comments and recommendations.
- Added a dedicated persistent Brave profile for the application.
- Replaced YouTube physical-monitor fullscreen with window fullscreen.
- Added window-fullscreen control through YouTube's fullscreen button, the `F` key and video double-click.
- Added `Esc` to return to normal YouTube browsing.

## 0.1.1

- Fixed YouTube player Error 153 by serving the embedded player from a loopback HTTP page.
- Added an HTTP referrer plus matching `origin` and `widget_referrer` parameters.
- Kept the video inside a clean, resizable Brave app window.
- Simplified the GitHub-hosted build and release workflow.

## 0.1.0

- Initial Windows release.
