using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuickFiles.Services;

internal static class ShellInterop
{
    // ---- Known folders ----------------------------------------------------

    private static readonly Guid DownloadsFolderId = new("374DE290-123F-4565-9164-39C4925E467B");

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

    public static string? GetDownloadsPath()
    {
        if (SHGetKnownFolderPath(DownloadsFolderId, 0, IntPtr.Zero, out var pathPtr) != 0)
            return null;
        try
        {
            return Marshal.PtrToStringUni(pathPtr);
        }
        finally
        {
            Marshal.FreeCoTaskMem(pathPtr);
        }
    }

    // ---- .lnk shortcut resolution ------------------------------------------

    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLinkCoClass
    {
    }

    [ComImport, Guid("000214F9-0000-0000-C000-000000000046"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out ushort pwHotkey);
        void SetHotkey(ushort wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    /// <summary>
    /// Returns the target path of a .lnk shortcut, or null if it has no
    /// filesystem target (e.g. links to virtual shell items). Does not call
    /// Resolve(), so it never hits the network or shows UI.
    /// </summary>
    public static string? ResolveShortcut(string lnkPath)
    {
        try
        {
            var link = (IShellLinkW)new ShellLinkCoClass();
            try
            {
                ((IPersistFile)link).Load(lnkPath, 0);
                var buffer = new StringBuilder(1024);
                link.GetPath(buffer, buffer.Capacity, IntPtr.Zero, 0);
                var target = buffer.ToString();
                return string.IsNullOrWhiteSpace(target) ? null : target;
            }
            finally
            {
                Marshal.ReleaseComObject(link);
            }
        }
        catch
        {
            return null;
        }
    }

    // ---- Cursor / monitor info (for positioning the flyout) ----------------

    /// <summary>All values in device pixels.</summary>
    public readonly record struct FlyoutAnchor(Point Cursor, Rect Work, Rect Monitor);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT pt);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfoW(IntPtr hMonitor, ref MONITORINFO lpmi);

    /// <summary>
    /// Returns the cursor position plus the work/full bounds of the monitor it
    /// is on. Captured when the flyout is summoned, the cursor sits on the
    /// taskbar icon the user just clicked — so it anchors the flyout to it.
    /// </summary>
    public static FlyoutAnchor? GetFlyoutAnchor()
    {
        if (!GetCursorPos(out var pt))
            return null;
        var monitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero)
            return null;
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfoW(monitor, ref info))
            return null;

        static Rect ToRect(RECT r) => new(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        return new FlyoutAnchor(new Point(pt.X, pt.Y), ToRect(info.rcWork), ToRect(info.rcMonitor));
    }

    // ---- Recycle bin --------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCTW
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string pFrom;
        public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string? lpszProgressTitle;
    }

    private const uint FO_DELETE = 3;
    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOERRORUI = 0x0400;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperationW(ref SHFILEOPSTRUCTW lpFileOp);

    public static bool MoveToRecycleBin(string path)
    {
        try
        {
            var op = new SHFILEOPSTRUCTW
            {
                wFunc = FO_DELETE,
                // pFrom must be double-null-terminated; the marshaler adds one.
                pFrom = path + "\0",
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI,
            };
            return SHFileOperationW(ref op) == 0 && !op.fAnyOperationsAborted;
        }
        catch
        {
            return false;
        }
    }

    // ---- File icons ---------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_LARGEICON = 0x0;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static ImageSource? GetFileIcon(string path)
    {
        var info = new SHFILEINFO();
        try
        {
            SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_LARGEICON);
            if (info.hIcon == IntPtr.Zero)
                return null;

            var source = Imaging.CreateBitmapSourceFromHIcon(
                info.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (info.hIcon != IntPtr.Zero)
                DestroyIcon(info.hIcon);
        }
    }
}
