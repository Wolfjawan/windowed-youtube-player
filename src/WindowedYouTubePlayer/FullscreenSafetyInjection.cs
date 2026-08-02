namespace WindowedYouTubePlayer;

internal static class FullscreenSafetyInjection
{
    public const string Source = """
        (() => {
          'use strict';
          if (window.__wspFullscreenSafetyInstalledV4) return;
          window.__wspFullscreenSafetyInstalledV4 = true;

          const controller = () => window.__wspWindowFullscreen || null;
          const nativeExitFullscreen = Document.prototype.exitFullscreen;
          const nativeWebkitExitFullscreen = Document.prototype.webkitExitFullscreen;
          const nativeFullscreenElement = Object.getOwnPropertyDescriptor(
            Document.prototype,
            'fullscreenElement');
          const nativeWebkitFullscreenElement = Object.getOwnPropertyDescriptor(
            Document.prototype,
            'webkitFullscreenElement');

          function defineAlwaysEnabled(name) {
            try {
              Object.defineProperty(Document.prototype, name, {
                configurable: true,
                enumerable: true,
                get() { return true; }
              });
            } catch {}
          }

          defineAlwaysEnabled('fullscreenEnabled');
          defineAlwaysEnabled('webkitFullscreenEnabled');

          function enterWindowOnly(element) {
            const activeController = controller();
            return activeController?.enter?.(element) || Promise.resolve();
          }

          function exitWindowOnly() {
            const activeController = controller();
            return activeController?.exit?.() || Promise.resolve();
          }

          function defineElementMethod(name) {
            try {
              Object.defineProperty(Element.prototype, name, {
                configurable: true,
                writable: true,
                value: function() {
                  return enterWindowOnly(this);
                }
              });
            } catch {
              try {
                Element.prototype[name] = function() {
                  return enterWindowOnly(this);
                };
              } catch {}
            }
          }

          [
            'requestFullscreen',
            'webkitRequestFullscreen',
            'webkitRequestFullScreen',
            'mozRequestFullScreen',
            'msRequestFullscreen'
          ].forEach(defineElementMethod);

          function defineDocumentExit(name) {
            try {
              Object.defineProperty(Document.prototype, name, {
                configurable: true,
                writable: true,
                value: function() {
                  const activeController = controller();
                  if (activeController?.isActive?.()) return exitWindowOnly();
                  return Promise.resolve();
                }
              });
            } catch {}
          }

          ['exitFullscreen', 'webkitExitFullscreen', 'webkitCancelFullScreen']
            .forEach(defineDocumentExit);

          const fullscreenSelectors = [
            '.ytp-fullscreen-button',
            '[data-uia*="full-screen" i]',
            '[data-uia*="fullscreen" i]',
            '[data-testid*="fullscreen" i]',
            '[data-testid*="full-screen" i]',
            '[data-control*="fullscreen" i]',
            '[class*="fullscreen" i]',
            '[class*="full-screen" i]',
            '[id*="fullscreen" i]',
            '[id*="full-screen" i]',
            '[aria-label*="fullscreen" i]',
            '[aria-label*="full screen" i]',
            '[title*="fullscreen" i]',
            '[title*="full screen" i]'
          ].join(',');

          function fullscreenControlFromEvent(event) {
            for (const node of event.composedPath?.() || []) {
              if (!(node instanceof Element)) continue;
              const candidate = node.closest?.(
                'button, [role="button"], input, a, div[tabindex], span[tabindex]') || node;

              if (candidate.matches?.(fullscreenSelectors)
                  || candidate.closest?.(fullscreenSelectors)) {
                return candidate;
              }

              const label = [
                candidate.getAttribute?.('aria-label'),
                candidate.getAttribute?.('title'),
                candidate.getAttribute?.('data-tooltip-text'),
                candidate.getAttribute?.('data-tooltip'),
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

          function blockEvent(event) {
            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
          }

          let suppressClickUntil = 0;

          window.addEventListener('pointerdown', event => {
            const control = fullscreenControlFromEvent(event);
            if (!control || !controller()) return;

            blockEvent(event);
            suppressClickUntil = performance.now() + 900;
            controller().toggle(control);
          }, true);

          window.addEventListener('mousedown', event => {
            const control = fullscreenControlFromEvent(event);
            if (!control || !controller()) return;

            blockEvent(event);
            if (performance.now() >= suppressClickUntil) {
              suppressClickUntil = performance.now() + 900;
              controller().toggle(control);
            }
          }, true);

          window.addEventListener('click', event => {
            const control = fullscreenControlFromEvent(event);
            if (!control || !controller()) return;

            blockEvent(event);
            if (performance.now() >= suppressClickUntil) {
              suppressClickUntil = performance.now() + 400;
              controller().toggle(control);
            }
          }, true);

          window.addEventListener('keydown', event => {
            const activeController = controller();
            if (!activeController) return;

            const editable = event.target instanceof Element
              && Boolean(event.target.closest(
                'input, textarea, select, [contenteditable="true"]'));

            if (event.key === 'Escape' && activeController.isActive?.()) {
              blockEvent(event);
              activeController.exit();
              return;
            }

            if (event.key.toLowerCase() === 'f'
                && !event.ctrlKey && !event.altKey && !event.metaKey
                && !editable) {
              blockEvent(event);
              activeController.toggle();
            }
          }, true);

          function nativeFullscreenTarget() {
            try {
              return nativeFullscreenElement?.get?.call(document)
                || nativeWebkitFullscreenElement?.get?.call(document)
                || null;
            } catch {
              return null;
            }
          }

          function forceNativeFullscreenBackIntoWindow(event) {
            const target = nativeFullscreenTarget();
            if (!target) return;

            event?.stopImmediatePropagation?.();
            const exit = nativeExitFullscreen?.bind(document)
              || nativeWebkitExitFullscreen?.bind(document);

            Promise.resolve(exit?.())
              .catch(() => {})
              .finally(() => enterWindowOnly(target));
          }

          document.addEventListener('fullscreenchange', forceNativeFullscreenBackIntoWindow, true);
          document.addEventListener('webkitfullscreenchange', forceNativeFullscreenBackIntoWindow, true);
        })();
        """;
}
