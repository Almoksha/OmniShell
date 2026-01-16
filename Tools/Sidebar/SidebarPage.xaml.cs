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
using WpfEllipse = System.Windows.Shapes.Ellipse;
using WpfContentPresenter = System.Windows.Controls.ContentPresenter;
using WpfStyle = System.Windows.Style;
using WpfSetter = System.Windows.Setter;
using WpfTrigger = System.Windows.Trigger;
using WpfControlTemplate = System.Windows.Controls.ControlTemplate;
using WpfFrameworkElementFactory = System.Windows.FrameworkElementFactory;
using WpfTemplateBindingExtension = System.Windows.TemplateBindingExtension;

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
        Background = new WpfSolidColorBrush(WpfColor.FromRgb(0x1E, 0x1E, 0x38)), // Slightly darker, more modern
        CornerRadius = new CornerRadius(10),
        Padding = new Thickness(16, 14, 16, 14), // More generous padding
        Margin = new Thickness(0, 0, 0, 10),
        BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(0x37, 0x41, 0x51)),
        BorderThickness = new Thickness(1),
        Cursor = Cursors.Hand
    };
    
    // Add hover effect
    border.MouseEnter += (s, e) =>
    {
        border.Background = new WpfSolidColorBrush(WpfColor.FromRgb(0x2A, 0x2A, 0x4E));
        border.BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(0x63, 0x66, 0xF1));
    };
    border.MouseLeave += (s, e) =>
    {
        border.Background = new WpfSolidColorBrush(WpfColor.FromRgb(0x1E, 0x1E, 0x38));
        border.BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(0x37, 0x41, 0x51));
    };
    
    var grid = new WpfGrid();
    grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });
    grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });
    grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
    grid.ColumnDefinitions.Add(new WpfColumnDefinition { Width = GridLength.Auto });
    
    // Move buttons stack - improved styling
    var moveStack = new WpfStackPanel 
    { 
        Orientation = WpfOrientation.Vertical, 
        Margin = new Thickness(0, 0, 12, 0),
        VerticalAlignment = VerticalAlignment.Center
    };
    
    var upBtn = new WpfButton 
    { 
        Content = "▲", 
        FontSize = 10,
        Width = 24, Height = 20,
        Background = new WpfSolidColorBrush(WpfColor.FromRgb(0x37, 0x41, 0x51)),
        Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(0x9C, 0xA3, 0xAF)),
        BorderThickness = new Thickness(0),
        Cursor = Cursors.Hand,
        IsEnabled = index > 0,
        Tag = widgetName,
        Margin = new Thickness(0, 0, 0, 4)
    };
    upBtn.Style = CreateModernButtonStyle();
    upBtn.Click += (s, e) => MoveWidgetUp(widgetName);
    
    var downBtn = new WpfButton 
    { 
        Content = "▼", 
        FontSize = 10,
        Width = 24, Height = 20,
        Background = new WpfSolidColorBrush(WpfColor.FromRgb(0x37, 0x41, 0x51)),
        Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(0x9C, 0xA3, 0xAF)),
        BorderThickness = new Thickness(0),
        Cursor = Cursors.Hand,
        IsEnabled = index < total - 1,
        Tag = widgetName
    };
    downBtn.Style = CreateModernButtonStyle();
    downBtn.Click += (s, e) => MoveWidgetDown(widgetName);
    
    moveStack.Children.Add(upBtn);
    moveStack.Children.Add(downBtn);
    WpfGrid.SetColumn(moveStack, 0);
    grid.Children.Add(moveStack);
    
    // Icon background circle
    var iconBorder = new WpfBorder
    {
        Width = 40,
        Height = 40,
        CornerRadius = new CornerRadius(20),
        Background = new WpfSolidColorBrush(WpfColor.FromRgb(0x37, 0x41, 0x51)),
        Margin = new Thickness(0, 0, 14, 0),
        VerticalAlignment = VerticalAlignment.Center
    };
    
    var iconText = new WpfTextBlock
    {
        Text = icon,
        FontSize = 22,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        // Ensure emoji colors are preserved
        Foreground = WpfBrushes.White
    };
    
    // Use TextElement.SetForeground to ensure emojis display in color
    iconBorder.Child = iconText;
    WpfGrid.SetColumn(iconBorder, 1);
    grid.Children.Add(iconBorder);
    
    // Name with better typography
    var nameText = new WpfTextBlock
    {
        Text = displayName,
        FontSize = 14,
        FontWeight = FontWeights.Medium,
        Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(0xFF, 0xFF, 0xFF)),
        VerticalAlignment = VerticalAlignment.Center
    };
    WpfGrid.SetColumn(nameText, 2);
    grid.Children.Add(nameText);
    
    // Custom styled toggle
    var toggleBorder = new WpfBorder
    {
        Width = 48,
        Height = 26,
        CornerRadius = new CornerRadius(13),
        Background = WidgetSettings.IsVisible(widgetName) 
            ? new WpfSolidColorBrush(WpfColor.FromRgb(0x63, 0x66, 0xF1))
            : new WpfSolidColorBrush(WpfColor.FromRgb(0x4B, 0x55, 0x63)),
        Cursor = Cursors.Hand,
        Tag = widgetName,
        Padding = new Thickness(2),
        VerticalAlignment = VerticalAlignment.Center
    };
    
    var toggleKnob = new WpfEllipse
    {
        Width = 22,
        Height = 22,
        Fill = WpfBrushes.White,
        HorizontalAlignment = WidgetSettings.IsVisible(widgetName) 
            ? HorizontalAlignment.Right 
            : HorizontalAlignment.Left
    };
    
    toggleBorder.Child = toggleKnob;
    
    toggleBorder.MouseLeftButtonUp += (s, e) =>
    {
        bool isVisible = WidgetSettings.IsVisible(widgetName);
        bool newState = !isVisible;
        
        WidgetSettings.SetVisible(widgetName, newState);
        toggleBorder.Background = newState 
            ? new WpfSolidColorBrush(WpfColor.FromRgb(0x63, 0x66, 0xF1))
            : new WpfSolidColorBrush(WpfColor.FromRgb(0x4B, 0x55, 0x63));
        toggleKnob.HorizontalAlignment = newState 
            ? HorizontalAlignment.Right 
            : HorizontalAlignment.Left;
        
        OnWidgetToggle(widgetName, newState);
    };
    
    WpfGrid.SetColumn(toggleBorder, 3);
    grid.Children.Add(toggleBorder);
    
    border.Child = grid;
    return border;
}

