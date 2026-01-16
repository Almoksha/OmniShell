using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using OmniShell.Interop;
using OmniShell.Services;

namespace OmniShell.Views;

/// <summary>
/// Sidebar display mode
/// </summary>
public enum SidebarMode
{
    Floating,   // Overlay - doesn't reserve screen space
    Docked      // AppBar - reserves screen space
}

/// <summary>
/// Sidebar window with dual mode support (Floating/Docked)
/// Emergency shortcut: Ctrl+Shift+Escape to force close
/// </summary>
public partial class SidebarWindow : Window
{
    private AppBarService? _appBarService;
    private readonly ClipboardService _clipboardService;
    private readonly SystemMonitorService _systemMonitorService;
    private bool _isInitialized;
    private const int SidebarWidth = 320;
    private const int CompactWidth = 60;
    
    public SidebarMode Mode { get; set; } = SidebarMode.Floating;
    public bool IsCompact { get; private set; } = false;
    public bool IsAppBarActive => _appBarService?.IsRegistered == true;

    public SidebarWindow()
    {
        InitializeComponent();

        _clipboardService = new ClipboardService();
        _systemMonitorService = new SystemMonitorService();

        Loaded += SidebarWindow_Loaded;
        Closing += SidebarWindow_Closing;
        
        // Emergency keyboard shortcut
        KeyDown += SidebarWindow_KeyDown;
    }

    private void SidebarWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        var hwnd = new WindowInteropHelper(this).Handle;

        // Hide from taskbar and Alt+Tab
        int exStyle = User32.GetWindowLong(hwnd, User32.GWL_EXSTYLE);
        exStyle |= User32.WS_EX_TOOLWINDOW;
        exStyle &= ~User32.WS_EX_APPWINDOW;
        User32.SetWindowLong(hwnd, User32.GWL_EXSTYLE, exStyle);

        // Apply acrylic/mica effect on Windows 11
        try
        {
            Dwmapi.SetDarkMode(hwnd, true);
            Dwmapi.ApplyAcrylic(hwnd);
        }
        catch
        {
            // Ignore if effects aren't supported
        }

        // NOTE: Don't call ApplyMode() here - ShowSidebar() handles it after Show()

        // Initialize clipboard monitoring
        _clipboardService.Initialize(hwnd);
        _clipboardService.ClipboardChanged += ClipboardService_ClipboardChanged;
        ClipboardItems.ItemsSource = _clipboardService.History;
        UpdateClipboardEmptyState();

        // Initialize system monitoring
        _systemMonitorService.MetricsUpdated += SystemMonitorService_MetricsUpdated;
        _systemMonitorService.Start();
        
        // Apply saved widget visibility settings
        ApplyWidgetSettings();
        
        // Subscribe to Pomodoro timer updates for compact view
        if (PomodoroWidgetContainer != null)
        {
            PomodoroWidgetContainer.TimerUpdated += (s, e) =>
            {
                if (CompactTimer != null)
                    CompactTimer.Text = PomodoroWidgetContainer.TimeRemainingText;
            };
        }
        
        // Subscribe to Battery updates for compact view
        if (BatteryWidgetContainer != null)
        {
            BatteryWidgetContainer.BatteryUpdated += (s, e) =>
            {
                if (CompactBattery != null)
                    CompactBattery.Text = $"{BatteryWidgetContainer.BatteryPercentage}%";
            };
        }
        
        // Subscribe to Weather updates for compact view
        if (WeatherWidgetContainer != null)
        {
            WeatherWidgetContainer.WeatherUpdated += (s, e) =>
            {
                if (CompactTemp != null && WeatherWidgetContainer.CurrentTemperature != null)
                    CompactTemp.Text = $"{WeatherWidgetContainer.CurrentTemperature}°";
            };
        }

