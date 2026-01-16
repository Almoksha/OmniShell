using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;

namespace OmniShell.Services;

/// <summary>
/// Manages Windows startup registration for the application
/// </summary>
public static class StartupManager
{
    private const string AppName = "OmniShell";
    private const string StartupKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Check if the app is set to start with Windows
    /// </summary>
    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupKeyPath, false);
            var value = key?.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Add the app to Windows startup
    /// </summary>
    public static bool EnableStartup()
    {
        try
        {
            var exePath = Assembly.GetExecutingAssembly().Location;
            
            // If it's a .dll (happens with dotnet run), get the actual exe path
            if (exePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                var directory = Path.GetDirectoryName(exePath);
                var exeName = Path.GetFileNameWithoutExtension(exePath) + ".exe";
                var potentialExePath = Path.Combine(directory ?? "", exeName);
                
                if (File.Exists(potentialExePath))
                {
                    exePath = potentialExePath;
                }
                else
                {
                    // Fallback: use dotnet run command (not ideal but works in dev)
                    exePath = $"dotnet run --project \"{directory}\"";
                }
            }

            using var key = Registry.CurrentUser.OpenSubKey(StartupKeyPath, true);
            key?.SetValue(AppName, $"\"{exePath}\"");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to enable startup: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Remove the app from Windows startup
    /// </summary>
    public static bool DisableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupKeyPath, true);
            key?.DeleteValue(AppName, false);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to disable startup: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Toggle startup enabled/disabled
    /// </summary>
    public static bool ToggleStartup()
    {
        if (IsStartupEnabled())
        {
            return DisableStartup();
        }
        else
        {
            return EnableStartup();
        }
    }
}
