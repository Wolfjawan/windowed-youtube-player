using System.Text;

namespace WindowedYouTubePlayer;

internal static class WindowFullscreenExtension
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    public static void Install(string extensionDirectory)
    {
        Directory.CreateDirectory(extensionDirectory);
        File.WriteAllText(Path.Combine(extensionDirectory, "manifest.json"), Manifest, Utf8WithoutBom);
        File.WriteAllText(Path.Combine(extensionDirectory, "youtube.css"), YouTubeCss, Utf8WithoutBom);
        File.WriteAllText(Path.Combine(extensionDirectory, "youtube.js"), YouTubeScript, Utf8WithoutBom);
        File.WriteAllText(Path.Combine(extensionDirectory, "generic.css"), GenericCss, Utf8WithoutBom);
        File.WriteAllText(Path.Combine(extensionDirectory, "generic.js"), GenericScript, Utf8WithoutBom);
    }

    private const string Manifest = """
        {
          "manifest_version": 3,
          "name": "Windowed Streaming Player Fullscreen",
          "version": "0.6.0",
          "description": "Makes streaming-player fullscreen fill only the current Chromium app window.",
          "content_scripts": [
            {
              "matches": [
                "https://www.youtube.com/*",
                "https://youtube.com/*",
                "https://m.youtube.com/*"
              ],
              "css": ["youtube.css"],
              "js": ["youtube.js"],
              "run_at": "document_start",
              "all_frames": false
            },
            {
              "matches": ["http://*/*", "https://*/*"],
              "exclude_matches": [
                "https://www.youtube.com/*",
                "https://youtube.com/*",
                "https://m.youtube.com/*"
              ],
              "css": ["generic.css"],
              "js": ["generic.js"],
              "run_at": "document_start",
              "all_frames": true,
              "match_about_blank": true,
              "match_origin_as_fallback": true,
              "world": "MAIN"
            }
          ]
        }
        """;

    // This is intentionally based on the last known-good pre-installer implementation (v0.3.0).
    private const string YouTubeCss = """
        html.wyp-window-fullscreen,
        html.wyp-window-fullscreen body {
          width: 100% !important;
          height: 100% !important;
          margin: 0 !important;
          overflow: hidden !important;
          background: #000 !important;
        }

        #wyp-player-overlay {
          position: fixed !important;
          inset: 0 !important;
          z-index: 2147483647 !important;
          width: 100vw !important;
          height: 100vh !important;
          margin: 0 !important;
          padding: 0 !important;
          overflow: hidden !important;
          background: #000 !important;
          isolation: isolate !important;
        }

        #wyp-player-overlay > #movie_player,
        #wyp-player-overlay > .html5-video-player {
          position: absolute !important;
          inset: 0 !important;
          z-index: 1 !important;
          box-sizing: border-box !important;
          width: 100% !important;
          height: 100% !important;
          min-width: 0 !important;
          min-height: 0 !important;
          max-width: none !important;
          max-height: none !important;
          margin: 0 !important;
          padding: 0 !important;
          transform: none !important;
          background: #000 !important;
        }

        #wyp-player-overlay .html5-video-container {
          position: absolute !important;
          inset: 0 !important;
          width: 100% !important;
          height: 100% !important;
          max-width: none !important;
          max-height: none !important;
          margin: 0 !important;
          padding: 0 !important;
          transform: none !important;
          overflow: hidden !important;
          background: #000 !important;
        }

        #wyp-player-overlay video.html5-main-video {
          position: absolute !important;
          inset: 0 !important;
          left: 0 !important;
          top: 0 !important;
          width: 100% !important;
          height: 100% !important;
          max-width: none !important;
          max-height: none !important;
          margin: 0 !important;
          transform: none !important;
          object-fit: contain !important;
          background: #000 !important;
        }

        #wyp-player-overlay .ytp-chrome-bottom {
          left: 12px !important;
          width: calc(100% - 24px) !important;
        }

        #wyp-player-overlay .ytp-gradient-bottom,
        #wyp-player-overlay .ytp-gradient-top {
          width: 100% !important;
        }
        """;

    private const string YouTubeScript = """
        (() => {
          'use strict';
          if (window.top !== window.self || window.__wspYouTubeV6) return;
          window.__wspYouTubeV6 = true;

          const rootClass = 'wyp-window-fullscreen';
          const overlayId = 'wyp-player-overlay';
          let resizeFrame = 0;
          let originalParent = null;
          let originalNextSibling = null;
          let playerStyleSnapshot = null;
          let videoStyleSnapshot = null;

          const playerElement = () => document.querySelector(
            'ytd-watch-flexy #movie_player, ytd-watch-flexy .html5-video-player, #movie_player, .html5-video-player'
          );
          const isWatchPage = () => location.pathname === '/watch'
            || location.pathname.startsWith('/live/')
            || location.pathname.startsWith('/shorts/');
          const isWindowFullscreen = () => document.documentElement.classList.contains(rootClass);
          const isEditableTarget = target => target instanceof Element && (
            target.matches('input, textarea, select, [contenteditable="true"]')
            || Boolean(target.closest('input, textarea, select, [contenteditable="true"]'))
          );

          function updateFullscreenButtons() {
            const label = isWindowFullscreen()
              ? 'Exit window fullscreen (Esc)'
              : 'Fill this window (F)';
            document.querySelectorAll('.ytp-fullscreen-button').forEach(button => {
              button.setAttribute('title', label);
              button.setAttribute('aria-label', label);
            });
          }

          function snapshotStyle(element) {
            return element?.hasAttribute('style') ? element.getAttribute('style') : null;
          }

          function restoreStyle(element, snapshot) {
            if (!element) return;
            if (snapshot === null) element.removeAttribute('style');
            else element.setAttribute('style', snapshot);
          }

          function refreshPlayerSize() {
            window.cancelAnimationFrame(resizeFrame);
            resizeFrame = window.requestAnimationFrame(() => {
              if (!isWindowFullscreen()) return;
              const overlay = document.getElementById(overlayId);
              const player = overlay?.querySelector('#movie_player, .html5-video-player');
              if (!player) return;

              player.style.setProperty('width', `${window.innerWidth}px`, 'important');
              player.style.setProperty('height', `${window.innerHeight}px`, 'important');
              player.style.setProperty('left', '0', 'important');
              player.style.setProperty('top', '0', 'important');

              const video = player.querySelector('video.html5-main-video');
              if (video) {
                video.style.setProperty('width', '100%', 'important');
                video.style.setProperty('height', '100%', 'important');
                video.style.setProperty('left', '0', 'important');
                video.style.setProperty('top', '0', 'important');
                video.style.setProperty('transform', 'none', 'important');
              }

              if (typeof player.setSize === 'function') {
                try { player.setSize(window.innerWidth, window.innerHeight); } catch {}
              }
            });
          }

          function enterWindowFullscreen() {
            const player = playerElement();
            if (!document.body || !isWatchPage() || !player) return Promise.resolve();
            if (document.getElementById(overlayId)) {
              refreshPlayerSize();
              return Promise.resolve();
            }

            originalParent = player.parentNode;
            originalNextSibling = player.nextSibling;
            playerStyleSnapshot = snapshotStyle(player);
            videoStyleSnapshot = snapshotStyle(player.querySelector('video.html5-main-video'));

            const overlay = document.createElement('div');
            overlay.id = overlayId;
            document.body.appendChild(overlay);
            overlay.appendChild(player);

            document.documentElement.classList.add(rootClass);
            document.body.classList.add(rootClass);
            updateFullscreenButtons();
            refreshPlayerSize();
            window.setTimeout(refreshPlayerSize, 80);
            window.setTimeout(refreshPlayerSize, 300);
            window.dispatchEvent(new Event('resize'));
            return Promise.resolve();
          }

          function exitWindowFullscreen() {
            const overlay = document.getElementById(overlayId);
            const player = overlay?.querySelector('#movie_player, .html5-video-player');
            const video = player?.querySelector('video.html5-main-video');

            document.documentElement.classList.remove(rootClass);
            document.body?.classList.remove(rootClass);

            if (player && originalParent) {
              if (originalNextSibling && originalNextSibling.parentNode === originalParent) {
                originalParent.insertBefore(player, originalNextSibling);
              } else {
                originalParent.appendChild(player);
              }
            }

            restoreStyle(player, playerStyleSnapshot);
            restoreStyle(video, videoStyleSnapshot);
            overlay?.remove();
            originalParent = null;
            originalNextSibling = null;
            playerStyleSnapshot = null;
            videoStyleSnapshot = null;
            updateFullscreenButtons();
            window.setTimeout(() => window.dispatchEvent(new Event('resize')), 0);
            return Promise.resolve();
          }

          const toggleWindowFullscreen = () => isWindowFullscreen()
            ? exitWindowFullscreen()
            : enterWindowFullscreen();

          document.addEventListener('click', event => {
            const button = event.target instanceof Element
              ? event.target.closest('.ytp-fullscreen-button')
              : null;
            if (!button) return;
            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
            toggleWindowFullscreen();
          }, true);

          document.addEventListener('dblclick', event => {
            const target = event.target instanceof Element ? event.target : null;
            if (!target?.closest('.html5-video-player') || target.closest('.ytp-chrome-controls')) return;
            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
            toggleWindowFullscreen();
          }, true);

          document.addEventListener('keydown', event => {
            if (event.key === 'Escape' && isWindowFullscreen()) {
              event.preventDefault();
              event.stopPropagation();
              event.stopImmediatePropagation();
              exitWindowFullscreen();
              return;
            }

            if (event.key.toLowerCase() === 'f'
              && !event.ctrlKey && !event.altKey && !event.metaKey
              && !isEditableTarget(event.target) && isWatchPage() && playerElement()) {
              event.preventDefault();
              event.stopPropagation();
              event.stopImmediatePropagation();
              toggleWindowFullscreen();
            }
          }, true);

          document.addEventListener('fullscreenchange', () => {
            if (!document.fullscreenElement) return;
            document.exitFullscreen().catch(() => {}).finally(enterWindowFullscreen);
          }, true);

          window.addEventListener('resize', () => {
            if (isWindowFullscreen()) refreshPlayerSize();
          });

          window.addEventListener('yt-navigate-start', () => {
            if (isWindowFullscreen()) exitWindowFullscreen();
          });

          window.addEventListener('yt-navigate-finish', () => {
            window.setTimeout(updateFullscreenButtons, 250);
          });

          const observer = new MutationObserver(() => {
            updateFullscreenButtons();
            if (isWindowFullscreen()) refreshPlayerSize();
          });

          const beginObserving = () => {
            if (!document.documentElement) {
              window.setTimeout(beginObserving, 20);
              return;
            }
            observer.observe(document.documentElement, { childList: true, subtree: true });
            updateFullscreenButtons();
          };

          beginObserving();
        })();
        """;

    private const string GenericCss = """
        html.wsp-generic-fullscreen,
        html.wsp-generic-fullscreen body {
          overflow: hidden !important;
          background: #000 !important;
        }

        .wsp-generic-fullscreen-target {
          position: fixed !important;
          inset: 0 !important;
          width: 100vw !important;
          height: 100vh !important;
          min-width: 0 !important;
          min-height: 0 !important;
          max-width: none !important;
          max-height: none !important;
          margin: 0 !important;
          padding: 0 !important;
          border: 0 !important;
          transform: none !important;
          z-index: 2147483647 !important;
          background: #000 !important;
          overflow: hidden !important;
        }

        .wsp-generic-fullscreen-target video,
        .wsp-generic-fullscreen-target canvas {
          width: 100% !important;
          height: 100% !important;
          max-width: none !important;
          max-height: none !important;
          object-fit: contain !important;
          background: #000 !important;
        }
        """;

    private const string GenericScript = """
        (() => {
          'use strict';
          if (window.__wspGenericFullscreenV6) return;
          window.__wspGenericFullscreenV6 = true;

          const targetClass = 'wsp-generic-fullscreen-target';
          const rootClass = 'wsp-generic-fullscreen';
          const messageMarker = '__wspWindowFullscreenV6';
          const originalRequestFullscreen = Element.prototype.requestFullscreen;
          const originalExitFullscreen = Document.prototype.exitFullscreen;
          let state = null;

          function visibleVideo() {
            return Array.from(document.querySelectorAll('video'))
              .filter(video => {
                const rect = video.getBoundingClientRect();
                const style = getComputedStyle(video);
                return rect.width > 120 && rect.height > 80
                  && style.display !== 'none'
                  && style.visibility !== 'hidden';
              })
              .sort((a, b) => {
                const ar = a.getBoundingClientRect();
                const br = b.getBoundingClientRect();
                return (br.width * br.height) - (ar.width * ar.height);
              })[0] || null;
          }

          function chooseTarget(requested) {
            if (requested instanceof Element) {
              if (requested.matches('video')) return requested.parentElement || requested;
              if (requested.querySelector?.('video')) return requested;
              const container = requested.closest?.(
                '[data-testid*="player" i], [data-uia*="player" i], .video-player, .player-container, [class*="player" i]');
              if (container?.querySelector('video')) return container;
            }

            const video = visibleVideo();
            if (!video) return null;
            return video.closest(
              '[data-testid*="player" i], [data-uia*="player" i], .video-player, .player-container, [class*="player" i]')
              || video.parentElement
              || video;
          }

          function saveStyle(element) {
            return element.hasAttribute('style') ? element.getAttribute('style') : null;
          }

          function restoreStyle(element, value) {
            if (value === null) element.removeAttribute('style');
            else element.setAttribute('style', value);
          }

          function notifyParent(type) {
            if (window.parent === window) return;
            try { window.parent.postMessage({ [messageMarker]: true, type }, '*'); } catch {}
          }

          function enter(requested) {
            if (state) return Promise.resolve();
            const target = chooseTarget(requested);
            if (!target || !target.isConnected) return Promise.resolve();

            state = { target, style: saveStyle(target) };
            target.classList.add(targetClass);
            document.documentElement.classList.add(rootClass);
            document.body?.classList.add(rootClass);
            window.dispatchEvent(new Event('resize'));
            notifyParent('enter-child');
            return Promise.resolve();
          }

          function exit() {
            if (!state) return Promise.resolve();
            const current = state;
            state = null;
            current.target.classList.remove(targetClass);
            restoreStyle(current.target, current.style);
            document.documentElement.classList.remove(rootClass);
            document.body?.classList.remove(rootClass);
            window.dispatchEvent(new Event('resize'));
            notifyParent('exit-child');
            return Promise.resolve();
          }

          function toggle(requested) {
            return state ? exit() : enter(requested);
          }

          function childFrameFor(source) {
            try {
              return Array.from(document.querySelectorAll('iframe, frame'))
                .find(frame => frame.contentWindow === source) || null;
            } catch {
              return null;
            }
          }

          window.addEventListener('message', event => {
            if (!event.data?.[messageMarker]) return;
            const frame = childFrameFor(event.source);
            if (!frame) return;
            if (event.data.type === 'enter-child') enter(frame);
            if (event.data.type === 'exit-child') exit();
          }, true);

          try {
            Object.defineProperty(Element.prototype, 'requestFullscreen', {
              configurable: true,
              writable: true,
              value: function() { return enter(this); }
            });
          } catch {
            Element.prototype.requestFullscreen = function() { return enter(this); };
          }

          for (const name of ['webkitRequestFullscreen', 'webkitRequestFullScreen']) {
            try { Element.prototype[name] = function() { return enter(this); }; } catch {}
          }

          try {
            Document.prototype.exitFullscreen = function() {
              return state ? exit() : Promise.resolve();
            };
          } catch {}

          const fullscreenSelectors = [
            '[data-uia*="full-screen" i]',
            '[data-uia*="fullscreen" i]',
            '[data-testid*="fullscreen" i]',
            '[data-testid*="full-screen" i]',
            '[class*="fullscreen" i]',
            '[class*="full-screen" i]',
            '[id*="fullscreen" i]',
            '[id*="full-screen" i]',
            '[aria-label*="fullscreen" i]',
            '[aria-label*="full screen" i]',
            '[title*="fullscreen" i]',
            '[title*="full screen" i]'
          ].join(',');

          document.addEventListener('click', event => {
            const node = event.target instanceof Element ? event.target : null;
            const control = node?.closest(fullscreenSelectors);
            if (!control || !visibleVideo()) return;
            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
            toggle(control);
          }, true);

          document.addEventListener('dblclick', event => {
            const node = event.target instanceof Element ? event.target : null;
            const video = node?.closest('video') || visibleVideo();
            if (!video) return;
            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
            toggle(video);
          }, true);

          document.addEventListener('keydown', event => {
            const editable = event.target instanceof Element
              && Boolean(event.target.closest('input, textarea, select, [contenteditable="true"]'));
            if (event.key === 'Escape' && state) {
              event.preventDefault();
              event.stopImmediatePropagation();
              exit();
              return;
            }
            if (event.key.toLowerCase() === 'f'
              && !event.ctrlKey && !event.altKey && !event.metaKey
              && !editable && visibleVideo()) {
              event.preventDefault();
              event.stopImmediatePropagation();
              toggle();
            }
          }, true);

          document.addEventListener('fullscreenchange', () => {
            if (!document.fullscreenElement) return;
            const nativeTarget = document.fullscreenElement;
            Promise.resolve(originalExitFullscreen?.call(document))
              .catch(() => {})
              .finally(() => enter(nativeTarget));
          }, true);

          window.__wspWindowFullscreenV6 = { enter, exit, toggle, isActive: () => Boolean(state), originalRequestFullscreen };
        })();
        """;
}
