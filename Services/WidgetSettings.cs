using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OmniShell.Services;

/// <summary>
/// Service for storing widget visibility and configuration settings
/// </summary>
public static class WidgetSettings
{
    private static readonly string SettingsPath;
    private static Dictionary<string, bool> _visibility = new();
    private static string _weatherLocation = "";
    private static List<string> _widgetOrder = new()
    {
        "Clipboard", "System", "Calendar", "Weather", "Pomodoro", "Notes", "Battery", "Network"
    };
    
    public static event EventHandler? WeatherLocationChanged;
    public static event EventHandler? WidgetOrderChanged;
    
    private static readonly List<string> DefaultOrder = new()
    {
        "Clipboard", "System", "Calendar", "Weather", "Pomodoro", "Notes", "Battery", "Network"
    };
    
    static WidgetSettings()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "OmniShell");
        Directory.CreateDirectory(folder);
        SettingsPath = Path.Combine(folder, "widget_settings.json");
        Load();
    }
    
    public static bool IsVisible(string widgetName)
    {
        return !_visibility.TryGetValue(widgetName, out var visible) || visible;
    }
    
    public static void SetVisible(string widgetName, bool visible)
    {
        _visibility[widgetName] = visible;
        Save();
    }
    
    public static string GetWeatherLocation()
    {
        return string.IsNullOrWhiteSpace(_weatherLocation) ? "" : _weatherLocation;
    }
    
    public static void SetWeatherLocation(string location)
    {
        _weatherLocation = location;
        Save();
        WeatherLocationChanged?.Invoke(null, EventArgs.Empty);
    }
    
    // Pomodoro state persistence
    private static int _pomodoroSessionCount = 0;
    private static double _pomodoroTimeRemaining = 25 * 60; // 25 minutes in seconds
    private static bool _pomodoroIsRunning = false;
    private static bool _pomodoroIsFocusMode = true;
    
    public static int GetPomodoroSessionCount()
    {
        return _pomodoroSessionCount;
    }
    
    public static void SetPomodoroSessionCount(int count)
    {
        _pomodoroSessionCount = count;
        Save();
    }
    
    public static double GetPomodoroTimeRemaining()
    {
        return _pomodoroTimeRemaining;
    }
    
    public static bool GetPomodoroIsRunning()
    {
        return _pomodoroIsRunning;
    }
    
    public static bool GetPomodoroIsFocusMode()
    {
        return _pomodoroIsFocusMode;
    }
    
    public static void SetPomodoroState(double timeRemaining, bool isRunning, bool isFocusMode)
    {
        _pomodoroTimeRemaining = timeRemaining;
        _pomodoroIsRunning = isRunning;
        _pomodoroIsFocusMode = isFocusMode;
        Save();
    }
    
    public static List<string> GetWidgetOrder()
    {
        return new List<string>(_widgetOrder);
    }
    
    public static void MoveWidget(string widgetName, int direction)
    {
        var index = _widgetOrder.IndexOf(widgetName);
        if (index < 0) return;
        
        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= _widgetOrder.Count) return;
        
        // Swap
        (_widgetOrder[index], _widgetOrder[newIndex]) = (_widgetOrder[newIndex], _widgetOrder[index]);
        Save();
        WidgetOrderChanged?.Invoke(null, EventArgs.Empty);
    }
    
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
                    _visibility = data.Visibility ?? new();
                    _weatherLocation = data.WeatherLocation ?? "";
                    if (data.WidgetOrder != null && data.WidgetOrder.Count > 0)
                    {
                        _widgetOrder = data.WidgetOrder;
                    }
                    
                    // Load Pomodoro state
                    _pomodoroSessionCount = data.PomodoroSessionCount;
                    _pomodoroTimeRemaining = data.PomodoroTimeRemaining;
                    _pomodoroIsRunning = data.PomodoroIsRunning;
                    _pomodoroIsFocusMode = data.PomodoroIsFocusMode;
                }
            }
        }
        catch { }
    }
    
    private static void Save()
    {
        try
        {
            var data = new SettingsData
            {
                Visibility = _visibility,
                WeatherLocation = _weatherLocation,
                WidgetOrder = _widgetOrder,
                PomodoroSessionCount = _pomodoroSessionCount,
                PomodoroTimeRemaining = _pomodoroTimeRemaining,
                PomodoroIsRunning = _pomodoroIsRunning,
                PomodoroIsFocusMode = _pomodoroIsFocusMode
            };
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
    
    private class SettingsData
    {
        public Dictionary<string, bool>? Visibility { get; set; }
        public string? WeatherLocation { get; set; }
        public List<string>? WidgetOrder { get; set; }
        public int PomodoroSessionCount { get; set; }
        public double PomodoroTimeRemaining { get; set; } = 25 * 60;
        public bool PomodoroIsRunning { get; set; }
        public bool PomodoroIsFocusMode { get; set; } = true;
    }
}