        // Add message hook for clipboard
        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);

        // NOTE: Don't call PlayEntranceAnimation() here - ShowSidebar() handles it
    }

    /// <summary>
    /// Emergency shortcut handler: Ctrl+Shift+Escape to force close
    /// </summary>
    private void SidebarWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape && 
            Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            ForceClose();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Force close the sidebar and unregister AppBar
    /// </summary>
    public void ForceClose()
    {
        try
        {
            _appBarService?.Unregister();
            _appBarService?.Dispose();
            _appBarService = null;
        }
        catch { }
        
        Hide();
    }

    private void ApplyMode()
    {
        if (Mode == SidebarMode.Docked)
        {
            ApplyDockedMode();
        }
        else
        {
            ApplyFloatingMode();
        }
    }

    private void ApplyFloatingMode()
    {
        // Unregister AppBar if it was registered
        if (_appBarService != null)
        {
            _appBarService.Unregister();
            _appBarService.Dispose();
            _appBarService = null;
        }

        // Position on screen edge without reserving space
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - SidebarWidth;
        Top = workArea.Top;
        Width = SidebarWidth;
        Height = workArea.Height;
        
        Topmost = true;
    }

    private void ApplyDockedMode()
    {
        Topmost = false;
        
        // Create and register AppBar
        _appBarService = new AppBarService();
        _appBarService.Initialize(this);
        _appBarService.Edge = Shell32.ABE_RIGHT;
        _appBarService.PositionChanged += AppBarService_PositionChanged;

        if (_appBarService.Register(SidebarWidth))
        {
            UpdateWindowFromAppBar();
        }
        else
        {
            // Fallback to floating if AppBar fails
            Mode = SidebarMode.Floating;
            ApplyFloatingMode();
        }
    }

    private void AppBarService_PositionChanged(object? sender, EventArgs e)
    {
        UpdateWindowFromAppBar();
    }

    private void UpdateWindowFromAppBar()
    {
        if (_appBarService == null) return;
        
        var rect = _appBarService.GetReservedRect();
        
        // Debug output
        System.Diagnostics.Debug.WriteLine($"[SidebarWindow] AppBar rect: Left={rect.Left}, Top={rect.Top}, Width={rect.Width}, Height={rect.Height}");
        
        // Validate rect - if invalid, something went wrong
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            System.Diagnostics.Debug.WriteLine("[SidebarWindow] Invalid AppBar rect! Falling back to floating.");
            Mode = SidebarMode.Floating;
            ApplyFloatingMode();
            return;
        }
        
        Left = rect.Left;
        Top = rect.Top;
        Width = rect.Width;
        Height = rect.Height;
        
        // Ensure visibility and bring to front
        Visibility = Visibility.Visible;
        Opacity = 1;
        
        // Force window to front
        Activate();
        Focus();
        
        System.Diagnostics.Debug.WriteLine($"[SidebarWindow] Window positioned at: Left={Left}, Top={Top}, Width={Width}, Height={Height}");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == User32.WM_CLIPBOARDUPDATE)
        {
            _clipboardService.ProcessClipboardChange();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void ClipboardService_ClipboardChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateClipboardEmptyState();
        });
    }

    private void UpdateClipboardEmptyState()
    {
        ClipboardEmptyState.Visibility = _clipboardService.History.Count == 0 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }

    private void SystemMonitorService_MetricsUpdated(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            // Update CPU
            CpuPercentText.Text = $"{_systemMonitorService.CpuUsage:F0}%";
            var cpuWidth = (ResourceWidget.ActualWidth - 24) * (_systemMonitorService.CpuUsage / 100.0);
            AnimateProgressBar(CpuProgressBar, Math.Max(0, cpuWidth));

            // Update RAM
            RamPercentText.Text = $"{_systemMonitorService.RamUsedGB:F1} / {_systemMonitorService.RamTotalGB:F0} GB";
            var ramWidth = (ResourceWidget.ActualWidth - 24) * (_systemMonitorService.RamUsagePercent / 100.0);
            AnimateProgressBar(RamProgressBar, Math.Max(0, ramWidth));
            
            // Update compact view CPU
            if (CompactCpu != null)
                CompactCpu.Text = $"{_systemMonitorService.CpuUsage:F0}%";
        });
    }

    private void AnimateProgressBar(FrameworkElement bar, double targetWidth)
    {
        var animation = new DoubleAnimation
        {
            To = targetWidth,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        bar.BeginAnimation(WidthProperty, animation);
    }

    private void PlayEntranceAnimation()
    {
        // For docked mode, no animation - just ensure visible
        if (Mode == SidebarMode.Docked)
        {
            System.Diagnostics.Debug.WriteLine("[SidebarWindow] Docked mode - no animation, setting opacity to 1");
            Opacity = 1;
            Visibility = Visibility.Visible;
            return;
        }
        
        // Floating mode - full slide-in animation
        var targetLeft = SystemParameters.WorkArea.Right - SidebarWidth;
        var startLeft = targetLeft + 50;
        Left = startLeft;
        
        var animation = new DoubleAnimation
        {
            From = startLeft,
            To = targetLeft,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(LeftProperty, animation);

        Opacity = 0;
        var floatOpacityAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(200)
        };
        BeginAnimation(OpacityProperty, floatOpacityAnimation);
    }

    private void CollapseButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleCompact();
    }
    
    /// <summary>
    /// Toggle between expanded and compact mode
    /// </summary>
    public void ToggleCompact()
    {
        IsCompact = !IsCompact;
        
        var targetWidth = IsCompact ? CompactWidth : SidebarWidth;
        
        // Update compact view elements
        UpdateCompactView();
        
        if (Mode == SidebarMode.Docked && _appBarService != null)
        {
            // Docked mode: instant switch (no animation) to avoid AppBar glitches
            BeginAnimation(WidthProperty, null);
            BeginAnimation(LeftProperty, null);
            
            _appBarService.Unregister();
            Width = targetWidth;
            _appBarService.Register(targetWidth);
            UpdateWindowFromAppBar();
        }
        else
        {
            // Floating mode: smooth animation
            var workArea = SystemParameters.WorkArea;
            var targetLeft = workArea.Right - targetWidth;
            
            var widthAnimation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var leftAnimation = new DoubleAnimation
            {
                To = targetLeft,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            BeginAnimation(LeftProperty, leftAnimation);
            BeginAnimation(WidthProperty, widthAnimation);
        }
    }
    
    private void UpdateCompactView()
    {
        // Update visibility of expanded/compact content
        var expandedVisibility = IsCompact ? Visibility.Collapsed : Visibility.Visible;
        var compactVisibility = IsCompact ? Visibility.Visible : Visibility.Collapsed;
        
        // Main content area
        if (FindName("WidgetsScroller") is System.Windows.Controls.ScrollViewer scroller)
        {
            scroller.Visibility = expandedVisibility;
        }
        
        // Compact view
        if (FindName("CompactView") is FrameworkElement compact)
        {
            compact.Visibility = compactVisibility;
        }
        
        // Update collapse button icon direction
        // Left chevron (E76B) when compact (to expand), Right chevron (E76C) when expanded (to collapse)
        if (CollapseButton?.Template?.FindName("CollapseIcon", CollapseButton) is System.Windows.Controls.TextBlock icon)
        {
            icon.Text = IsCompact ? "\uE76B" : "\uE76C";
        }
        
        // Close popup if open
        if (FindName("WidgetPopup") is System.Windows.Controls.Primitives.Popup popup)
        {
            popup.IsOpen = false;
        }
    }
    
    private void CompactIcon_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string widgetName)
            return;
            
        var popup = FindName("WidgetPopup") as System.Windows.Controls.Primitives.Popup;
        var content = FindName("PopupContent") as System.Windows.Controls.Grid;
        
        if (popup == null || content == null)
            return;
        
        // Clear previous content (but don't destroy the widgets)
        content.Children.Clear();
        
        // Get existing widget instance or create simple views
        FrameworkElement? widget = widgetName switch
        {
            "Calendar" => CalendarWidgetContainer,
            "Weather" => WeatherWidgetContainer,
            "Pomodoro" => PomodoroWidgetContainer,
            "Notes" => NotesWidgetContainer,
            "Battery" => BatteryWidgetContainer,
            "Network" => NetworkWidgetContainer,
            "System" => CreateSystemWidgetForPopup(),
            "Clipboard" => CreateClipboardWidgetForPopup(),
            _ => null
        };
        
        if (widget != null)
        {
            // If it's an existing widget, temporarily move it to popup
            if (widget.Parent is System.Windows.Controls.Panel parent)
            {
                parent.Children.Remove(widget);
            }
            content.Children.Add(widget);
            
            // Restore widget when popup closes
            popup.Closed -= RestoreWidgetToSidebar;
            popup.Closed += RestoreWidgetToSidebar;
        }
        
        // Position popup relative to clicked element
        popup.PlacementTarget = element;
        popup.HorizontalOffset = -10;
        popup.IsOpen = true;
    }
    
    private void RestoreWidgetToSidebar(object? sender, EventArgs e)
    {
        var popup = FindName("WidgetPopup") as System.Windows.Controls.Primitives.Popup;
        var content = FindName("PopupContent") as System.Windows.Controls.Grid;
        var widgetsPanel = FindName("WidgetsScroller") as System.Windows.Controls.ScrollViewer;
        
        if (content == null || widgetsPanel == null)
            return;
        
        // Get the widget from popup
        if (content.Children.Count > 0 && content.Children[0] is FrameworkElement widget)
        {
            content.Children.Clear();
            
            // Only restore sidebar widgets (not the dynamically created ones)
            if (widget is OmniShell.Views.Widgets.CalendarWidget or 
                OmniShell.Views.Widgets.WeatherWidget or
                OmniShell.Views.Widgets.PomodoroWidget or
                OmniShell.Views.Widgets.NotesWidget or
                OmniShell.Views.Widgets.BatteryWidget or
                OmniShell.Views.Widgets.NetworkWidget)
            {
                // Find the widgets container StackPanel inside the ScrollViewer
                if (widgetsPanel.Content is System.Windows.Controls.StackPanel stack)
                {
                    // Add widget back (ApplyWidgetOrder will fix the position)
                    stack.Children.Add(widget);
                }
                
                // Re-apply the correct widget order
                ApplyWidgetOrder();
            }
        }
    }
    
    private FrameworkElement CreateSystemWidgetForPopup()
    {
        var border = new System.Windows.Controls.Border
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x1E, 0x38)),
            Padding = new Thickness(16)
        };
        
        var stack = new System.Windows.Controls.StackPanel();
        
        // Header
        var header = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
        header.Children.Add(new System.Windows.Controls.TextBlock { Text = "💻", FontSize = 18, Margin = new Thickness(0, 0, 8, 0) });
        header.Children.Add(new System.Windows.Controls.TextBlock { Text = "System", FontWeight = FontWeights.SemiBold, FontSize = 16, Foreground = System.Windows.Media.Brushes.White });
        stack.Children.Add(header);
        
        // Get current metrics from service
        var cpuText = $"CPU: {_systemMonitorService.CpuUsage:F0}%";
        var ramText = $"RAM: {_systemMonitorService.RamUsedGB:F1} / {_systemMonitorService.RamTotalGB:F1} GB";
        
        // CPU
        var cpuStack = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        cpuStack.Children.Add(new System.Windows.Controls.TextBlock { Text = cpuText, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x06, 0xB6, 0xD4)) });
        stack.Children.Add(cpuStack);
        
        // RAM
        var ramStack = new System.Windows.Controls.StackPanel();
        ramStack.Children.Add(new System.Windows.Controls.TextBlock { Text = ramText, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD9, 0x46, 0xEF)) });
        stack.Children.Add(ramStack);
        
        border.Child = stack;
        return border;
    }
    
    private FrameworkElement CreateClipboardWidgetForPopup()
    {
        var border = new System.Windows.Controls.Border
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x1E, 0x38)),
            Padding = new Thickness(16),
            MaxHeight = 350
        };
        
        var stack = new System.Windows.Controls.StackPanel();
        
        // Header
        var header = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(new System.Windows.Controls.TextBlock { Text = "📋", FontSize = 18, Margin = new Thickness(0, 0, 8, 0) });
        header.Children.Add(new System.Windows.Controls.TextBlock { Text = "Clipboard", FontWeight = FontWeights.SemiBold, FontSize = 16, Foreground = System.Windows.Media.Brushes.White });
        stack.Children.Add(header);
        
        // Show last 5 items 
        foreach (var item in _clipboardService.History.Take(5))
        {
            var itemBorder = new System.Windows.Controls.Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x26, 0x26, 0x40)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            
            var text = new System.Windows.Controls.TextBlock
            {
                Text = item.Preview,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x9C, 0xA3, 0xAF)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 250
            };
            
            itemBorder.Child = text;
            itemBorder.MouseLeftButtonUp += (s, e) => _clipboardService.CopyToClipboard(item);
            stack.Children.Add(itemBorder);
        }
        
        border.Child = stack;
        return border;
    }
    
    /// <summary>
    /// Show or hide a specific widget by name
    /// </summary>
    public void SetWidgetVisibility(string widgetName, bool visible)
    {
        var visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        
        FrameworkElement? widget = widgetName switch
        {
            "Clipboard" => ClipboardWidget,
            "System" => ResourceWidget,
            "Calendar" => FindName("CalendarWidgetContainer") as FrameworkElement,
            "Weather" => FindName("WeatherWidgetContainer") as FrameworkElement,
            "Pomodoro" => FindName("PomodoroWidgetContainer") as FrameworkElement,
            "Notes" => FindName("NotesWidgetContainer") as FrameworkElement,
            "Battery" => FindName("BatteryWidgetContainer") as FrameworkElement,
            "Network" => FindName("NetworkWidgetContainer") as FrameworkElement,
            _ => null
        };
        
        if (widget != null)
        {
            widget.Visibility = visibility;
        }
    }
    
    /// <summary>
    /// Apply all widget visibility settings from saved preferences
    /// </summary>
    public void ApplyWidgetSettings()
    {
        SetWidgetVisibility("Clipboard", Services.WidgetSettings.IsVisible("Clipboard"));
        SetWidgetVisibility("System", Services.WidgetSettings.IsVisible("System"));
        SetWidgetVisibility("Calendar", Services.WidgetSettings.IsVisible("Calendar"));
        SetWidgetVisibility("Weather", Services.WidgetSettings.IsVisible("Weather"));
        SetWidgetVisibility("Pomodoro", Services.WidgetSettings.IsVisible("Pomodoro"));
        SetWidgetVisibility("Notes", Services.WidgetSettings.IsVisible("Notes"));
        SetWidgetVisibility("Battery", Services.WidgetSettings.IsVisible("Battery"));
        SetWidgetVisibility("Network", Services.WidgetSettings.IsVisible("Network"));
        
        ApplyWidgetOrder();
    }
    
    /// <summary>
    /// Reorder widgets in the sidebar based on saved order
    /// </summary>
    public void ApplyWidgetOrder()
    {
        // Find the widgets container StackPanel
        var scroller = FindName("WidgetsScroller") as System.Windows.Controls.ScrollViewer;
        if (scroller?.Content is not System.Windows.Controls.StackPanel widgetsPanel)
            return;
        
        var order = Services.WidgetSettings.GetWidgetOrder();
        
        // Map widget names to their container elements (expanded view)
        var widgetElements = new Dictionary<string, FrameworkElement?>
        {
            ["Clipboard"] = ClipboardWidget,
            ["System"] = ResourceWidget,
            ["Calendar"] = CalendarWidgetContainer,
            ["Weather"] = WeatherWidgetContainer,
            ["Pomodoro"] = PomodoroWidgetContainer,
            ["Notes"] = NotesWidgetContainer,
            ["Battery"] = BatteryWidgetContainer,
            ["Network"] = NetworkWidgetContainer
        };
        
        // Remove all widgets from panel
        var existingWidgets = new List<FrameworkElement>();
        foreach (var kvp in widgetElements)
        {
            if (kvp.Value != null && widgetsPanel.Children.Contains(kvp.Value))
            {
                existingWidgets.Add(kvp.Value);
                widgetsPanel.Children.Remove(kvp.Value);
            }
        }
        
        // Re-add in the correct order (after the header which should stay at index 0-1)
        foreach (var widgetName in order)
        {
            if (widgetElements.TryGetValue(widgetName, out var widget) && widget != null)
            {
                widgetsPanel.Children.Add(widget);
            }
        }
        
        // Also reorder compact view icons
        ApplyCompactViewOrder(order);
    }
    
    /// <summary>
    /// Reorder compact view icons to match widget order
    /// </summary>
    private void ApplyCompactViewOrder(List<string> order)
    {
        var compactPanel = FindName("CompactIconsPanel") as System.Windows.Controls.StackPanel;
        if (compactPanel == null) return;
        
        // Map widget names to their compact view icons
        var compactIcons = new Dictionary<string, FrameworkElement?>
        {
            ["Clipboard"] = CompactClipboardBtn,
            ["System"] = CompactSystemBtn,
            ["Calendar"] = CompactCalendarBtn,
            ["Weather"] = CompactWeatherBtn,
            ["Pomodoro"] = CompactPomodoroBtn,
            ["Notes"] = CompactNotesBtn,
            ["Battery"] = CompactBatteryBtn,
            ["Network"] = CompactNetworkBtn
        };
        
        // Remove all compact icons from panel
        foreach (var kvp in compactIcons)
        {
            if (kvp.Value != null && compactPanel.Children.Contains(kvp.Value))
            {
                compactPanel.Children.Remove(kvp.Value);
            }
        }
        
        // Re-add in the correct order
        foreach (var widgetName in order)
        {
            if (compactIcons.TryGetValue(widgetName, out var icon) && icon != null)
            {
                compactPanel.Children.Add(icon);
            }
        }
    }

    private void ClearClipboardButton_Click(object sender, RoutedEventArgs e)
    {
        _clipboardService.ClearHistory();
        UpdateClipboardEmptyState();
    }

    private void ClipboardItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ClipboardItem item)
        {
            _clipboardService.CopyToClipboard(item);
        }
    }

    private void SidebarWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _systemMonitorService.Stop();
        _clipboardService.Dispose();
        _appBarService?.Dispose();
    }

    public void ShowSidebar()
    {
        // For Docked mode, we need the window handle first
        // So show window with temporary position, then apply AppBar
        if (Mode == SidebarMode.Docked)
        {
            // Position temporarily before showing
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - SidebarWidth;
            Top = workArea.Top;
            Width = SidebarWidth;
            Height = workArea.Height;
        }
        
        Show();
        Activate();
        
        // Now apply mode - window handle exists
        ApplyMode();
        
        PlayEntranceAnimation();
    }

    public void HideSidebar()
    {
        // IMPORTANT: Unregister AppBar when hiding to release screen space
        if (_appBarService != null)
        {
            _appBarService.Unregister();
            _appBarService.Dispose();
            _appBarService = null;
        }
        
        Hide();
    }

    /// <summary>
    /// Set the sidebar mode and reapply if visible
    /// </summary>
    public void SetMode(SidebarMode mode)
    {
        if (Mode == mode) return;
        Mode = mode;
        
        if (IsVisible)
        {
            ApplyMode();
        }
    }
}
