using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace OmniShell.Views.Widgets;

public partial class NotesWidget : System.Windows.Controls.UserControl
{
    private readonly string _notesFilePath;
    private readonly DispatcherTimer _saveTimer;

    public NotesWidget()
    {
        InitializeComponent();
        
        // Store notes in app data
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var omnishellFolder = Path.Combine(appData, "OmniShell");
        Directory.CreateDirectory(omnishellFolder);
        _notesFilePath = Path.Combine(omnishellFolder, "quick_notes.txt");
        
        // Debounced save timer
        _saveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _saveTimer.Tick += (s, e) =>
        {
            _saveTimer.Stop();
            SaveNotes();
        };
        
        Loaded += (s, e) => LoadNotes();
    }

    private void LoadNotes()
    {
        try
        {
            if (File.Exists(_notesFilePath))
            {
                NotesTextBox.Text = File.ReadAllText(_notesFilePath);
            }
        }
        catch { }
        
        UpdateCharCount();
    }

    private void SaveNotes()
    {
        try
        {
            File.WriteAllText(_notesFilePath, NotesTextBox.Text);
        }
        catch { }
    }

    private void UpdateCharCount()
    {
        CharCountText.Text = $"{NotesTextBox.Text.Length} characters";
    }

    private void NotesTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateCharCount();
        
        // Restart save timer (debounce)
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        NotesTextBox.Text = "";
        SaveNotes();
    }
}
