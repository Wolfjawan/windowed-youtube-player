namespace WindowedYouTubePlayer;

internal static class FullscreenInjection
{
    public const string Source = """
        (() => {
          'use strict';
          if (window.__windowedStreamingPlayerInstalled) return;
          window.__windowedStreamingPlayerInstalled = true;

          const overlayId = 'wsp-window-overlay';
          const activeClass = 'wsp-window-active';
          const targetClass = 'wsp-window-target';
          let state = null;

          const style = document.createElement('style');
          style.id = 'wsp-window-style';
          style.textContent = `
            html.${activeClass}, html.${activeClass} body {
              overflow: hidden !important;
              background: #000 !important;
            }
            #${overlayId} {
              position: fixed !important;
              inset: 0 !important;
              width: 100vw !important;
              height: 100vh !important;
              min-width: 0 !important;
              min-height: 0 !important;
              margin: 0 !important;
              padding: 0 !important;
              z-index: 2147483647 !important;
              overflow: hidden !important;
              background: #000 !important;
              display: block !important;
              transform: none !important;
              contain: layout paint size !important;
            }
            #${overlayId} > .${targetClass} {
              position: absolute !important;
              inset: 0 !important;
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
            #${overlayId} video {
              position: absolute !important;
              inset: 0 !important;
              width: 100% !important;
              height: 100% !important;
              min-width: 0 !important;
              min-height: 0 !important;
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
          `;
          (document.head || document.documentElement).appendChild(style);

          const visibleVideo = () => Array.from(document.querySelectorAll('video'))
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

          const knownContainer = video => video?.closest([
            '#movie_player',
            '.html5-video-player',
            '.watch-video',
            '.watch-video--player-view',
            '[data-uia="video-canvas"]',
            '[data-uia="player"]',
            '.webPlayerSDKContainer',
            '#velocity-player-package',
            '.video-player',
            '[class*="videoPlayer"]',
            '[class*="VideoPlayer"]',
            'video-js',
            '.jwplayer',
            '.shaka-video-container'
          ].join(','));

          function chooseTarget(requested) {
            if (requested instanceof Element
              && requested !== document.documentElement
              && requested !== document.body
              && (requested.matches('video') || requested.querySelector('video'))) {
              return requested.matches('video') ? knownContainer(requested) || requested : requested;
            }

            const video = visibleVideo();
            if (!video) return null;
            const known = knownContainer(video);
            if (known) return known;

            let best = video;
            let node = video.parentElement;
            const videoRect = video.getBoundingClientRect();
            while (node && node !== document.body && node !== document.documentElement) {
              const rect = node.getBoundingClientRect();
              const containsOnlyReasonableArea = rect.width <= window.innerWidth * 1.15
                && rect.height <= window.innerHeight * 1.15;
              const containsVideo = rect.width >= videoRect.width * 0.9
                && rect.height >= videoRect.height * 0.9;
              if (containsOnlyReasonableArea && containsVideo) best = node;
              node = node.parentElement;
            }
            return best;
          }

          function resizeTarget() {
            if (!state) return;
            const { target } = state;
            target.style.setProperty('width', `${window.innerWidth}px`, 'important');
            target.style.setProperty('height', `${window.innerHeight}px`, 'important');
            target.style.setProperty('left', '0', 'important');
            target.style.setProperty('top', '0', 'important');
            target.style.setProperty('transform', 'none', 'important');
            target.querySelectorAll('video').forEach(video => {
              video.style.setProperty('width', '100%', 'important');
              video.style.setProperty('height', '100%', 'important');
              video.style.setProperty('left', '0', 'important');
              video.style.setProperty('top', '0', 'important');
              video.style.setProperty('transform', 'none', 'important');
              video.style.setProperty('object-fit', 'contain', 'important');
            });
            if (typeof target.setSize === 'function') {
              try { target.setSize(window.innerWidth, window.innerHeight); } catch {}
            }
          }

          function enterWindowFullscreen(requested) {
            if (state) return Promise.resolve();
            const target = chooseTarget(requested);
            if (!target || !target.parentNode) return Promise.resolve();

            const placeholder = document.createComment('windowed-streaming-player-placeholder');
            target.parentNode.insertBefore(placeholder, target);
            const originalStyle = target.getAttribute('style');
            const overlay = document.createElement('div');
            overlay.id = overlayId;
            document.documentElement.appendChild(overlay);
            overlay.appendChild(target);
            target.classList.add(targetClass);
            document.documentElement.classList.add(activeClass);
            document.body?.classList.add(activeClass);
            state = { target, placeholder, originalStyle, overlay };

            resizeTarget();
            window.setTimeout(resizeTarget, 60);
            window.setTimeout(resizeTarget, 250);
            window.setTimeout(resizeTarget, 700);
            return Promise.resolve();
          }

          function exitWindowFullscreen() {
            if (!state) return Promise.resolve();
            const current = state;
            state = null;

            current.target.classList.remove(targetClass);
            if (current.originalStyle === null) current.target.removeAttribute('style');
            else current.target.setAttribute('style', current.originalStyle);

            if (current.placeholder.parentNode) {
              current.placeholder.parentNode.replaceChild(current.target, current.placeholder);
            }
            current.overlay.remove();
            document.documentElement.classList.remove(activeClass);
            document.body?.classList.remove(activeClass);
            window.dispatchEvent(new Event('resize'));
            return Promise.resolve();
          }

          const toggleWindowFullscreen = requested => state
            ? exitWindowFullscreen()
            : enterWindowFullscreen(requested);

          Element.prototype.requestFullscreen = function() {
            return enterWindowFullscreen(this);
          };

          ['webkitRequestFullscreen', 'webkitRequestFullScreen', 'mozRequestFullScreen', 'msRequestFullscreen']
            .forEach(name => {
              if (typeof Element.prototype[name] === 'function') {
                Element.prototype[name] = function() {
                  return enterWindowFullscreen(this);
                };
              }
            });

          const nativeExitFullscreen = Document.prototype.exitFullscreen;
          Document.prototype.exitFullscreen = function() {
            return state ? exitWindowFullscreen() : nativeExitFullscreen?.call(this) || Promise.resolve();
          };

          document.addEventListener('fullscreenchange', () => {
            const nativeTarget = document.fullscreenElement;
            if (!nativeTarget || state) return;
            nativeExitFullscreen?.call(document)
              .catch(() => {})
              .finally(() => enterWindowFullscreen(nativeTarget));
          }, true);

          document.addEventListener('keydown', event => {
            const editable = event.target instanceof Element
              && Boolean(event.target.closest('input, textarea, select, [contenteditable="true"]'));

            if (event.key === 'Escape' && state) {
              event.preventDefault();
              event.stopImmediatePropagation();
              exitWindowFullscreen();
              return;
            }

            if (event.key.toLowerCase() === 'f'
              && !event.ctrlKey && !event.altKey && !event.metaKey
              && !editable && visibleVideo()) {
              event.preventDefault();
              event.stopImmediatePropagation();
              toggleWindowFullscreen();
            }
          }, true);

          document.addEventListener('dblclick', event => {
            if (!(event.target instanceof Element)) return;
            const video = event.target.closest('video')
              || event.target.closest('[class*="player"], [id*="player"]')?.querySelector('video');
            if (!video) return;
            event.preventDefault();
            event.stopImmediatePropagation();
            toggleWindowFullscreen(knownContainer(video) || video);
          }, true);

          window.addEventListener('resize', () => {
            if (state) resizeTarget();
          });

          new MutationObserver(() => {
            if (state && !state.target.isConnected) exitWindowFullscreen();
          }).observe(document.documentElement, { childList: true, subtree: true });
        })();
        """;
}