private WpfStyle CreateModernButtonStyle()
{
    var style = new WpfStyle(typeof(WpfButton));
    
    var template = new WpfControlTemplate(typeof(WpfButton));
    var factory = new WpfFrameworkElementFactory(typeof(WpfBorder));
    factory.Name = "border";
    factory.SetValue(WpfBorder.BackgroundProperty, new WpfTemplateBindingExtension(WpfButton.BackgroundProperty));
    factory.SetValue(WpfBorder.CornerRadiusProperty, new CornerRadius(6));
    factory.SetValue(WpfBorder.PaddingProperty, new WpfTemplateBindingExtension(WpfButton.PaddingProperty));
    
    var contentFactory = new WpfFrameworkElementFactory(typeof(WpfContentPresenter));
    contentFactory.SetValue(WpfContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
    contentFactory.SetValue(WpfContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
    factory.AppendChild(contentFactory);
    
    template.VisualTree = factory;
    
    // Hover trigger
    var hoverTrigger = new WpfTrigger { Property = WpfButton.IsMouseOverProperty, Value = true };
    hoverTrigger.Setters.Add(new WpfSetter(WpfButton.BackgroundProperty, 
        new WpfSolidColorBrush(WpfColor.FromRgb(0x4B, 0x55, 0x63)))); // Apply to the button itself
    template.Triggers.Add(hoverTrigger);
    
    style.Setters.Add(new WpfSetter(WpfButton.TemplateProperty, template));
    
    return style;
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
            FloatingCheckmark.Visibility = Visibility.Visible;
            
            DockedModeCard.Background = WpfBrushes.Transparent;
            DockedModeCard.BorderThickness = new Thickness(1);
            DockedCheckmark.Visibility = Visibility.Collapsed;
        }
        else
        {
            DockedModeCard.Background = new WpfSolidColorBrush(
                WpfColor.FromArgb(0x1A, 0x63, 0x66, 0xF1));
            DockedModeCard.BorderThickness = new Thickness(0);
            DockedCheckmark.Visibility = Visibility.Visible;
            
            FloatingModeCard.Background = WpfBrushes.Transparent;
            FloatingModeCard.BorderThickness = new Thickness(1);
            FloatingCheckmark.Visibility = Visibility.Collapsed;
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
