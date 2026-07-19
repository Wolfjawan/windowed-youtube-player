using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowedYouTubePlayer;

internal static class NativeMethods
{
    internal const int GwlStyle = -16;
    internal const int GwlExStyle = -20;

    internal const long WsCaption = 0x00C00000L;
    internal const long WsExDlgModalFrame = 0x00000001L;
    internal const long WsExWindowEdge = 0x00000100L;
    internal const long WsExClientEdge = 0x00000200L;
    internal const long WsExStaticEdge = 0x00020000L;

    internal const uint SwpNoSize = 0x0001;
    internal const uint SwpNoMove = 0x0002;
    internal const uint SwpNoZOrder = 0x0004;
    internal const uint SwpNoActivate = 0x0010;
    internal const uint SwpFrameChanged = 0x0020;
    internal const uint WmClose = 0x0010;

    internal delegate bool EnumWindowsProc(nint windowHandle, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowVisible(nint windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindow(nint windowHandle);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint windowHandle, StringBuilder className, int maximumCount);

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(nint windowHandle, out uint processId);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong32(nint windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr64(nint windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong32(nint windowHandle, int index, int value);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr64(nint windowHandle, int index, nint value);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowPos(
        nint windowHandle,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostMessage(nint windowHandle, uint message, nint wParam, nint lParam);

    internal static nint GetWindowLongPtr(nint windowHandle, int index) =>
        nint.Size == 8 ? GetWindowLongPtr64(windowHandle, index) : new nint(GetWindowLong32(windowHandle, index));

    internal static nint SetWindowLongPtr(nint windowHandle, int index, nint value) =>
        nint.Size == 8
            ? SetWindowLongPtr64(windowHandle, index, value)
            : new nint(SetWindowLong32(windowHandle, index, value.ToInt32()));

    internal static bool IsBraveWindow(nint windowHandle)
    {
        if (!IsWindowVisible(windowHandle))
        {
            return false;
        }

        StringBuilder className = new(256);
        if (GetClassName(windowHandle, className, className.Capacity) == 0
            || !className.ToString().StartsWith("Chrome_WidgetWin_", StringComparison.Ordinal))
        {
            return false;
        }

        GetWindowThreadProcessId(windowHandle, out uint processId);
        if (processId == 0)
        {
            return false;
        }

        try
        {
            using Process process = Process.GetProcessById((int)processId);
            return string.Equals(process.ProcessName, "brave", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
