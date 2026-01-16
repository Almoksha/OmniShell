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
    
    public static MainWindow? Instance { get; private set; }
    
    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        
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
        Close();
    }
    
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
    }
    
    public void HideSidebar()
    {
        _sidebarWindow?.HideSidebar();
    }
    
    public bool IsSidebarVisible => _sidebarWindow?.IsVisible ?? false;
    
    public void SetSidebarMode(Views.SidebarMode mode)
    {
        _pendingSidebarMode = mode;
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