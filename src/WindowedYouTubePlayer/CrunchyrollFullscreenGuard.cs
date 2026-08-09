namespace WindowedYouTubePlayer;

internal static class CrunchyrollFullscreenGuard
{
    public const string Source = """
        (() => {
          'use strict';

          const host = location.hostname.toLowerCase();
          const referrer = (document.referrer || '').toLowerCase();
          const isCrunchyroll = host.includes('crunchyroll') || referrer.includes('crunchyroll');
          if (!isCrunchyroll || window.__wspCrunchyrollGuardV62) return;
          window.__wspCrunchyrollGuardV62 = true;

          const selectors = [
            '[data-testid*="fullscreen" i]',
            '[data-testid*="full-screen" i]',
            '[data-t*="fullscreen" i]',
            '[class*="fullscreen" i]',
            '[class*="full-screen" i]',
            '[id*="fullscreen" i]',
            '[id*="full-screen" i]',
            '[aria-label*="fullscreen" i]',
            '[aria-label*="full screen" i]',
            '[title*="fullscreen" i]',
            '[title*="full screen" i]'
          ].join(',');

          let suppressUntil = 0;

          function fullscreenControl(event) {
            const path = typeof event.composedPath === 'function' ? event.composedPath() : [];
            for (const item of path) {
              if (!(item instanceof Element)) continue;
              if (item.matches(selectors)) return item;
              const closest = item.closest?.(selectors);
              if (closest) return closest;
            }

            const target = event.target instanceof Element ? event.target : null;
            return target?.closest(selectors) || null;
          }

          function largestVisibleFrame() {
            return Array.from(document.querySelectorAll('iframe, frame'))
              .filter(frame => {
                const rect = frame.getBoundingClientRect();
                const style = getComputedStyle(frame);
                return rect.width > 240 && rect.height > 140
                  && style.display !== 'none'
                  && style.visibility !== 'hidden';
              })
              .sort((a, b) => {
                const ar = a.getBoundingClientRect();
                const br = b.getBoundingClientRect();
                return (br.width * br.height) - (ar.width * ar.height);
              })[0] || null;
          }

          function block(event) {
            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
          }

          function toggleWindowFullscreen(control) {
            const controller = window.__wspWindowFullscreenV6;
            if (!controller) return;

            let target = control;
            const hasVisibleVideo = Array.from(document.querySelectorAll('video')).some(video => {
              const rect = video.getBoundingClientRect();
              const style = getComputedStyle(video);
              return rect.width > 160 && rect.height > 90
                && style.display !== 'none'
                && style.visibility !== 'hidden';
            });

            if (!hasVisibleVideo) {
              target = largestVisibleFrame() || control;
            }

            controller.toggle(target);
          }

          function beginGesture(event) {
            const control = fullscreenControl(event);
            if (!control) return;

            block(event);
            const now = performance.now();
            if (now < suppressUntil) return;

            suppressUntil = now + 750;
            toggleWindowFullscreen(control);
          }

          function finishGesture(event) {
            const control = fullscreenControl(event);
            if (!control) return;

            block(event);
            if (performance.now() >= suppressUntil) {
              suppressUntil = performance.now() + 500;
              toggleWindowFullscreen(control);
            }
          }

          window.addEventListener('pointerdown', beginGesture, true);
          document.addEventListener('pointerdown', beginGesture, true);
          window.addEventListener('mousedown', beginGesture, true);
          document.addEventListener('mousedown', beginGesture, true);

          for (const type of ['pointerup', 'mouseup']) {
            window.addEventListener(type, event => {
              if (fullscreenControl(event)) block(event);
            }, true);
            document.addEventListener(type, event => {
              if (fullscreenControl(event)) block(event);
            }, true);
          }

          window.addEventListener('click', finishGesture, true);
          document.addEventListener('click', finishGesture, true);
        })();
        """;
}
