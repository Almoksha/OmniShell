using System;
using System.Windows.Controls;
using Microsoft.Win32;

namespace OmniShell.Views;

public partial class SettingsPage : Page
{
    private const string ContextMenuRegistryPath = @"Directory\shell\OmniShell";
    
    public SettingsPage()
    {
        InitializeComponent();
        CheckContextMenuStatus();
        UpdateSidebarButtonState();
        InitializeStartupToggle();
    }
    
    private void InitializeStartupToggle()
    {
        // Set initial state based on whether startup is enabled
        UpdateStartupToggleUI(Services.StartupManager.IsStartupEnabled());
    }
    
    private void UpdateStartupToggleUI(bool isEnabled)
    {
        var enabledColor = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x63, 0x66, 0xF1)); // Primary blue
        var disabledColor = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x4B, 0x55, 0x63)); // Gray
        
        StartupToggle.Background = isEnabled ? enabledColor : disabledColor;
        StartupToggleKnob.HorizontalAlignment = isEnabled 
            ? System.Windows.HorizontalAlignment.Right 
            : System.Windows.HorizontalAlignment.Left;
    }
    
    private void StartupToggle_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            bool currentState = Services.StartupManager.IsStartupEnabled();
            bool newState = !currentState;
            
            if (newState)
            {
                if (Services.StartupManager.EnableStartup())
                {
                    UpdateStartupToggleUI(true);
                    System.Windows.MessageBox.Show("OmniShell will now start with Windows!", 
                        "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to enable startup. Please try running as Administrator.", 
                        "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            else
            {
                if (Services.StartupManager.DisableStartup())
                {
                    UpdateStartupToggleUI(false);
                    System.Windows.MessageBox.Show("OmniShell will no longer start with Windows.", 
                        "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to disable startup.", 
                        "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    private void UpdateSidebarButtonState()
    {
        var mainWindow = MainWindow.Instance;
        SidebarToggleButton.Content = mainWindow?.IsSidebarVisible == true ? "Hide" : "Show";
    }

    private void CheckContextMenuStatus()
    {
        try
        {
            using var key = Registry.ClassesRoot.OpenSubKey(ContextMenuRegistryPath);
            ContextMenuButton.Content = key != null ? "Uninstall" : "Install";
        }
        catch
        {
            ContextMenuButton.Content = "Install";
        }
    }

    private void ContextMenuButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            using var key = Registry.ClassesRoot.OpenSubKey(ContextMenuRegistryPath);
            
            if (key != null)
            {
                // Uninstall
                Registry.ClassesRoot.DeleteSubKeyTree(ContextMenuRegistryPath);
                ContextMenuButton.Content = "Install";
                System.Windows.MessageBox.Show("Context menu entry removed successfully!", 
                    "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else
            {
                // Install
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                exePath = exePath.Replace(".dll", ".exe");
                
                using var shellKey = Registry.ClassesRoot.CreateSubKey(ContextMenuRegistryPath);
                shellKey?.SetValue("", "Tint Folder with OmniShell");
                shellKey?.SetValue("Icon", exePath);
                
                using var commandKey = Registry.ClassesRoot.CreateSubKey($@"{ContextMenuRegistryPath}\command");
                commandKey?.SetValue("", $"\"{exePath}\" --tint \"%V\"");
                
                ContextMenuButton.Content = "Uninstall";
                System.Windows.MessageBox.Show("Context menu entry added successfully!\nNote: On Windows 11, this appears under 'Show more options'.", 
                    "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        catch (UnauthorizedAccessException)
        {
            System.Windows.MessageBox.Show("Please run OmniShell as Administrator to modify context menu.", 
                "Permission Required", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    private void SidebarToggleButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var mainWindow = MainWindow.Instance;
        if (mainWindow == null) return;
        
        mainWindow.ToggleSidebar();
        UpdateSidebarButtonState();
    }
    
    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to open link: {ex.Message}", "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
