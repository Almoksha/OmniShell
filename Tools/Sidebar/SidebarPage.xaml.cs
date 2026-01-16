using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using OmniShell.Views;
using OmniShell.Services;

// Explicitly use WPF controls
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfBorder = System.Windows.Controls.Border;
using WpfGrid = System.Windows.Controls.Grid;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfColumnDefinition = System.Windows.Controls.ColumnDefinition;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfPage = System.Windows.Controls.Page;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace OmniShell.Tools.Sidebar;

/// <summary>
/// Page for configuring and controlling the Quick Access Sidebar.
/// </summary>
public partial class SidebarPage : System.Windows.Controls.Page
{
    private static readonly Dictionary<string, (string Icon, string Name)> WidgetInfo = new()
    {
        ["Clipboard"] = ("📋", "Clipboard Manager"),
        ["System"] = ("💻", "System Monitor"),
        ["Calendar"] = ("📅", "Calendar"),
        ["Weather"] = ("🌤️", "Weather"),
        ["Pomodoro"] = ("⏰", "Pomodoro Timer"),
        ["Notes"] = ("📝", "Quick Notes"),
        ["Battery"] = ("🔋", "Battery Status"),
        ["Network"] = ("📊", "Network Monitor")
    };
    
    public SidebarPage()
    {
        InitializeComponent();
        Loaded += SidebarPage_Loaded;
    }

