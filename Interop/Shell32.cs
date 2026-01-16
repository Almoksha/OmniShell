using System;
using System.Runtime.InteropServices;

namespace OmniShell.Interop;

/// <summary>
/// P/Invoke declarations for Shell32.dll functions
/// </summary>
public static class Shell32
{
    // SHChangeNotify event types
    public const uint SHCNE_ASSOCCHANGED = 0x08000000;
    public const uint SHCNE_UPDATEDIR = 0x00001000;
    public const uint SHCNE_UPDATEITEM = 0x00002000;
    public const uint SHCNE_ATTRIBUTES = 0x00000800;
    
    // SHChangeNotify flags
    public const uint SHCNF_IDLIST = 0x0000;
    public const uint SHCNF_PATH = 0x0005;
    public const uint SHCNF_FLUSH = 0x1000;
    public const uint SHCNF_FLUSHNOWAIT = 0x3000;

    /// <summary>
    /// Notifies the system of an event that an application has performed.
    /// The shell can then update its cached state accordingly.
    /// </summary>
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern void SHChangeNotify(
        uint wEventId,
        uint uFlags,
        IntPtr dwItem1,
        IntPtr dwItem2);

    /// <summary>
    /// Overload with string parameters for path-based notifications
    /// </summary>
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern void SHChangeNotify(
        uint wEventId,
        uint uFlags,
        [MarshalAs(UnmanagedType.LPWStr)] string dwItem1,
        IntPtr dwItem2);

    // AppBar message types
    public const uint ABM_NEW = 0x00000000;
    public const uint ABM_REMOVE = 0x00000001;
    public const uint ABM_QUERYPOS = 0x00000002;
    public const uint ABM_SETPOS = 0x00000003;
    public const uint ABM_GETSTATE = 0x00000004;
    public const uint ABM_GETTASKBARPOS = 0x00000005;
    public const uint ABM_ACTIVATE = 0x00000006;
    public const uint ABM_GETAUTOHIDEBAR = 0x00000007;
    public const uint ABM_SETAUTOHIDEBAR = 0x00000008;
    public const uint ABM_WINDOWPOSCHANGED = 0x00000009;
    public const uint ABM_SETSTATE = 0x0000000A;

    // AppBar edge constants
    public const int ABE_LEFT = 0;
    public const int ABE_TOP = 1;
    public const int ABE_RIGHT = 2;
    public const int ABE_BOTTOM = 3;

    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public int lParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>
    /// Sends an appbar message to the system.
    /// </summary>
    [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);
}
