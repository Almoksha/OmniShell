using System;
using System.IO;
using System.Reflection;
using OmniShell.Interop;

namespace OmniShell.Services;

/// <summary>
/// Service for managing folder icons through desktop.ini manipulation
/// </summary>
public class FolderIconService
{
    private const string DesktopIniFileName = "desktop.ini";
    private const string ShellClassInfoSection = "[.ShellClassInfo]";
    private readonly string _iconResourcePath;

    /// <summary>
    /// Predefined folder colors with their icon indices
    /// </summary>
    public enum FolderColor
    {
        Default = -1,
        Red = 0,
        Orange = 1,
        Yellow = 2,
        Green = 3,
        Blue = 4,
        Purple = 5,
        Pink = 6,
        Brown = 7,
        Gray = 8,
        Cyan = 9
    }

    public FolderIconService()
    {
        // Get the path to the Resources folder next to the executable
        string? exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        _iconResourcePath = Path.Combine(exeDir ?? "", "Resources", "FolderIcons");
        
        Log($"=== FolderIconService Initialized ===");
        Log($"Assembly Location: {Assembly.GetExecutingAssembly().Location}");
        Log($"Exe Directory: {exeDir}");
        Log($"Icon Resource Path: {_iconResourcePath}");
        Log($"Icon Resource Path Exists: {Directory.Exists(_iconResourcePath)}");
        
        if (Directory.Exists(_iconResourcePath))
        {
            var files = Directory.GetFiles(_iconResourcePath);
            Log($"Files in icon directory ({files.Length}):");
            foreach (var file in files)
            {
                Log($"  - {Path.GetFileName(file)} ({new FileInfo(file).Length} bytes)");
            }
        }
    }

    private void Log(string message)
    {
        string logMessage = $"[FolderIconService] {message}";
        System.Diagnostics.Debug.WriteLine(logMessage);
        Console.WriteLine(logMessage);
    }

    /// <summary>
    /// Sets a custom icon for a folder
    /// </summary>
    public bool SetFolderIcon(string folderPath, string iconPath, int iconIndex = 0)
    {
        Log($"--- SetFolderIcon called ---");
        Log($"  Folder Path: {folderPath}");
        Log($"  Icon Path: {iconPath}");
        Log($"  Icon Index: {iconIndex}");
        Log($"  Icon Path Exists: {File.Exists(iconPath)}");
        
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Log($"  ERROR: Folder does not exist!");
                return false;
            }

            string iniPath = Path.Combine(folderPath, DesktopIniFileName);
            Log($"  Desktop.ini Path: {iniPath}");

            // Step 1: Remove system/hidden attributes if file exists
            if (File.Exists(iniPath))
            {
                Log($"  Existing desktop.ini found, removing attributes...");
                File.SetAttributes(iniPath, FileAttributes.Normal);
            }

            // Step 2: Write the desktop.ini content with BOTH formats for compatibility
            // Using Unicode encoding which Windows requires
            string content = $"{ShellClassInfoSection}\r\nIconFile={iconPath}\r\nIconIndex={iconIndex}\r\nIconResource={iconPath},{iconIndex}\r\n";
            Log($"  Writing desktop.ini content:");
            Log($"    {content.Replace("\r\n", " | ")}");
            File.WriteAllText(iniPath, content, System.Text.Encoding.Unicode);
            Log($"  desktop.ini written successfully (Unicode encoding)");

            // Verify what was written
            string writtenContent = File.ReadAllText(iniPath);
            Log($"  Verification - desktop.ini now contains: {writtenContent.Replace("\r\n", " | ")}");

