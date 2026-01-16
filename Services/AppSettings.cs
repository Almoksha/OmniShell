using System;
using System.IO;
using System.Text.Json;

namespace OmniShell.Services;

/// <summary>
/// Centralized service for storing application-level settings
/// </summary>
public static class AppSettings
{
    private static readonly string SettingsPath;
    private static SettingsData _settings = new();
    
    static AppSettings()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "OmniShell");
        Directory.CreateDirectory(folder);
        SettingsPath = Path.Combine(folder, "app_settings.json");
        Load();
    }
    
    #region Sidebar Settings
    
    public static string GetSidebarMode()
    {
        return _settings.SidebarMode ?? "Floating";
    }
    
    public static void SetSidebarMode(string mode)
    {
        _settings.SidebarMode = mode;
        Save();
    }
    
    public static bool GetSidebarVisibleOnStartup()
    {
        return _settings.SidebarVisibleOnStartup;
    }
    
    public static void SetSidebarVisibleOnStartup(bool visible)
    {
        _settings.SidebarVisibleOnStartup = visible;
        Save();
    }
    
    #endregion
    
    #region Window Settings
    
    public static double GetWindowLeft()
    {
        return _settings.MainWindowLeft;
    }
    
    public static void SetWindowLeft(double left)
    {
        _settings.MainWindowLeft = left;
        Save();
    }
    
    public static double GetWindowTop()
    {
        return _settings.MainWindowTop;
    }
    
    public static void SetWindowTop(double top)
    {
        _settings.MainWindowTop = top;
        Save();
    }
    
    public static double GetWindowWidth()
    {
        return _settings.MainWindowWidth;
    }
    
    public static void SetWindowWidth(double width)
    {
        _settings.MainWindowWidth = width;
        Save();
    }
    
    public static double GetWindowHeight()
    {
        return _settings.MainWindowHeight;
    }
    
    public static void SetWindowHeight(double height)
    {
        _settings.MainWindowHeight = height;
        Save();
    }
    
    public static string GetWindowState()
    {
        return _settings.WindowState ?? "Normal";
    }
    
    public static void SetWindowState(string state)
    {
        _settings.WindowState = state;
        Save();
    }
    
    #endregion
    
    private static void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var data = JsonSerializer.Deserialize<SettingsData>(json);
                if (data != null)
                {
                    _settings = data;
                }
            }
        }
        catch
        {
            // Use default settings if load fails
        }
    }
    
    private static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail if save fails
        }
    }
    
    private class SettingsData
    {
        // Sidebar settings
        public string? SidebarMode { get; set; } = "Floating";
        public bool SidebarVisibleOnStartup { get; set; } = false;
        
        // Window settings
        public double MainWindowLeft { get; set; } = 0;
        public double MainWindowTop { get; set; } = 0;
        public double MainWindowWidth { get; set; } = 1100;
        public double MainWindowHeight { get; set; } = 700;
        public string? WindowState { get; set; } = "Normal";
    }
}