    private void SidebarPage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateButtonState();
        UpdateModeCardStyles();
        BuildWidgetList();
        InitializeWeatherLocation();
    }
    
    private void InitializeWeatherLocation()
    {
        var savedLocation = WidgetSettings.GetWeatherLocation();
        WeatherLocationInput.Text = string.IsNullOrEmpty(savedLocation) ? "Current Location" : savedLocation;
        WeatherLocationInput.LostFocus += (s, e) => 
        {
            var location = WeatherLocationInput.Text.Trim();
            if (location.Equals("Current Location", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(location))
            {
                WidgetSettings.SetWeatherLocation("");
                WeatherLocationInput.Text = "Current Location";
            }
            else
            {
                WidgetSettings.SetWeatherLocation(location);
            }
        };
    }
    
    private void UseCurrentLocation_Click(object sender, RoutedEventArgs e)
    {
        WeatherLocationInput.Text = "Current Location";
        WidgetSettings.SetWeatherLocation("");
    }
    
    private void BuildWidgetList()
    {
        WidgetListPanel.Children.Clear();
        var order = WidgetSettings.GetWidgetOrder();
        
        for (int i = 0; i < order.Count; i++)
        {
            var widgetName = order[i];
            if (!WidgetInfo.TryGetValue(widgetName, out var info))
                continue;
                
            var row = CreateWidgetRow(widgetName, info.Icon, info.Name, i, order.Count);
            WidgetListPanel.Children.Add(row);
        }
    }
    
    private FrameworkElement CreateWidgetRow(string widgetName, string icon, string displayName, int index, int total)
    {
        var border = new WpfBorder
        {
            Background = new WpfSolidColorBrush(WpfColor.FromRgb(0x26, 0x26, 0x40)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8)
        };
        
        var grid = new WpfGrid();
        grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });
        
        // Move buttons
        var moveStack = new WpfStackPanel { Orientation = WpfOrientation.Vertical, Margin = new Thickness(0, 0, 8, 0) };
        
        var upBtn = new WpfButton 
        { 
            Content = "▲", 
            FontSize = 8,
            Width = 20, Height = 14,
            Background = WpfBrushes.Transparent,
            Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(0x9C, 0xA3, 0xAF)),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            IsEnabled = index > 0,
            Tag = widgetName
        };
        upBtn.Click += (s, e) => MoveWidgetUp(widgetName);
        
        var downBtn = new WpfButton 
        { 
            Content = "▼", 
            FontSize = 8,
            Width = 20, Height = 14,
            Background = WpfBrushes.Transparent,
            Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(0x9C, 0xA3, 0xAF)),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            IsEnabled = index < total - 1,
            Tag = widgetName
        };
        downBtn.Click += (s, e) => MoveWidgetDown(widgetName);
        
        moveStack.Children.Add(upBtn);
        moveStack.Children.Add(downBtn);
        WpfGrid.SetColumn(moveStack, 0);
        grid.Children.Add(moveStack);
        
        // Icon
        var iconText = new WpfTextBlock
        {
            Text = icon,
            FontSize = 20,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };
        WpfGrid.SetColumn(iconText, 1);
        grid.Children.Add(iconText);
        
        // Name
        var nameText = new WpfTextBlock
        {
            Text = displayName,
            FontSize = 13,
            Foreground = WpfBrushes.White,
            VerticalAlignment = VerticalAlignment.Center
        };
        WpfGrid.SetColumn(nameText, 2);
        grid.Children.Add(nameText);
        
        // Toggle checkbox
        var toggle = new WpfCheckBox
        {
            IsChecked = WidgetSettings.IsVisible(widgetName),
            Tag = widgetName
        };
        toggle.Checked += (s, e) => OnWidgetToggle(widgetName, true);
        toggle.Unchecked += (s, e) => OnWidgetToggle(widgetName, false);
        WpfGrid.SetColumn(toggle, 4);
        grid.Children.Add(toggle);
        
        border.Child = grid;
        return border;
    }
    
    private void MoveWidgetUp(string widgetName)
    {
        WidgetSettings.MoveWidget(widgetName, -1);
        BuildWidgetList();
        ApplyWidgetOrderToSidebar();
    }
    
    private void MoveWidgetDown(string widgetName)
    {
        WidgetSettings.MoveWidget(widgetName, 1);
        BuildWidgetList();
        ApplyWidgetOrderToSidebar();
    }
    
    private void ApplyWidgetOrderToSidebar()
    {
        var mainWindow = MainWindow.Instance;
        mainWindow?.ApplyWidgetOrder();
    }
    
    private void OnWidgetToggle(string widgetName, bool visible)
    {
        WidgetSettings.SetVisible(widgetName, visible);
        
        // Update sidebar if visible
        var mainWindow = MainWindow.Instance;
        mainWindow?.UpdateWidgetVisibility(widgetName, visible);
    }

    private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = MainWindow.Instance;
        if (mainWindow == null) return;

        // Set the mode BEFORE toggling - this ensures new sidebar gets correct mode
        var mode = DockedModeRadio.IsChecked == true 
            ? SidebarMode.Docked 
            : SidebarMode.Floating;
        mainWindow.SetSidebarMode(mode);

        mainWindow.ToggleSidebar();
        UpdateButtonState();
    }

    private void FloatingMode_Click(object sender, MouseButtonEventArgs e)
    {
        FloatingModeRadio.IsChecked = true;
        UpdateModeCardStyles();
        ApplyModeToActiveSidebar();
    }

    private void DockedMode_Click(object sender, MouseButtonEventArgs e)
    {
        DockedModeRadio.IsChecked = true;
        UpdateModeCardStyles();
        ApplyModeToActiveSidebar();
    }

    private void UpdateModeCardStyles()
    {
        if (FloatingModeRadio.IsChecked == true)
        {
            FloatingModeCard.Background = new WpfSolidColorBrush(
                WpfColor.FromArgb(0x1A, 0x63, 0x66, 0xF1));
            FloatingModeCard.BorderThickness = new Thickness(0);
            
            DockedModeCard.Background = WpfBrushes.Transparent;
            DockedModeCard.BorderThickness = new Thickness(1);
        }
        else
        {
            DockedModeCard.Background = new WpfSolidColorBrush(
                WpfColor.FromArgb(0x1A, 0x63, 0x66, 0xF1));
            DockedModeCard.BorderThickness = new Thickness(0);
            
            FloatingModeCard.Background = WpfBrushes.Transparent;
            FloatingModeCard.BorderThickness = new Thickness(1);
        }
    }

    private void ApplyModeToActiveSidebar()
    {
        var mainWindow = MainWindow.Instance;
        if (mainWindow == null) return;

        var mode = DockedModeRadio.IsChecked == true 
            ? SidebarMode.Docked 
            : SidebarMode.Floating;

        mainWindow.SetSidebarMode(mode);
    }

    private void UpdateButtonState()
    {
        var mainWindow = MainWindow.Instance;
        bool isVisible = mainWindow?.IsSidebarVisible == true;

        ToggleSidebarButton.Content = isVisible ? "Hide Sidebar" : "Show Sidebar";
        
        StatusCard.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        
        var modeText = DockedModeRadio.IsChecked == true ? "docked" : "floating";
        StatusText.Text = isVisible 
            ? $"Sidebar is currently visible ({modeText} mode)" 
            : "";
    }
}