            // Step 3: Set required attributes on desktop.ini
            Log($"  Setting desktop.ini attributes (Hidden + System)...");
            File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System);

            // Step 4: Set ReadOnly flag on the folder
            var folderAttrs = File.GetAttributes(folderPath);
            Log($"  Current folder attributes: {folderAttrs}");
            File.SetAttributes(folderPath, folderAttrs | FileAttributes.ReadOnly);
            Log($"  Set folder ReadOnly attribute");

            // Step 5: Notify the shell to refresh
            Log($"  Calling RefreshFolderIcon...");
            RefreshFolderIcon(folderPath);

            Log($"  SUCCESS: SetFolderIcon completed");
            return true;
        }
        catch (Exception ex)
        {
            Log($"  EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            Log($"  Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Sets a folder color using a predefined color
    /// </summary>
    public bool SetFolderColor(string folderPath, FolderColor color)
    {
        Log($"--- SetFolderColor called ---");
        Log($"  Folder Path: {folderPath}");
        Log($"  Color: {color}");
        
        if (color == FolderColor.Default)
        {
            Log($"  Color is Default, calling RemoveFolderIcon...");
            return RemoveFolderIcon(folderPath);
        }

        // Get the path to the colored folder icon
        string iconPath = GetColoredFolderIconPath(color);
        Log($"  Resolved Icon Path: {iconPath}");
        Log($"  Icon File Exists: {File.Exists(iconPath)}");
        
        // Verify the icon file exists
        if (!File.Exists(iconPath))
        {
            Log($"  ERROR: Icon file not found!");
            
            // Try to find what files DO exist
            if (Directory.Exists(_iconResourcePath))
            {
                Log($"  Available files in {_iconResourcePath}:");
                foreach (var file in Directory.GetFiles(_iconResourcePath))
                {
                    Log($"    - {file}");
                }
            }
            else
            {
                Log($"  ERROR: Icon resource directory doesn't exist: {_iconResourcePath}");
            }
            
            return false;
        }

        return SetFolderIcon(folderPath, iconPath, 0);
    }

    /// <summary>
    /// Removes custom icon from a folder (restores default appearance)
    /// </summary>
    public bool RemoveFolderIcon(string folderPath)
    {
        Log($"--- RemoveFolderIcon called ---");
        Log($"  Folder Path: {folderPath}");
        
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Log($"  ERROR: Folder does not exist!");
                return false;
            }

            string iniPath = Path.Combine(folderPath, DesktopIniFileName);
            Log($"  Desktop.ini Path: {iniPath}");

            if (File.Exists(iniPath))
            {
                Log($"  Deleting desktop.ini...");
                File.SetAttributes(iniPath, FileAttributes.Normal);
                File.Delete(iniPath);
                Log($"  desktop.ini deleted");
            }
            else
            {
                Log($"  No desktop.ini found");
            }

            // Remove the ReadOnly flag from the folder
            var folderAttrs = File.GetAttributes(folderPath);
            File.SetAttributes(folderPath, folderAttrs & ~FileAttributes.ReadOnly);
            Log($"  Removed folder ReadOnly attribute");

            // Notify the shell
            RefreshFolderIcon(folderPath);
            Log($"  SUCCESS: RemoveFolderIcon completed");

            return true;
        }
        catch (Exception ex)
        {
            Log($"  EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the current icon for a folder, if any
    /// </summary>
    public (string? IconPath, int IconIndex)? GetFolderIcon(string folderPath)
    {
        try
        {
            string iniPath = Path.Combine(folderPath, DesktopIniFileName);
            if (!File.Exists(iniPath))
                return null;

            var originalAttrs = File.GetAttributes(iniPath);
            File.SetAttributes(iniPath, FileAttributes.Normal);

            string[] lines = File.ReadAllLines(iniPath);
            File.SetAttributes(iniPath, originalAttrs);

            foreach (string line in lines)
            {
                if (line.StartsWith("IconResource=", StringComparison.OrdinalIgnoreCase))
                {
                    string value = line.Substring("IconResource=".Length);
                    int commaIndex = value.LastIndexOf(',');
                    if (commaIndex > 0)
                    {
                        string path = value.Substring(0, commaIndex);
                        if (int.TryParse(value.Substring(commaIndex + 1), out int index))
                        {
                            return (path, index);
                        }
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Forces Windows Explorer to refresh the icon cache for a specific folder
    /// </summary>
    public void RefreshFolderIcon(string folderPath)
    {
        Log($"  RefreshFolderIcon: {folderPath}");
        
        // Method 1: Notify shell of directory update (most reliable)
        Shell32.SHChangeNotify(
            Shell32.SHCNE_UPDATEDIR,
            Shell32.SHCNF_PATH | Shell32.SHCNF_FLUSH,
            folderPath,
            IntPtr.Zero);
        Log($"    Sent SHCNE_UPDATEDIR with FLUSH");

        // Method 2: Notify about the specific item
        Shell32.SHChangeNotify(
            Shell32.SHCNE_UPDATEITEM,
            Shell32.SHCNF_PATH | Shell32.SHCNF_FLUSH,
            folderPath,
            IntPtr.Zero);
        Log($"    Sent SHCNE_UPDATEITEM with FLUSH");
        
        // Method 3: Notify about attributes change
        Shell32.SHChangeNotify(
            Shell32.SHCNE_ATTRIBUTES,
            Shell32.SHCNF_PATH | Shell32.SHCNF_FLUSH,
            folderPath,
            IntPtr.Zero);
        Log($"    Sent SHCNE_ATTRIBUTES with FLUSH");

        // Method 4: Also notify parent directory
        string? parentPath = Path.GetDirectoryName(folderPath);
        if (!string.IsNullOrEmpty(parentPath))
        {
            Shell32.SHChangeNotify(
                Shell32.SHCNE_UPDATEDIR,
                Shell32.SHCNF_PATH | Shell32.SHCNF_FLUSH,
                parentPath,
                IntPtr.Zero);
            Log($"    Sent SHCNE_UPDATEDIR for parent");
        }
        
        // Method 5: Force full association change (nuclear option)
        Shell32.SHChangeNotify(
            Shell32.SHCNE_ASSOCCHANGED,
            Shell32.SHCNF_IDLIST | Shell32.SHCNF_FLUSH,
            IntPtr.Zero,
            IntPtr.Zero);
        Log($"    Sent SHCNE_ASSOCCHANGED (full refresh)");
        
        // Method 6: Run ie4uinit to refresh icon cache without Explorer restart
        try
        {
            Log($"    Running ie4uinit.exe -show...");
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ie4uinit.exe",
                Arguments = "-show",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            System.Diagnostics.Process.Start(psi)?.WaitForExit(2000);
            Log($"    ie4uinit.exe completed");
        }
        catch (Exception ex)
        {
            Log($"    ie4uinit.exe failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Forces a full icon cache refresh
    /// </summary>
    public void RefreshAllIcons()
    {
        Log($"RefreshAllIcons called");
        Shell32.SHChangeNotify(
            Shell32.SHCNE_ASSOCCHANGED,
            Shell32.SHCNF_IDLIST | Shell32.SHCNF_FLUSH,
            IntPtr.Zero,
            IntPtr.Zero);
    }

    /// <summary>
    /// Gets the path to the colored folder icon file
    /// </summary>
    private string GetColoredFolderIconPath(FolderColor color)
    {
        string fileName = color switch
        {
            FolderColor.Red => "red.ico",
            FolderColor.Orange => "orange.ico",
            FolderColor.Yellow => "yellow.ico",
            FolderColor.Green => "green.ico",
            FolderColor.Blue => "blue.ico",
            FolderColor.Purple => "purple.ico",
            FolderColor.Pink => "pink.ico",
            FolderColor.Brown => "gray.ico",
            FolderColor.Gray => "gray.ico",
            FolderColor.Cyan => "cyan.ico",
            _ => "yellow.ico"
        };

        string fullPath = Path.Combine(_iconResourcePath, fileName);
        Log($"  GetColoredFolderIconPath: {color} -> {fileName} -> {fullPath}");
        return fullPath;
    }
}
