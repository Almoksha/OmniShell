using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using OmniShell.Interop;

namespace OmniShell.Services;

/// <summary>
/// Represents a clipboard history item
/// </summary>
public class ClipboardItem
{
    public ClipboardItemType Type { get; set; }
    public string? Text { get; set; }
    public BitmapSource? Image { get; set; }
    public string[]? Files { get; set; }
    public DateTime Timestamp { get; set; }

    public string Preview
    {
        get
        {
            return Type switch
            {
                ClipboardItemType.Text => Text?.Length > 100 ? Text[..100] + "..." : Text ?? "",
                ClipboardItemType.Image => "📷 Image",
                ClipboardItemType.Files => $"📁 {Files?.Length ?? 0} file(s)",
                _ => ""
            };
        }
    }

    public string TimeAgo
    {
        get
        {
            var elapsed = DateTime.Now - Timestamp;
            if (elapsed.TotalMinutes < 1) return "now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m";
            if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h";
            return $"{(int)elapsed.TotalDays}d";
        }
    }
}

public enum ClipboardItemType
{
    Text,
    Image,
    Files
}

/// <summary>
/// Service for monitoring clipboard changes and maintaining history
/// </summary>
public class ClipboardService : IDisposable
{
    private IntPtr _hwnd;
    private bool _isListening;
    private bool _disposed;
    private const int MaxHistoryItems = 25;

    public ObservableCollection<ClipboardItem> History { get; } = new();

    public event EventHandler? ClipboardChanged;

    public void Initialize(IntPtr hwnd)
    {
        _hwnd = hwnd;
        
        if (User32.AddClipboardFormatListener(_hwnd))
        {
            _isListening = true;
        }
    }

    public void ProcessClipboardChange()
    {
        try
        {
            ClipboardItem? item = null;

            if (System.Windows.Clipboard.ContainsText())
            {
                var text = System.Windows.Clipboard.GetText();
                // Avoid duplicates
                if (History.Count > 0 && History[0].Type == ClipboardItemType.Text && History[0].Text == text)
                    return;

                item = new ClipboardItem
                {
                    Type = ClipboardItemType.Text,
                    Text = text,
                    Timestamp = DateTime.Now
                };
            }
            else if (System.Windows.Clipboard.ContainsImage())
            {
                var image = System.Windows.Clipboard.GetImage();
                if (image != null)
                {
                    item = new ClipboardItem
                    {
                        Type = ClipboardItemType.Image,
                        Image = image,
                        Timestamp = DateTime.Now
                    };
                }
            }
            else if (System.Windows.Clipboard.ContainsFileDropList())
            {
                var files = System.Windows.Clipboard.GetFileDropList();
                var fileArray = new string[files.Count];
                files.CopyTo(fileArray, 0);

                item = new ClipboardItem
                {
                    Type = ClipboardItemType.Files,
                    Files = fileArray,
                    Timestamp = DateTime.Now
                };
            }

            if (item != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    History.Insert(0, item);
                    while (History.Count > MaxHistoryItems)
                    {
                        History.RemoveAt(History.Count - 1);
                    }
                    ClipboardChanged?.Invoke(this, EventArgs.Empty);
                });
            }
        }
        catch
        {
            // Clipboard access can fail if another app has it locked
        }
    }

    public void CopyToClipboard(ClipboardItem item)
    {
        try
        {
            switch (item.Type)
            {
                case ClipboardItemType.Text:
                    if (item.Text != null)
                        System.Windows.Clipboard.SetText(item.Text);
                    break;
                case ClipboardItemType.Image:
                    if (item.Image != null)
                        System.Windows.Clipboard.SetImage(item.Image);
                    break;
                case ClipboardItemType.Files:
                    if (item.Files != null)
                    {
                        var collection = new System.Collections.Specialized.StringCollection();
                        collection.AddRange(item.Files);
                        System.Windows.Clipboard.SetFileDropList(collection);
                    }
                    break;
            }
        }
        catch
        {
            // Clipboard access can fail
        }
    }

    public void ClearHistory()
    {
        History.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_isListening && _hwnd != IntPtr.Zero)
        {
            User32.RemoveClipboardFormatListener(_hwnd);
            _isListening = false;
        }

        _disposed = true;
    }
}
