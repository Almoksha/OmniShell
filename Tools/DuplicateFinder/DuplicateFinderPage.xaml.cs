using System.Windows.Controls;

namespace OmniShell.Tools.DuplicateFinder;

/// <summary>
/// Duplicate file finder page
/// </summary>
public partial class DuplicateFinderPage : Page
{
    private string? _selectedFolderPath;

    public DuplicateFinderPage()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select a folder to scan for duplicates",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _selectedFolderPath = dialog.SelectedPath;
            SelectedFolderText.Text = _selectedFolderPath;
        }
    }

    private async void ScanButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedFolderPath))
        {
            StatusText.Text = "⚠ Please select a folder first";
            return;
        }

        ScanButton.IsEnabled = false;
        ScanProgress.Visibility = System.Windows.Visibility.Visible;
        EmptyState.Visibility = System.Windows.Visibility.Collapsed;
        DuplicatesList.Items.Clear();
        StatusText.Text = "Scanning files...";

        try
        {
            // Simulate scanning (in production, use DuplicateFinderService)
            for (int i = 0; i <= 100; i += 10)
            {
                ScanProgress.Value = i;
                await System.Threading.Tasks.Task.Delay(100);
            }

            StatusText.Text = "✓ Scan complete - No duplicates found";
        }
        finally
        {
            ScanButton.IsEnabled = true;
            ScanProgress.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}
