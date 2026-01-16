using System;
using System.IO;
using System.Windows.Controls;
using OmniShell.Services;

namespace OmniShell.Tools.FolderTint;

/// <summary>
/// Page for folder icon tinting functionality
/// </summary>
public partial class FolderTintPage : Page
{
    private readonly IconGenerator _iconGenerator;
    private readonly FolderIconManager _folderIconManager;
    private string? _selectedFolderPath;

    public FolderTintPage()
    {
        InitializeComponent();
        
        // Create cache directory in AppData
        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OmniShell", "IconCache");
        Directory.CreateDirectory(cacheDir);
        
        _iconGenerator = new IconGenerator(cacheDir);
        _folderIconManager = new FolderIconManager();
        Log("FolderTintPage initialized with FolderTools approach");
    }

    private void Log(string message)
    {
        string logMessage = $"[FolderTintPage] {message}";
        System.Diagnostics.Debug.WriteLine(logMessage);
        Console.WriteLine(logMessage);
    }

    private void BrowseButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Log("BrowseButton_Click");
        
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select a folder to customize",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _selectedFolderPath = dialog.SelectedPath;
            SelectedFolderText.Text = _selectedFolderPath;
            StatusText.Text = "";
            Log($"Selected folder: {_selectedFolderPath}");
        }
    }

    private async void ColorButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var button = (System.Windows.Controls.Button)sender;
        var colorTag = button.Tag?.ToString();
        
        Log($"=== ColorButton_Click ===");
        Log($"  Button Tag: {colorTag}");
        Log($"  Selected Folder: {_selectedFolderPath}");
        
        if (string.IsNullOrEmpty(_selectedFolderPath))
        {
            Log("  ERROR: No folder selected");
            StatusText.Text = "⚠ Please select a folder first";
            StatusText.Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush");
            return;
        }

        if (string.IsNullOrEmpty(colorTag))
        {
            Log("  ERROR: Color tag is null/empty");
            return;
        }

        bool success = false;
        
        if (colorTag == "Default")
        {
            Log("  Action: RemoveFolderIcon");
            try
            {
                _folderIconManager.RemoveFolderIcon(_selectedFolderPath);
                success = true;
                StatusText.Text = "✓ Folder icon reset to default";
                StatusText.Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush");
            }
            catch (Exception ex)
            {
                Log($"  ERROR: {ex.Message}");
                success = false;
            }
        }
        else
        {
            Log($"  Action: SetFolderColor({colorTag})");
            
            try
            {
                // Get the colored icon path from the generator
                string? iconPath = _iconGenerator.GetColoredFolderIcon(colorTag);
                
                if (string.IsNullOrEmpty(iconPath) || !File.Exists(iconPath))
                {
                    Log($"  ERROR: Failed to generate icon for color '{colorTag}'");
                    StatusText.Text = $"⚠ Failed to generate {colorTag} icon";
                    StatusText.Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush");
                    return;
                }
                
                Log($"  Generated icon path: {iconPath}");
                
                // Apply the icon to the folder
                await _folderIconManager.ApplyFolderIcon(_selectedFolderPath, iconPath);
                success = true;
                
                Log($"  SUCCESS: Color applied");
                StatusText.Text = $"✓ Applied {colorTag} color to folder";
                StatusText.Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush");
            }
            catch (Exception ex)
            {
                Log($"  EXCEPTION: {ex.Message}");
                success = false;
            }
        }

        if (!success)
        {
            Log("  Final status: FAILED");
            StatusText.Text = "⚠ Failed to apply icon. Check Output window for details.";
            StatusText.Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush");
        }
        
        Log($"=== ColorButton_Click complete ===");
    }
}
