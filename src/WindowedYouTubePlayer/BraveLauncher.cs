using System.Diagnostics;

namespace WindowedYouTubePlayer;

internal sealed class PlayerWindowController
{
    private readonly nint _originalStyle;
    private readonly nint _originalExStyle;
    private bool _isBorderless;

    internal PlayerWindowController(nint windowHandle)
    {
        WindowHandle = windowHandle;
        _originalStyle = NativeMethods.GetWindowLongPtr(windowHandle, NativeMethods.GwlStyle);
        _originalExStyle = NativeMethods.GetWindowLongPtr(windowHandle, NativeMethods.GwlExStyle);
    }

    public nint WindowHandle { get; }
    public bool IsAvailable => WindowHandle != nint.Zero && NativeMethods.IsWindow(WindowHandle);
    public bool IsBorderless => _isBorderless;

    public void ToggleFrame()
    {
        if (_isBorderless)
        {
            RestoreFrame();
        }
        else
        {
            ApplyBorderless();
        }
    }

    public void ApplyBorderless()
    {
        if (!IsAvailable)
        {
            return;
        }

        long style = NativeMethods.GetWindowLongPtr(WindowHandle, NativeMethods.GwlStyle).ToInt64();
        long exStyle = NativeMethods.GetWindowLongPtr(WindowHandle, NativeMethods.GwlExStyle).ToInt64();

        style &= ~NativeMethods.WsCaption;
        exStyle &= ~(NativeMethods.WsExDlgModalFrame
                     | NativeMethods.WsExWindowEdge
                     | NativeMethods.WsExClientEdge
                     | NativeMethods.WsExStaticEdge);

        NativeMethods.SetWindowLongPtr(WindowHandle, NativeMethods.GwlStyle, new nint(style));
        NativeMethods.SetWindowLongPtr(WindowHandle, NativeMethods.GwlExStyle, new nint(exStyle));
        RefreshFrame();
        _isBorderless = true;
    }

    public void RestoreFrame()
    {
        if (!IsAvailable)
        {
            return;
        }

        NativeMethods.SetWindowLongPtr(WindowHandle, NativeMethods.GwlStyle, _originalStyle);
        NativeMethods.SetWindowLongPtr(WindowHandle, NativeMethods.GwlExStyle, _originalExStyle);
        RefreshFrame();
        _isBorderless = false;
    }

    public void Close()
    {
        if (IsAvailable)
        {
            NativeMethods.PostMessage(WindowHandle, NativeMethods.WmClose, nint.Zero, nint.Zero);
        }
    }

    private void RefreshFrame() => NativeMethods.SetWindowPos(
        WindowHandle,
        nint.Zero,
        0,
        0,
        0,
        0,
        NativeMethods.SwpNoMove
        | NativeMethods.SwpNoSize
        | NativeMethods.SwpNoZOrder
        | NativeMethods.SwpNoActivate
        | NativeMethods.SwpFrameChanged);
}

internal static class BraveLauncher
{
    public static async Task<PlayerWindowController?> LaunchAsync(
        string bravePath,
        string playerUrl,
        int width,
        int height,
        bool startBorderless,
        CancellationToken cancellationToken = default)
    {
        HashSet<nint> windowsBefore = CaptureBraveWindows();

        ProcessStartInfo startInfo = new(bravePath)
        {
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add($"--app={playerUrl}");
        startInfo.ArgumentList.Add("--new-window");
        startInfo.ArgumentList.Add($"--window-size={width},{height}");
        startInfo.ArgumentList.Add("--disable-session-crashed-bubble");

        Process.Start(startInfo) ?? throw new InvalidOperationException("Brave could not be started.");

        nint handle = await WaitForNewWindowAsync(windowsBefore, cancellationToken);
        if (handle == nint.Zero)
        {
            return null;
        }

        PlayerWindowController controller = new(handle);
        if (startBorderless)
        {
            controller.ApplyBorderless();
        }

        return controller;
    }

    private static HashSet<nint> CaptureBraveWindows()
    {
        HashSet<nint> windows = [];
        NativeMethods.EnumWindows((handle, _) =>
        {
            if (NativeMethods.IsBraveWindow(handle))
            {
                windows.Add(handle);
            }

            return true;
        }, nint.Zero);

        return windows;
    }

    private static async Task<nint> WaitForNewWindowAsync(
        HashSet<nint> windowsBefore,
        CancellationToken cancellationToken)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            nint found = nint.Zero;

            NativeMethods.EnumWindows((handle, _) =>
            {
                if (!windowsBefore.Contains(handle) && NativeMethods.IsBraveWindow(handle))
                {
                    found = handle;
                    return false;
                }

                return true;
            }, nint.Zero);

            if (found != nint.Zero)
            {
                return found;
            }

            await Task.Delay(150, cancellationToken);
        }

        return nint.Zero;
    }
}
