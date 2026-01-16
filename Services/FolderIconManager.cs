using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OmniShell.Services;

/// <summary>
/// Manages folder icon customization via desktop.ini
/// </summary>
public class FolderIconManager
{
    #region Windows API Imports
    
    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint Msg, UIntPtr wParam, IntPtr lParam,
        uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    
    [DllImport("shell32.dll")]
    private static extern int SHGetSetFolderCustomSettings(
        ref SHFOLDERCUSTOMSETTINGS pfcs, string pszPath, uint dwReadWrite);
    
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ILCreateFromPath(string pszPath);
    
    [DllImport("shell32.dll")]
    private static extern void ILFree(IntPtr pidl);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetFileTime(IntPtr hFile, IntPtr lpCreationTime, IntPtr lpLastAccessTime, ref long lpLastWriteTime);
    
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
    
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);
    
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    
    private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
    private const uint WM_SETTINGCHANGE = 0x001A;
    private const uint SMTO_ABORTIFHUNG = 0x0002;
    
    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const int SHCNF_IDLIST = 0x0000;
    private const int SHCNE_UPDATEDIR = 0x00001000;
    private const int SHCNE_UPDATEITEM = 0x00002000;
    private const int SHCNE_DELETE = 0x00000004;
    private const int SHCNE_CREATE = 0x00000002;
    private const int SHCNE_ATTRIBUTES = 0x00000800;
    private const int SHCNF_PATHW = 0x0005;
    private const int SHCNF_FLUSH = 0x1000;
    private const int SHCNF_FLUSHNOWAIT = 0x3000;
    private const int SHCNE_ALLEVENTS = 0x7FFFFFFF;
    
    // Keyboard event constants for F5 refresh
    private const byte VK_F5 = 0x74;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    
    private const uint FCS_READ = 0x00000001;
    private const uint FCS_FORCEWRITE = 0x00000002;
    private const uint FCSM_ICONFILE = 0x00000010;
    
    // File access constants
    private const uint FILE_WRITE_ATTRIBUTES = 0x0100;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFOLDERCUSTOMSETTINGS
    {
        public uint dwSize;
        public uint dwMask;
        public IntPtr pvid;
        public string pszWebViewTemplate;
        public uint cchWebViewTemplate;
        public string pszWebViewTemplateVersion;
        public string pszInfoTip;
        public uint cchInfoTip;
        public IntPtr pclsid;
        public uint dwFlags;
        public string pszIconFile;
        public uint cchIconFile;
        public int iIconIndex;
        public string pszLogo;
        public uint cchLogo;
    }
    
    #endregion
    
    /// <summary>
    /// Apply a custom icon to a folder
    /// </summary>
    public async Task ApplyFolderIcon(string folderPath, string iconPath)
    {
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] === ApplyFolderIcon START ===");
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] FolderPath: {folderPath}");
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] IconPath: {iconPath}");
        
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
        
        if (!File.Exists(iconPath))
            throw new FileNotFoundException($"Icon not found: {iconPath}");
        
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] Icon file exists: {File.Exists(iconPath)}");
        
        var iniPath = Path.Combine(folderPath, "desktop.ini");
        var dirInfo = new DirectoryInfo(folderPath);
        
        // Step 1: Remove attributes to allow modifications
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] Step 1: Removing folder attributes");
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   Before: {dirInfo.Attributes}");
        dirInfo.Attributes &= ~(FileAttributes.ReadOnly | FileAttributes.System);
        dirInfo.Refresh();
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   After: {dirInfo.Attributes}");
        
        // Step 2: Delete existing desktop.ini and old .foldericon files
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] Step 2: Cleaning up existing files");
        if (File.Exists(iniPath))
        {
            System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   Deleting existing desktop.ini");
            File.SetAttributes(iniPath, FileAttributes.Normal);
            File.Delete(iniPath);
        }
        
        // Delete any old .foldericon*.ico files
        foreach (var oldIcon in Directory.GetFiles(folderPath, ".foldericon*.ico"))
        {
            try
            {
                File.SetAttributes(oldIcon, FileAttributes.Normal);
                File.Delete(oldIcon);
                System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   Deleted old icon: {oldIcon}");
            }
            catch { }
        }
        
        // Step 3: Copy icon INTO the folder with a unique name (bypasses Windows cache!)
        var uniqueId = DateTime.Now.Ticks.ToString();
        var localIconName = $".foldericon_{uniqueId}.ico";
        var localIconPath = Path.Combine(folderPath, localIconName);
        
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] Step 3: Copying icon to folder");
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   Source: {iconPath}");
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   Destination: {localIconPath}");
        File.Copy(iconPath, localIconPath, overwrite: true);
        File.SetAttributes(localIconPath, FileAttributes.Hidden | FileAttributes.System);
        
        // Step 4: Write new desktop.ini pointing to LOCAL icon (UTF-8, like Python script)
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] Step 4: Writing new desktop.ini (UTF-8)");
        var content = "[.ShellClassInfo]\n" +
                      $"IconResource={localIconPath},0\n";
        
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   Content:\n{content}");
        File.WriteAllText(iniPath, content, Encoding.UTF8);
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   File written: {File.Exists(iniPath)}");
        
        // Step 5: Set desktop.ini attributes (Hidden|System|Archive - matches Windows Properties)
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] Step 5: Setting desktop.ini attributes to Hidden|System|Archive");
        File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive);
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   Attributes: {File.GetAttributes(iniPath)}");
        
        // Step 6: Touch desktop.ini timestamp (CRITICAL - signals Explorer metadata changed)
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] Step 6: Touching desktop.ini timestamp");
        var beforeTime = File.GetLastWriteTimeUtc(iniPath);
        File.SetLastWriteTimeUtc(iniPath, DateTime.UtcNow);
        var afterTime = File.GetLastWriteTimeUtc(iniPath);
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   Before: {beforeTime}, After: {afterTime}");
        
        // Step 7: Toggle ReadOnly attribute (CRITICAL - forces Explorer to reparse)
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] Step 7: Toggling ReadOnly attribute");
        dirInfo.Refresh();
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   Before toggle: {dirInfo.Attributes}");
        dirInfo.Attributes &= ~FileAttributes.ReadOnly;
        dirInfo.Refresh();
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   ReadOnly removed: {dirInfo.Attributes}");
        await Task.Delay(50); // Small delay for filesystem sync
        dirInfo.Attributes |= FileAttributes.ReadOnly;
        dirInfo.Refresh();
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   ReadOnly added back: {dirInfo.Attributes}");
        
        // Step 8: Also set via Windows Shell API (how Windows Properties does it)
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] Step 8: Calling SetFolderIconViaApi");
        try
        {
            SetFolderIconViaApi(folderPath, localIconPath);
            System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   SetFolderIconViaApi succeeded");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   SetFolderIconViaApi failed: {ex.Message}");
        }
        
        // Step 9: Targeted folder refresh with FLUSH
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] Step 9: Calling RefreshFolderIcon for folder");
        RefreshFolderIcon(folderPath);
        
        // Step 9: Also refresh parent (handles case when folder is open in Explorer)
        var parentPath = Directory.GetParent(folderPath)?.FullName;
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] Step 9: Refreshing parent folder: {parentPath}");
        if (parentPath != null)
        {
            RefreshFolderIcon(parentPath);
        }
        
        // Step 9: Global flush
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] Step 9: Global SHCNE_ASSOCCHANGED notification");
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
        
        // Step 10: Run ie4uinit.exe to refresh icon cache (safe method)
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] Step 10: Running ie4uinit.exe -show");
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "ie4uinit.exe";
            process.StartInfo.Arguments = "-show";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            process.WaitForExit(3000);
            System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   ie4uinit.exe completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   ie4uinit.exe failed: {ex.Message}");
        }
        
        // Step 11: Send F5 refresh to all Explorer windows
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] Step 11: Calling RefreshAllExplorerWindows");
        RefreshAllExplorerWindows();
        
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] === ApplyFolderIcon END ===");
    }
    
    /// <summary>
    /// Temporarily rename folder to force Explorer to refresh icon cache
    /// This is the most reliable method to force icon updates
    /// </summary>
    private void TempRenameFolder(string folderPath)
    {
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] TempRenameFolder START: {folderPath}");
        try
        {
            var parentDir = Path.GetDirectoryName(folderPath);
            var folderName = Path.GetFileName(folderPath);
            
            if (parentDir == null || folderName == null)
            {
                System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   SKIP: parentDir or folderName is null");
                return;
            }
            
            // Create a temporary name
            var tempName = folderName + "_tmp";
            var tempPath = Path.Combine(parentDir, tempName);
            
            // Ensure temp path doesn't exist
            if (Directory.Exists(tempPath))
            {
                tempName = folderName + "_tmp" + Guid.NewGuid().ToString("N").Substring(0, 4);
                tempPath = Path.Combine(parentDir, tempName);
            }
            
            System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   TempPath: {tempPath}");
            
            // Rename to temp name
            System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   Moving {folderPath} -> {tempPath}");
            Directory.Move(folderPath, tempPath);
            
            // Immediately rename back
            System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   Moving {tempPath} -> {folderPath}");
            Directory.Move(tempPath, folderPath);
            
            System.Diagnostics.Debug.WriteLine($"[FolderIconManager] TempRenameFolder SUCCESS");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FolderIconManager] TempRenameFolder FAILED: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Targeted folder icon refresh with proper flags
    /// </summary>
    private void RefreshFolderIcon(string folderPath)
    {
        var pathPtr = Marshal.StringToCoTaskMemUni(folderPath);
        try
        {
            // SHCNE_UPDATEITEM with SHCNF_FLUSH is the correct combination
            SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSH, pathPtr, IntPtr.Zero);
            SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_PATHW | SHCNF_FLUSH, pathPtr, IntPtr.Zero);
        }
        finally
        {
            Marshal.FreeCoTaskMem(pathPtr);
        }
    }
    
    /// <summary>
    /// Send F5 refresh to all Explorer windows (nuclear option)
    /// </summary>
    private void RefreshAllExplorerWindows()
    {
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] RefreshAllExplorerWindows START");
        var explorerWindows = new List<IntPtr>();
        
        EnumWindows((hWnd, lParam) =>
        {
            if (IsWindowVisible(hWnd))
            {
                var className = new StringBuilder(256);
                GetClassName(hWnd, className, 256);
                
                // CabinetWClass is the class name for Explorer windows
                if (className.ToString() == "CabinetWClass")
                {
                    explorerWindows.Add(hWnd);
                }
            }
            return true; // Continue enumeration
        }, IntPtr.Zero);
        
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   Found {explorerWindows.Count} Explorer windows");
        
        // Send F5 to each Explorer window
        foreach (var hWnd in explorerWindows)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   Sending F5 to window 0x{hWnd.ToInt64():X}");
                SetForegroundWindow(hWnd);
                Thread.Sleep(50); // Small delay for window to come to foreground
                
                // Send F5 key down and up
                keybd_event(VK_F5, 0, 0, UIntPtr.Zero);
                keybd_event(VK_F5, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FolderIconManager]   Failed to refresh window: {ex.Message}");
            }
        }
        System.Diagnostics.Debug.WriteLine($"[FolderIconManager] RefreshAllExplorerWindows END");
    }
    
    /// <summary>
    /// Use Windows API to set folder icon (additional shell integration)
    /// </summary>
    private void SetFolderIconViaApi(string folderPath, string iconPath)
    {
        var fcs = new SHFOLDERCUSTOMSETTINGS
        {
            dwSize = (uint)Marshal.SizeOf<SHFOLDERCUSTOMSETTINGS>(),
            dwMask = FCSM_ICONFILE,
            pszIconFile = iconPath,
            cchIconFile = 0,
            iIconIndex = 0
        };
        
        int result = SHGetSetFolderCustomSettings(ref fcs, folderPath, FCS_FORCEWRITE);
        if (result != 0)
        {
            throw new Exception($"SHGetSetFolderCustomSettings failed with code {result}");
        }
    }
    
    /// <summary>
    /// Primary method using desktop.ini (most reliable cross-version)
    /// </summary>
    private void SetFolderIconViaDesktopIni(string folderPath, string iconPath)
    {
        var iniPath = Path.Combine(folderPath, "desktop.ini");
        
        // Remove folder attributes first to allow modifications
        var dirInfo = new DirectoryInfo(folderPath);
        dirInfo.Attributes &= ~(FileAttributes.ReadOnly | FileAttributes.System);
        
        // Remove existing desktop.ini if present
        if (File.Exists(iniPath))
        {
            // Force remove all attributes to ensure we can delete it
            File.SetAttributes(iniPath, FileAttributes.Normal);
            File.Delete(iniPath);
        }
        
        // Write desktop.ini content with proper Windows format
        // Use both IconFile and IconResource for maximum compatibility
        var content = "[.ShellClassInfo]\r\n" +
                      $"IconFile={iconPath}\r\n" +
                      "IconIndex=0\r\n" +
                      $"IconResource={iconPath},0\r\n";
        
        File.WriteAllText(iniPath, content, Encoding.Unicode);
        
        // Set desktop.ini as hidden and system
        File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System);
        
        // Set folder as read-only AND system (both required for reliable custom icon)
        // Note: ReadOnly is standard, but System attribute helps prevent some clearing operations
        dirInfo.Attributes |= FileAttributes.ReadOnly | FileAttributes.System;
    }
    
    /// <summary>
    /// Remove custom icon from a folder (reset to default)
    /// </summary>
    public void RemoveFolderIcon(string folderPath)
    {
        RemoveFolderIconInternal(folderPath, triggerRefresh: true);
    }
    
    /// <summary>
    /// Internal method to remove folder icon with optional refresh
    /// </summary>
    private void RemoveFolderIconInternal(string folderPath, bool triggerRefresh)
    {
        if (!Directory.Exists(folderPath))
            return;
        
        // Remove folder attributes first (both ReadOnly and System)
        var dirInfo = new DirectoryInfo(folderPath);
        dirInfo.Attributes &= ~(FileAttributes.ReadOnly | FileAttributes.System);
        
        var iniPath = Path.Combine(folderPath, "desktop.ini");
        
        if (File.Exists(iniPath))
        {
            File.SetAttributes(iniPath, FileAttributes.Normal);
            File.Delete(iniPath);
        }
        
        // Optionally refresh Explorer
        if (triggerRefresh)
        {
            ForceExplorerRefresh(folderPath);
        }
    }
    
    /// <summary>
    /// Force Explorer to refresh and show new icon immediately
    /// Uses multiple aggressive techniques to ensure the icon updates
    /// </summary>
    private void ForceExplorerRefresh(string folderPath)
    {
        var iniPath = Path.Combine(folderPath, "desktop.ini");
        
        // Step 1: Touch the folder's timestamp to force Explorer to re-read metadata
        TouchFolderTimestamp(folderPath);
        
        // Step 2: Notify that desktop.ini was deleted (trick to invalidate cache)
        if (File.Exists(iniPath))
        {
            var iniPtr = Marshal.StringToCoTaskMemUni(iniPath);
            try
            {
                SHChangeNotify(SHCNE_DELETE, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, iniPtr, IntPtr.Zero);
                SHChangeNotify(SHCNE_CREATE, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, iniPtr, IntPtr.Zero);
                SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, iniPtr, IntPtr.Zero);
            }
            finally
            {
                Marshal.FreeCoTaskMem(iniPtr);
            }
        }
        
        // Step 3: Use PIDL-based notification (more reliable than path-based)
        NotifyWithPidl(folderPath);
        
        // Step 4: Path-based notifications with flush
        var pathPtr = Marshal.StringToCoTaskMemUni(folderPath);
        try
        {
            SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATHW | SHCNF_FLUSH, pathPtr, IntPtr.Zero);
            SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW | SHCNF_FLUSH, pathPtr, IntPtr.Zero);
            SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_PATHW | SHCNF_FLUSH, pathPtr, IntPtr.Zero);
        }
        finally
        {
            Marshal.FreeCoTaskMem(pathPtr);
        }
        
        // Step 5: Notify all parent folders up the chain
        NotifyParentFolders(folderPath);
        
        // Step 6: Global association change notification (nuclear option)
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
        
        // Step 7: Broadcast settings change to all windows
        UIntPtr result;
        SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, UIntPtr.Zero, IntPtr.Zero,
            SMTO_ABORTIFHUNG, 1000, out result);
        
        // Step 8: Delete icon cache database files (most aggressive technique for modern Windows)
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var explorerCacheDir = Path.Combine(localAppData, "Microsoft", "Windows", "Explorer");
            
            if (Directory.Exists(explorerCacheDir))
            {
                foreach (var file in Directory.GetFiles(explorerCacheDir, "iconcache*.db"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Files may be locked by Explorer - this is expected
                    }
                }
                
                foreach (var file in Directory.GetFiles(explorerCacheDir, "thumbcache*.db"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Files may be locked by Explorer - this is expected
                    }
                }
            }
        }
        catch
        {
            // Ignore cache deletion failures
        }
        
        // Step 9: Run ie4uinit.exe to force icon cache refresh (most reliable on modern Windows)
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "ie4uinit.exe";
            process.StartInfo.Arguments = "-show";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            process.WaitForExit(2000); // Wait up to 2 seconds
        }
        catch
        {
            // Ignore if ie4uinit is not available
        }
    }
    
    /// <summary>
    /// Use PIDL-based notification which is more reliable for folder icons
    /// </summary>
    private void NotifyWithPidl(string folderPath)
    {
        IntPtr pidl = ILCreateFromPath(folderPath);
        if (pidl != IntPtr.Zero)
        {
            try
            {
                SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_IDLIST | SHCNF_FLUSH, pidl, IntPtr.Zero);
                SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_IDLIST | SHCNF_FLUSH, pidl, IntPtr.Zero);
            }
            finally
            {
                ILFree(pidl);
            }
        }
    }
    
    /// <summary>
    /// Notify all parent folders up to the root
    /// </summary>
    private void NotifyParentFolders(string folderPath)
    {
        var parent = Directory.GetParent(folderPath);
        int depth = 0;
        
        while (parent != null && depth < 3) // Notify up to 3 levels
        {
            var parentPtr = Marshal.StringToCoTaskMemUni(parent.FullName);
            try
            {
                SHChangeNotify(SHCNE_UPDATEDIR, SHCNF_PATHW | SHCNF_FLUSHNOWAIT, parentPtr, IntPtr.Zero);
            }
            finally
            {
                Marshal.FreeCoTaskMem(parentPtr);
            }
            
            // Also try PIDL-based
            NotifyWithPidl(parent.FullName);
            
            parent = parent.Parent;
            depth++;
        }
    }
    
    /// <summary>
    /// Touch folder's last write time to force Explorer to re-read folder metadata
    /// </summary>
    private void TouchFolderTimestamp(string folderPath)
    {
        try
        {
            // Use CreateFile with FILE_FLAG_BACKUP_SEMANTICS to open a directory
            IntPtr handle = CreateFile(
                folderPath,
                FILE_WRITE_ATTRIBUTES,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);
            
            if (handle != INVALID_HANDLE_VALUE)
            {
                try
                {
                    long now = DateTime.Now.ToFileTime();
                    SetFileTime(handle, IntPtr.Zero, IntPtr.Zero, ref now);
                }
                finally
                {
                    CloseHandle(handle);
                }
            }
        }
        catch
        {
            // Fallback: use .NET method
            try
            {
                Directory.SetLastWriteTime(folderPath, DateTime.Now);
            }
            catch
            {
                // Ignore if we can't touch the timestamp
            }
        }
    }
}
