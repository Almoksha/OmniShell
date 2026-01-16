using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OmniShell.Core;
using OmniShell.Services;
using OmniShell.Views;

namespace OmniShell;

/// <summary>
/// Main window with dynamic plugin-based navigation sidebar
/// </summary>
public partial class MainWindow : Window
{
    private readonly PluginLoader _pluginLoader;
    private SidebarWindow? _sidebarWindow;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    
    public static MainWindow? Instance { get; private set; }
    
    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        
        // Restore window position and size
        RestoreWindowState();
        
        // Initialize system tray icon
        InitializeTrayIcon();
        
        // Restore sidebar mode preference
        var savedMode = Services.AppSettings.GetSidebarMode();
        _pendingSidebarMode = savedMode == "Docked" ? Views.SidebarMode.Docked : Views.SidebarMode.Floating;
        
        // Restore sidebar visibility if it was visible on last run
        Loaded += (s, e) =>
        {
            if (Services.AppSettings.GetSidebarVisibleOnStartup())
            {
                ShowSidebar();
            }
        };
        
        // Initialize plugin loader and discover plugins
        _pluginLoader = new PluginLoader();
        _pluginLoader.DiscoverPlugins();
        
        // Bind plugins to navigation
        PluginNavItems.ItemsSource = _pluginLoader.Plugins;
        
