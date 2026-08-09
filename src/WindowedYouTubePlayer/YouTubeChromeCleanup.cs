namespace WindowedYouTubePlayer;

internal static class YouTubeChromeCleanup
{
    public const string Source = """
        (() => {
          'use strict';
          if (window.__wspYouTubeChromeCleanupV61) return;

          const host = location.hostname.toLowerCase();
          const isYouTube = host === 'youtube.com'
            || host.endsWith('.youtube.com')
            || host === 'youtu.be';
          if (!isYouTube || window.top !== window.self) return;

          window.__wspYouTubeChromeCleanupV61 = true;
          const style = document.createElement('style');
          style.id = 'wsp-youtube-chrome-cleanup-v61';
          style.textContent = `
            html.wsp-youtube-window-fullscreen ytd-masthead,
            html.wsp-youtube-window-fullscreen #masthead-container,
            html.wsp-youtube-window-fullscreen ytd-mini-guide-renderer,
            html.wsp-youtube-window-fullscreen tp-yt-app-drawer,
            html.wsp-youtube-window-fullscreen #guide,
            html.wsp-youtube-window-fullscreen ytd-watch-metadata,
            html.wsp-youtube-window-fullscreen #below,
            html.wsp-youtube-window-fullscreen #secondary,
            html.wsp-youtube-window-fullscreen #comments,
            html.wsp-youtube-window-fullscreen ytd-reel-player-overlay-renderer,
            html.wsp-youtube-window-fullscreen yt-reel-player-overlay-renderer,
            html.wsp-youtube-window-fullscreen ytd-reel-video-renderer #overlay,
            html.wsp-youtube-window-fullscreen ytd-reel-video-renderer #actions,
            html.wsp-youtube-window-fullscreen ytd-reel-video-renderer #metadata,
            html.wsp-youtube-window-fullscreen ytd-reel-video-renderer #channel-info,
            html.wsp-youtube-window-fullscreen ytd-reel-video-renderer #description,
            html.wsp-youtube-window-fullscreen ytd-reel-video-renderer #details,
            html.wsp-youtube-window-fullscreen [class*="shorts" i][class*="metadata" i],
            html.wsp-youtube-window-fullscreen [class*="shorts" i][class*="overlay" i],
            html.wsp-youtube-window-fullscreen [class*="reel" i][class*="metadata" i],
            html.wsp-youtube-window-fullscreen [id*="shorts" i][id*="metadata" i],
            html.wsp-youtube-window-fullscreen [id*="reel" i][id*="metadata" i] {
              display: none !important;
              visibility: hidden !important;
              opacity: 0 !important;
              pointer-events: none !important;
            }

            html.wsp-youtube-window-fullscreen ytd-page-manager,
            html.wsp-youtube-window-fullscreen ytd-watch-flexy,
            html.wsp-youtube-window-fullscreen ytd-shorts {
              margin-top: 0 !important;
              padding-top: 0 !important;
              top: 0 !important;
            }
          `;
          (document.head || document.documentElement).appendChild(style);
        })();
        """;
}
