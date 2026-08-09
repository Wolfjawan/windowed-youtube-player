namespace WindowedYouTubePlayer;

internal static class WindowFullscreenRuntime
{
    public const string Source = """
        (() => {
          'use strict';
          if (window.__wspRuntimeV6) return;
          window.__wspRuntimeV6 = true;

          const host = location.hostname.toLowerCase();
          const isYouTube = host === 'youtube.com'
            || host.endsWith('.youtube.com')
            || host === 'youtu.be';

          if (isYouTube && window.top === window.self) {
            installYouTube();
            return;
          }

          installGeneric();

          function installYouTube() {
            const rootClass = 'wyp-window-fullscreen';
            const overlayId = 'wyp-player-overlay';
            let resizeFrame = 0;
            let originalParent = null;
            let originalNextSibling = null;
            let playerStyleSnapshot = null;
            let videoStyleSnapshot = null;

            const style = document.createElement('style');
            style.textContent = `
              html.${rootClass}, html.${rootClass} body {
                width: 100% !important;
                height: 100% !important;
                margin: 0 !important;
                overflow: hidden !important;
                background: #000 !important;
              }
              #${overlayId} {
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
              #${overlayId} > #movie_player,
              #${overlayId} > .html5-video-player {
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
              #${overlayId} .html5-video-container {
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
              #${overlayId} video.html5-main-video {
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
              #${overlayId} .ytp-chrome-bottom {
                left: 12px !important;
                width: calc(100% - 24px) !important;
              }
              #${overlayId} .ytp-gradient-bottom,
              #${overlayId} .ytp-gradient-top {
                width: 100% !important;
              }
            `;
            (document.head || document.documentElement).appendChild(style);

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
              const target = document.fullscreenElement;
              document.exitFullscreen().catch(() => {}).finally(() => {
                if (!isWindowFullscreen()) enterWindowFullscreen(target);
              });
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
          }

          function installGeneric() {
            const rootClass = 'wsp-generic-window-fullscreen';
            const targetClass = 'wsp-generic-window-target';
            let state = null;

            const style = document.createElement('style');
            style.textContent = `
              html.${rootClass}, html.${rootClass} body {
                overflow: hidden !important;
                background: #000 !important;
              }
              .${targetClass} {
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
              .${targetClass} video,
              .${targetClass} canvas {
                width: 100% !important;
                height: 100% !important;
                max-width: none !important;
                max-height: none !important;
                object-fit: contain !important;
                background: #000 !important;
              }
            `;
            (document.head || document.documentElement).appendChild(style);

            function visibleVideo() {
              return Array.from(document.querySelectorAll('video'))
                .filter(video => {
                  const rect = video.getBoundingClientRect();
                  const computed = getComputedStyle(video);
                  return rect.width > 120 && rect.height > 80
                    && computed.display !== 'none'
                    && computed.visibility !== 'hidden';
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
              }

              const video = visibleVideo();
              if (!video) return null;
              return video.closest([
                '[data-testid*="player" i]',
                '[data-uia*="player" i]',
                '.video-player',
                '.player-container',
                '[class*="videoPlayer"]',
                '[class*="playerContainer"]',
                '.jwplayer',
                '.shaka-video-container'
              ].join(',')) || video.parentElement || video;
            }

            function saveStyle(element) {
              return element.hasAttribute('style') ? element.getAttribute('style') : null;
            }

            function restoreStyle(element, value) {
              if (value === null) element.removeAttribute('style');
              else element.setAttribute('style', value);
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
              return Promise.resolve();
            }

            function toggle(requested) {
              return state ? exit() : enter(requested);
            }

            try {
              Element.prototype.requestFullscreen = function() { return enter(this); };
            } catch {}
            for (const name of ['webkitRequestFullscreen', 'webkitRequestFullScreen']) {
              try { Element.prototype[name] = function() { return enter(this); }; } catch {}
            }
            try {
              Document.prototype.exitFullscreen = function() { return state ? exit() : Promise.resolve(); };
            } catch {}

            const selectors = [
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
              const control = node?.closest(selectors);
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
              const target = document.fullscreenElement;
              Promise.resolve(document.exitFullscreen?.())
                .catch(() => {})
                .finally(() => enter(target));
            }, true);
          }
        })();
        """;
}
