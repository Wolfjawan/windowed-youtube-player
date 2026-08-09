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

          if (isYouTube && window.top === window.self) installYouTube();
          else installGeneric();

          function installYouTube() {
            const rootClass = 'wsp-youtube-window-fullscreen';
            const targetClass = 'wsp-youtube-window-target';
            const maximumZ = '2147483647';
            let state = null;

            const nativeRequestFullscreen = Element.prototype.requestFullscreen;
            const nativeExitFullscreen = Document.prototype.exitFullscreen;
            const nativeFullscreenElement = Object.getOwnPropertyDescriptor(
              Document.prototype, 'fullscreenElement');
            const nativeFullscreenEnabled = Object.getOwnPropertyDescriptor(
              Document.prototype, 'fullscreenEnabled');
            const nativeWebkitFullscreenElement = Object.getOwnPropertyDescriptor(
              Document.prototype, 'webkitFullscreenElement');

            const style = document.createElement('style');
            style.textContent = `
              html.${rootClass}, html.${rootClass} body {
                width: 100% !important;
                height: 100% !important;
                margin: 0 !important;
                overflow: hidden !important;
                background: #000 !important;
              }
              .${targetClass} {
                position: fixed !important;
                inset: 0 !important;
                left: 0 !important;
                top: 0 !important;
                right: 0 !important;
                bottom: 0 !important;
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
                z-index: ${maximumZ} !important;
                box-sizing: border-box !important;
                background: #000 !important;
                overflow: hidden !important;
                visibility: visible !important;
                opacity: 1 !important;
                isolation: isolate !important;
              }
              .${targetClass} .html5-video-container,
              .${targetClass} .ytp-player-content {
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
                overflow: hidden !important;
                background: #000 !important;
              }
              .${targetClass} video.html5-main-video {
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
              .${targetClass} .ytp-chrome-bottom {
                left: 12px !important;
                width: calc(100% - 24px) !important;
              }
            `;
            (document.head || document.documentElement).appendChild(style);

            const playerElement = () => document.querySelector(
              'ytd-watch-flexy #movie_player, ytd-watch-flexy .html5-video-player, #movie_player, .html5-video-player'
            );
            const isWatchPage = () => location.pathname === '/watch'
              || location.pathname.startsWith('/live/')
              || location.pathname.startsWith('/shorts/');
            const isEditable = target => target instanceof Element && Boolean(
              target.closest('input, textarea, select, [contenteditable="true"]'));

            function saveStyle(element) {
              return { element, style: element.getAttribute('style') };
            }

            function restoreStyle(saved) {
              if (saved.style === null) saved.element.removeAttribute('style');
              else saved.element.setAttribute('style', saved.style);
            }

            function unlockAncestors(target) {
              const saved = [];
              let node = target.parentElement;
              while (node) {
                saved.push(saveStyle(node));
                node.style.setProperty('overflow', 'visible', 'important');
                node.style.setProperty('overflow-x', 'visible', 'important');
                node.style.setProperty('overflow-y', 'visible', 'important');
                node.style.setProperty('transform', 'none', 'important');
                node.style.setProperty('translate', 'none', 'important');
                node.style.setProperty('scale', 'none', 'important');
                node.style.setProperty('rotate', 'none', 'important');
                node.style.setProperty('filter', 'none', 'important');
                node.style.setProperty('perspective', 'none', 'important');
                node.style.setProperty('contain', 'none', 'important');
                node.style.setProperty('clip', 'auto', 'important');
                node.style.setProperty('clip-path', 'none', 'important');
                if (node === document.documentElement) break;
                node = node.parentElement;
              }
              return saved;
            }

            function updateButtons() {
              const label = state ? 'Exit window fullscreen (Esc)' : 'Fill this window (F)';
              document.querySelectorAll('.ytp-fullscreen-button').forEach(button => {
                button.setAttribute('title', label);
                button.setAttribute('aria-label', label);
              });
            }

            function applyLayout() {
              if (!state) return;
              const player = state.target;
              player.style.setProperty('position', 'fixed', 'important');
              player.style.setProperty('inset', '0', 'important');
              player.style.setProperty('width', `${window.innerWidth}px`, 'important');
              player.style.setProperty('height', `${window.innerHeight}px`, 'important');
              player.style.setProperty('z-index', maximumZ, 'important');
              player.style.setProperty('transform', 'none', 'important');

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
              window.dispatchEvent(new Event('resize'));
            }

            function dispatchFullscreenChange(target) {
              queueMicrotask(() => {
                try { target?.dispatchEvent(new Event('fullscreenchange', { bubbles: true })); } catch {}
              });
            }

            function enter() {
              if (state) return Promise.resolve();
              const player = playerElement();
              if (!player || !isWatchPage()) return Promise.resolve();

              state = {
                target: player,
                targetStyle: saveStyle(player),
                ancestorStyles: unlockAncestors(player),
                scrollX: window.scrollX,
                scrollY: window.scrollY
              };

              player.classList.add(targetClass);
              document.documentElement.classList.add(rootClass);
              document.body?.classList.add(rootClass);
              applyLayout();
              requestAnimationFrame(applyLayout);
              window.setTimeout(applyLayout, 80);
              window.setTimeout(applyLayout, 300);
              updateButtons();
              dispatchFullscreenChange(player);
              return Promise.resolve();
            }

            function exit() {
              if (!state) return Promise.resolve();
              const current = state;
              state = null;
              current.target.classList.remove(targetClass);
              restoreStyle(current.targetStyle);
              current.ancestorStyles.reverse().forEach(restoreStyle);
              document.documentElement.classList.remove(rootClass);
              document.body?.classList.remove(rootClass);
              window.scrollTo(current.scrollX, current.scrollY);
              window.dispatchEvent(new Event('resize'));
              updateButtons();
              dispatchFullscreenChange(current.target);
              return Promise.resolve();
            }

            function toggle() {
              return state ? exit() : enter();
            }

            function defineGetter(name, descriptor, activeValue) {
              try {
                Object.defineProperty(Document.prototype, name, {
                  configurable: true,
                  enumerable: descriptor?.enumerable ?? true,
                  get() {
                    if (state) return activeValue();
                    return descriptor?.get?.call(this) ?? null;
                  }
                });
              } catch {}
            }

            defineGetter('fullscreenElement', nativeFullscreenElement, () => state.target);
            defineGetter('webkitFullscreenElement', nativeWebkitFullscreenElement, () => state.target);
            defineGetter('fullscreenEnabled', nativeFullscreenEnabled, () => true);

            try {
              Element.prototype.requestFullscreen = function() { return enter(); };
            } catch {}
            for (const name of ['webkitRequestFullscreen', 'webkitRequestFullScreen']) {
              try { Element.prototype[name] = function() { return enter(); }; } catch {}
            }
            try {
              Document.prototype.exitFullscreen = function() {
                return state ? exit() : (nativeExitFullscreen?.call(this) || Promise.resolve());
              };
            } catch {}

            document.addEventListener('click', event => {
              const button = event.target instanceof Element
                ? event.target.closest('.ytp-fullscreen-button')
                : null;
              if (!button) return;
              event.preventDefault();
              event.stopPropagation();
              event.stopImmediatePropagation();
              toggle();
            }, true);

            document.addEventListener('dblclick', event => {
              const target = event.target instanceof Element ? event.target : null;
              if (!target?.closest('.html5-video-player') || target.closest('.ytp-chrome-controls')) return;
              event.preventDefault();
              event.stopPropagation();
              event.stopImmediatePropagation();
              toggle();
            }, true);

            document.addEventListener('keydown', event => {
              if (event.key === 'Escape' && state) {
                event.preventDefault();
                event.stopImmediatePropagation();
                exit();
                return;
              }
              if (event.key.toLowerCase() === 'f'
                && !event.ctrlKey && !event.altKey && !event.metaKey
                && !isEditable(event.target) && isWatchPage() && playerElement()) {
                event.preventDefault();
                event.stopImmediatePropagation();
                toggle();
              }
            }, true);

            document.addEventListener('fullscreenchange', event => {
              let nativeTarget = null;
              try { nativeTarget = nativeFullscreenElement?.get?.call(document) || null; } catch {}
              if (!nativeTarget || state) return;
              event.stopImmediatePropagation();
              Promise.resolve(nativeExitFullscreen?.call(document))
                .catch(() => {})
                .finally(enter);
            }, true);

            window.addEventListener('resize', () => { if (state) applyLayout(); });
            window.addEventListener('yt-navigate-start', () => { if (state) exit(); });
            window.addEventListener('yt-navigate-finish', () => window.setTimeout(updateButtons, 250));

            const observer = new MutationObserver(() => {
              updateButtons();
              if (state && !state.target.isConnected) exit();
            });
            const begin = () => {
              if (!document.documentElement) return window.setTimeout(begin, 20);
              observer.observe(document.documentElement, { childList: true, subtree: true });
              updateButtons();
            };
            begin();
          }

          function installGeneric() {
            const rootClass = 'wsp-generic-window-fullscreen';
            const targetClass = 'wsp-generic-window-target';
            const marker = '__wspWindowFullscreenV6';
            let state = null;

            const nativeRequestFullscreen = Element.prototype.requestFullscreen;
            const nativeExitFullscreen = Document.prototype.exitFullscreen;
            const nativeFullscreenElement = Object.getOwnPropertyDescriptor(
              Document.prototype, 'fullscreenElement');

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
                visibility: visible !important;
                opacity: 1 !important;
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
              if (requested instanceof HTMLIFrameElement || requested instanceof HTMLFrameElement) return requested;
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

            function notifyParent(type) {
              if (window.parent === window) return;
              try { window.parent.postMessage({ [marker]: true, type }, '*'); } catch {}
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
              } catch { return null; }
            }

            window.addEventListener('message', event => {
              if (!event.data?.[marker]) return;
              const frame = childFrameFor(event.source);
              if (!frame) return;
              if (event.data.type === 'enter-child') enter(frame);
              else if (event.data.type === 'exit-child') exit();
            }, true);

            try { Element.prototype.requestFullscreen = function() { return enter(this); }; } catch {}
            for (const name of ['webkitRequestFullscreen', 'webkitRequestFullScreen']) {
              try { Element.prototype[name] = function() { return enter(this); }; } catch {}
            }
            try {
              Document.prototype.exitFullscreen = function() {
                return state ? exit() : (nativeExitFullscreen?.call(this) || Promise.resolve());
              };
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

            document.addEventListener('fullscreenchange', event => {
              let nativeTarget = null;
              try { nativeTarget = nativeFullscreenElement?.get?.call(document) || null; } catch {}
              if (!nativeTarget || state) return;
              event.stopImmediatePropagation();
              Promise.resolve(nativeExitFullscreen?.call(document))
                .catch(() => {})
                .finally(() => enter(nativeTarget));
            }, true);

            window.__wspWindowFullscreenV6 = {
              enter, exit, toggle, isActive: () => Boolean(state), nativeRequestFullscreen
            };
          }
        })();
        """;
}
