namespace WindowedYouTubePlayer;

internal static class FullscreenInjection
{
    public const string Source = """
        (() => {
          'use strict';
          if (window.__windowedStreamingPlayerInstalledV3) return;
          window.__windowedStreamingPlayerInstalledV3 = true;

          const overlayId = 'wsp-window-overlay';
          const activeClass = 'wsp-window-active';
          const targetClass = 'wsp-window-target';
          const maximumZIndex = '2147483647';
          let state = null;
          let convertingNativeFullscreen = false;

          const nativeRequestFullscreen = Element.prototype.requestFullscreen;
          const nativeExitFullscreen = Document.prototype.exitFullscreen;
          const nativeFullscreenElement = Object.getOwnPropertyDescriptor(
            Document.prototype,
            'fullscreenElement');
          const nativeFullscreenEnabled = Object.getOwnPropertyDescriptor(
            Document.prototype,
            'fullscreenEnabled');
          const nativeWebkitFullscreenElement = Object.getOwnPropertyDescriptor(
            Document.prototype,
            'webkitFullscreenElement');

          const style = document.createElement('style');
          style.id = 'wsp-window-style';
          style.textContent = `
            html.${activeClass},
            html.${activeClass} body {
              overflow: hidden !important;
              background: #000 !important;
            }
            #${overlayId} {
              position: fixed !important;
              inset: 0 !important;
              width: 100vw !important;
              height: 100vh !important;
              margin: 0 !important;
              padding: 0 !important;
              z-index: 2147483646 !important;
              overflow: hidden !important;
              pointer-events: none !important;
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
              translate: none !important;
              scale: none !important;
              rotate: none !important;
              z-index: ${maximumZIndex} !important;
              box-sizing: border-box !important;
              background: #000 !important;
              visibility: visible !important;
              opacity: 1 !important;
              overflow: hidden !important;
              isolation: isolate !important;
            }
            .${targetClass} video,
            .${targetClass} canvas {
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
            .${targetClass} .html5-video-container,
            .${targetClass} .video-stream,
            .${targetClass} .ytp-player-content,
            .${targetClass} [class*="video-container" i],
            .${targetClass} [class*="videoContainer"] {
              width: 100% !important;
              height: 100% !important;
              min-width: 0 !important;
              min-height: 0 !important;
              max-width: none !important;
              max-height: none !important;
              inset: 0 !important;
              transform: none !important;
            }
            .${targetClass} .ytp-chrome-bottom {
              left: 12px !important;
              width: calc(100% - 24px) !important;
            }
          `;
          (document.head || document.documentElement).appendChild(style);

          function visibleVideo(root = document) {
            let videos = [];
            try {
              videos = Array.from(root.querySelectorAll('video'));
            } catch {}

            return videos
              .filter(video => {
                const rect = video.getBoundingClientRect();
                const computed = getComputedStyle(video);
                return rect.width > 120
                  && rect.height > 80
                  && computed.display !== 'none'
                  && computed.visibility !== 'hidden'
                  && Number.parseFloat(computed.opacity || '1') > 0;
              })
              .sort((a, b) => {
                const ar = a.getBoundingClientRect();
                const br = b.getBoundingClientRect();
                return (br.width * br.height) - (ar.width * ar.height);
              })[0] || null;
          }

          const containerSelectors = [
            '#movie_player',
            '.html5-video-player',
            '.watch-video',
            '.watch-video--player-view',
            '[data-uia="video-canvas"]',
            '[data-uia="player"]',
            '[data-testid*="player" i]',
            '[data-testid*="video" i]',
            '.webPlayerSDKContainer',
            '#velocity-player-package',
            '.video-player',
            '.video-player-wrapper',
            '.player-container',
            '[class*="videoPlayer"]',
            '[class*="VideoPlayer"]',
            '[class*="playerContainer"]',
            '[class*="PlayerContainer"]',
            'video-js',
            '.jwplayer',
            '.shaka-video-container'
          ].join(',');

          function knownContainer(video) {
            try {
              return video?.closest(containerSelectors) || null;
            } catch {
              return null;
            }
          }

          function playerScore(element, videoRect) {
            const rect = element.getBoundingClientRect();
            if (rect.width < videoRect.width * 0.85
              || rect.height < videoRect.height * 0.85
              || rect.width > window.innerWidth * 1.35
              || rect.height > window.innerHeight * 1.35) {
              return -1;
            }

            const identity = `${element.id || ''} ${element.className || ''}`.toLowerCase();
            let score = 0;
            if (identity.includes('player')) score += 8;
            if (identity.includes('video')) score += 5;
            if (element.querySelector('button, [role="button"]')) score += 4;
            if (rect.width >= videoRect.width * 0.98) score += 2;
            if (rect.height >= videoRect.height * 0.98) score += 2;
            score -= Math.abs((rect.width * rect.height) - (videoRect.width * videoRect.height))
              / Math.max(1, window.innerWidth * window.innerHeight);
            return score;
          }

          function chooseTarget(requested) {
            let root = document;
            if (requested instanceof Element) {
              try { root = requested.getRootNode() || document; } catch {}

              if (requested.matches('video')) {
                return knownContainer(requested) || requested;
              }

              const requestedContainer = requested.closest?.(containerSelectors);
              if (requestedContainer?.querySelector('video')) return requestedContainer;
              if (requested.querySelector?.('video')) return requested;
            }

            const video = visibleVideo(root) || visibleVideo(document);
            if (!video) return null;

            const known = knownContainer(video);
            if (known) return known;

            const videoRect = video.getBoundingClientRect();
            let best = video;
            let bestScore = 0;
            let node = video.parentElement;
            while (node && node !== document.body && node !== document.documentElement) {
              const score = playerScore(node, videoRect);
              if (score > bestScore) {
                best = node;
                bestScore = score;
              }
              node = node.parentElement;
            }
            return best;
          }

          function saveInlineStyle(element) {
            return { element, style: element.getAttribute('style') };
          }

          function restoreInlineStyle(saved) {
            if (saved.style === null) saved.element.removeAttribute('style');
            else saved.element.setAttribute('style', saved.style);
          }

          function unlockAncestors(target) {
            const saved = [];
            let node = target.parentElement;
            while (node) {
              saved.push(saveInlineStyle(node));
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
              node.style.setProperty('mask', 'none', 'important');
              node.style.setProperty('visibility', 'visible', 'important');
              node.style.setProperty('opacity', '1', 'important');
              if (node === document.documentElement) break;
              node = node.parentElement;
            }
            return saved;
          }

          function applyWindowLayout() {
            if (!state) return;
            const { target } = state;
            target.style.setProperty('position', 'fixed', 'important');
            target.style.setProperty('inset', '0', 'important');
            target.style.setProperty('left', '0', 'important');
            target.style.setProperty('top', '0', 'important');
            target.style.setProperty('right', '0', 'important');
            target.style.setProperty('bottom', '0', 'important');
            target.style.setProperty('width', `${window.innerWidth}px`, 'important');
            target.style.setProperty('height', `${window.innerHeight}px`, 'important');
            target.style.setProperty('z-index', maximumZIndex, 'important');
            target.style.setProperty('transform', 'none', 'important');

            target.querySelectorAll('video, canvas').forEach(media => {
              media.style.setProperty('width', '100%', 'important');
              media.style.setProperty('height', '100%', 'important');
              media.style.setProperty('left', '0', 'important');
              media.style.setProperty('top', '0', 'important');
              media.style.setProperty('transform', 'none', 'important');
              media.style.setProperty('object-fit', 'contain', 'important');
            });

            window.dispatchEvent(new Event('resize'));
          }

          function dispatchSyntheticFullscreenChange(target) {
            queueMicrotask(() => {
              try {
                target?.dispatchEvent(new Event('fullscreenchange', { bubbles: true }));
                target?.dispatchEvent(new Event('webkitfullscreenchange', { bubbles: true }));
              } catch {}
            });
          }

          function enterWindowFullscreen(requested) {
            if (state) return Promise.resolve();
            const target = chooseTarget(requested);
            if (!target || !target.isConnected) return Promise.resolve();

            const overlay = document.createElement('div');
            overlay.id = overlayId;
            document.documentElement.appendChild(overlay);

            state = {
              target,
              targetStyle: saveInlineStyle(target),
              ancestorStyles: unlockAncestors(target),
              overlay,
              scrollX: window.scrollX,
              scrollY: window.scrollY
            };

            target.classList.add(targetClass);
            document.documentElement.classList.add(activeClass);
            document.body?.classList.add(activeClass);
            applyWindowLayout();

            requestAnimationFrame(applyWindowLayout);
            window.setTimeout(applyWindowLayout, 80);
            window.setTimeout(applyWindowLayout, 300);
            window.setTimeout(applyWindowLayout, 900);
            dispatchSyntheticFullscreenChange(target);
            return Promise.resolve();
          }

          function exitWindowFullscreen() {
            if (!state) return Promise.resolve();
            const current = state;
            state = null;

            current.target.classList.remove(targetClass);
            restoreInlineStyle(current.targetStyle);
            current.ancestorStyles.reverse().forEach(restoreInlineStyle);
            current.overlay.remove();
            document.documentElement.classList.remove(activeClass);
            document.body?.classList.remove(activeClass);
            window.scrollTo(current.scrollX, current.scrollY);
            window.dispatchEvent(new Event('resize'));
            dispatchSyntheticFullscreenChange(current.target);
            return Promise.resolve();
          }

          const toggleWindowFullscreen = requested => state
            ? exitWindowFullscreen()
            : enterWindowFullscreen(requested);

          function defineDocumentGetter(name, nativeDescriptor, valueWhenActive) {
            try {
              Object.defineProperty(Document.prototype, name, {
                configurable: true,
                enumerable: nativeDescriptor?.enumerable ?? true,
                get() {
                  if (state) return valueWhenActive();
                  return nativeDescriptor?.get?.call(this) ?? null;
                }
              });
            } catch {}
          }

          defineDocumentGetter('fullscreenElement', nativeFullscreenElement, () => state.target);
          defineDocumentGetter('webkitFullscreenElement', nativeWebkitFullscreenElement, () => state.target);
          defineDocumentGetter('fullscreenEnabled', nativeFullscreenEnabled, () => true);

          Element.prototype.requestFullscreen = function() {
            return enterWindowFullscreen(this);
          };

          ['webkitRequestFullscreen', 'webkitRequestFullScreen', 'mozRequestFullScreen', 'msRequestFullscreen']
            .forEach(name => {
              try {
                Element.prototype[name] = function() {
                  return enterWindowFullscreen(this);
                };
              } catch {}
            });

          Document.prototype.exitFullscreen = function() {
            return state
              ? exitWindowFullscreen()
              : nativeExitFullscreen?.call(this) || Promise.resolve();
          };

          function nativeFullscreenTarget() {
            try {
              return nativeFullscreenElement?.get?.call(document)
                || nativeWebkitFullscreenElement?.get?.call(document)
                || null;
            } catch {
              return null;
            }
          }

          document.addEventListener('fullscreenchange', event => {
            const nativeTarget = nativeFullscreenTarget();
            if (!nativeTarget || state || convertingNativeFullscreen) return;

            event.stopImmediatePropagation();
            convertingNativeFullscreen = true;
            Promise.resolve(nativeExitFullscreen?.call(document))
              .catch(() => {})
              .finally(() => {
                convertingNativeFullscreen = false;
                enterWindowFullscreen(nativeTarget);
              });
          }, true);

          function fullscreenControlFromEvent(event) {
            const selectors = [
              '.ytp-fullscreen-button',
              '[data-uia*="full-screen" i]',
              '[data-testid*="fullscreen" i]',
              '[data-testid*="full-screen" i]',
              '[class*="fullscreen" i]',
              '[class*="full-screen" i]',
              '[id*="fullscreen" i]',
              '[id*="full-screen" i]'
            ].join(',');

            for (const node of event.composedPath?.() || []) {
              if (!(node instanceof Element)) continue;
              const candidate = node.closest?.('button, [role="button"], input, a, div[tabindex]') || node;
              if (candidate.matches?.(selectors)) return candidate;

              const label = [
                candidate.getAttribute?.('aria-label'),
                candidate.getAttribute?.('title'),
                candidate.getAttribute?.('data-tooltip-text'),
                candidate.textContent
              ].filter(Boolean).join(' ').toLowerCase().replace(/\s+/g, ' ');

              if (label.includes('fullscreen')
                || label.includes('full screen')
                || label.includes('exit full screen')) {
                return candidate;
              }
            }
            return null;
          }

          document.addEventListener('click', event => {
            const control = fullscreenControlFromEvent(event);
            if (!control) return;
            const root = control.getRootNode?.() || document;
            const video = visibleVideo(root) || visibleVideo(document);
            if (!video) return;

            event.preventDefault();
            event.stopImmediatePropagation();
            toggleWindowFullscreen(knownContainer(video) || video);
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
            const root = event.target.getRootNode?.() || document;
            const video = event.target.closest('video') || visibleVideo(root);
            if (!video) return;
            event.preventDefault();
            event.stopImmediatePropagation();
            toggleWindowFullscreen(knownContainer(video) || video);
          }, true);

          window.addEventListener('resize', () => {
            if (state) applyWindowLayout();
          });

          new MutationObserver(() => {
            if (state && !state.target.isConnected) exitWindowFullscreen();
          }).observe(document.documentElement, { childList: true, subtree: true });

          window.__wspWindowFullscreen = {
            enter: enterWindowFullscreen,
            exit: exitWindowFullscreen,
            toggle: toggleWindowFullscreen,
            isActive: () => Boolean(state),
            nativeRequestFullscreen
          };
        })();
        """;
}
