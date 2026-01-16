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
}