        // Navigate to first plugin by default
        if (_pluginLoader.Plugins.Count > 0)
        {
            NavigateToPlugin(_pluginLoader.Plugins[0]);
            
            // Check the first radio button
            Loaded += (s, e) =>
            {
                var firstItem = PluginNavItems.ItemContainerGenerator.ContainerFromIndex(0);
                if (firstItem is ContentPresenter presenter)
                {
                    var radioButton = FindVisualChild<System.Windows.Controls.RadioButton>(presenter);
                    if (radioButton != null)
                    {
                        radioButton.IsChecked = true;
                    }
                }
            };
        }
    }
    
    private void RestoreWindowState()
    {
        try
        {
            var left = Services.AppSettings.GetWindowLeft();
            var top = Services.AppSettings.GetWindowTop();
            var width = Services.AppSettings.GetWindowWidth();
            var height = Services.AppSettings.GetWindowHeight();
            var state = Services.AppSettings.GetWindowState();
            
            // Restore size
            if (width > 0 && height > 0)
            {
                Width = width;
                Height = height;
            }
            
            // Check if we have saved position (if both are 0, it's first run)
            bool hasValidPosition = !(left == 0 && top == 0);
            
            if (hasValidPosition)
            {
                // Restore saved position
                Left = left;
                Top = top;
            }
            else
            {
                // First run - center the window
                Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
                Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
            }
            
            if (state == "Maximized")
            {
                WindowState = WindowState.Maximized;
            }
        }
        catch
        {
            // Use default window settings if restoration fails
            // Center window as fallback
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
        }
    }
    
    private void NavigateToPlugin(IToolPlugin plugin)
    {
        try
        {
            var view = plugin.CreateView();
            ContentFrame.Navigate(view);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to navigate to plugin {plugin.Id}: {ex.Message}");
        }
    }
    
    private void PluginNavItem_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.RadioButton radioButton && radioButton.Tag is IToolPlugin plugin)
        {
            NavigateToPlugin(plugin);
        }
    }
    
    private void NavSettings_Checked(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(new SettingsPage());
    }
    
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            MaximizeButton_Click(sender, e);
        }
        else
        {
            DragMove();
        }
    }
    
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
    
    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized 
            ? WindowState.Normal 
            : WindowState.Maximized;
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Minimize to tray instead of closing
        Hide();
        if (_trayIcon != null)
        {
            _trayIcon.Visible = true;
            _trayIcon.ShowBalloonTip(2000, "OmniShell", "App is running in the background", 
                System.Windows.Forms.ToolTipIcon.Info);
        }
    }
    
    #region System Tray Management
    
    private void InitializeTrayIcon()
    {
        try
        {
            System.Drawing.Icon appIcon = System.Drawing.SystemIcons.Application;
            
            // Try to load custom icon
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app_icon.ico");
            if (System.IO.File.Exists(iconPath))
            {
                try
                {
                    appIcon = new System.Drawing.Icon(iconPath);
                }
                catch
                {
                    // Fallback to system icon if custom icon fails to load
                }
            }
            
            _trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = appIcon,
                Visible = false,
                Text = "OmniShell"
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize tray icon: {ex.Message}");
            // Create minimal tray icon without crashing
            _trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = false,
                Text = "OmniShell"
            };
        }
        // Create context menu
        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        
        var showItem = new System.Windows.Forms.ToolStripMenuItem("Show OmniShell");
        showItem.Click += (s, e) => 
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            if (_trayIcon != null) _trayIcon.Visible = false;
        };
        contextMenu.Items.Add(showItem);
        
        var toggleSidebarItem = new System.Windows.Forms.ToolStripMenuItem("Toggle Sidebar");
        toggleSidebarItem.Click += (s, e) => ToggleSidebar();
        contextMenu.Items.Add(toggleSidebarItem);
        
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        
        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) =>
        {
            _trayIcon?.Dispose();
            _trayIcon = null;
            Application.Current.Shutdown();
        };
        contextMenu.Items.Add(exitItem);
        
        _trayIcon.ContextMenuStrip = contextMenu;
        
        // Double-click to show window
        _trayIcon.DoubleClick += (s, e) =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            _trayIcon.Visible = false;
        };
    }
    
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Save window state before closing
        SaveWindowState();
        
        // Prevent closing, minimize to tray instead
        e.Cancel = true;
        Hide();
        if (_trayIcon != null)
        {
            _trayIcon.Visible = true;
        }
        base.OnClosing(e);
    }
    
    private void SaveWindowState()
    {
        try
        {
            // Only save position if not minimized
            if (WindowState != WindowState.Minimized)
            {
                Services.AppSettings.SetWindowLeft(Left);
                Services.AppSettings.SetWindowTop(Top);
                Services.AppSettings.SetWindowWidth(Width);
                Services.AppSettings.SetWindowHeight(Height);
                Services.AppSettings.SetWindowState(WindowState == WindowState.Maximized ? "Maximized" : "Normal");
            }
        }
        catch
        {
            // Silently fail if save fails
        }
    }
    
    #endregion
    
    #region Sidebar Management
    
    private Views.SidebarMode _pendingSidebarMode = Views.SidebarMode.Floating;
    
    public void ToggleSidebar()
    {
        if (_sidebarWindow == null || !_sidebarWindow.IsVisible)
        {
            ShowSidebar();
        }
        else
        {
            HideSidebar();
        }
    }
    
    public void ShowSidebar()
    {
        if (_sidebarWindow == null)
        {
            _sidebarWindow = new SidebarWindow();
            _sidebarWindow.Mode = _pendingSidebarMode;
            _sidebarWindow.Closed += (s, e) => _sidebarWindow = null;
        }
        _sidebarWindow.ShowSidebar();
        
        // Save visibility preference
        Services.AppSettings.SetSidebarVisibleOnStartup(true);
    }
    
    public void HideSidebar()
    {
        _sidebarWindow?.HideSidebar();
        
        // Save visibility preference
        Services.AppSettings.SetSidebarVisibleOnStartup(false);
    }
    
    public bool IsSidebarVisible => _sidebarWindow?.IsVisible ?? false;
    
    public void SetSidebarMode(Views.SidebarMode mode)
    {
        _pendingSidebarMode = mode;
        
        // Save mode preference
        Services.AppSettings.SetSidebarMode(mode == Views.SidebarMode.Docked ? "Docked" : "Floating");
        
        if (_sidebarWindow != null)
        {
            _sidebarWindow.SetMode(mode);
        }
    }
    
    public void UpdateWidgetVisibility(string widgetName, bool visible)
    {
        _sidebarWindow?.SetWidgetVisibility(widgetName, visible);
    }
    
    public void ApplyWidgetOrder()
    {
        _sidebarWindow?.ApplyWidgetOrder();
    }
    
    #endregion
    
    /// <summary>
    /// Helper to find a visual child of a specific type
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;
            
            var descendant = FindVisualChild<T>(child);
            if (descendant != null)
                return descendant;
        }
        return null;
    }
}