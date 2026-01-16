using System;
using System.Media;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace OmniShell.Views.Widgets;

public partial class PomodoroWidget : System.Windows.Controls.UserControl
{
    private readonly DispatcherTimer _timer;
    private TimeSpan _timeRemaining;
    private TimeSpan _totalTime;
    private bool _isRunning;
    private bool _isFocusMode = true;
    private int _sessionCount;
    
    private const int FocusMinutes = 25;
    private const int BreakMinutes = 5;
    
    // Public property and event for compact view sync
    public string TimeRemainingText => $"{(int)_timeRemaining.TotalMinutes:D2}:{_timeRemaining.Seconds:D2}";
    public event EventHandler? TimerUpdated;

    public PomodoroWidget()
    {
        InitializeComponent();
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        
        ResetTimer();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _timeRemaining = _timeRemaining.Subtract(TimeSpan.FromSeconds(1));
        
        if (_timeRemaining <= TimeSpan.Zero)
        {
            CompleteSession();
        }
        else
        {
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        TimerText.Text = $"{(int)_timeRemaining.TotalMinutes:D2}:{_timeRemaining.Seconds:D2}";
        
        // Update progress bar
        var progress = 1 - (_timeRemaining.TotalSeconds / _totalTime.TotalSeconds);
        ProgressBar.Width = (ActualWidth - 24) * Math.Max(0, Math.Min(1, progress));
        
        // Notify subscribers (for compact view)
        TimerUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void CompleteSession()
    {
        _timer.Stop();
        _isRunning = false;
        
        // Play notification sound
        try
        {
            SystemSounds.Exclamation.Play();
        }
        catch { }
        
        if (_isFocusMode)
        {
            _sessionCount++;
            SessionCountText.Text = $"{_sessionCount} session{(_sessionCount == 1 ? "" : "s")}";
            
            // Switch to break
            _isFocusMode = false;
            _timeRemaining = TimeSpan.FromMinutes(BreakMinutes);
            _totalTime = _timeRemaining;
            ModeText.Text = "Break Time";
            TimerText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81));
        }
        else
        {
            // Switch to focus
            _isFocusMode = true;
            _timeRemaining = TimeSpan.FromMinutes(FocusMinutes);
            _totalTime = _timeRemaining;
            ModeText.Text = "Focus Time";
            TimerText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF9, 0x73, 0x16));
        }
        
        UpdateDisplay();
    }

    private void ResetTimer()
    {
        _timer.Stop();
        _isRunning = false;
        _isFocusMode = true;
        _timeRemaining = TimeSpan.FromMinutes(FocusMinutes);
        _totalTime = _timeRemaining;
        ModeText.Text = "Focus Time";
        TimerText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF9, 0x73, 0x16));
        ProgressBar.Width = 0;
        UpdateDisplay();
    }

    private void StartPauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            _timer.Stop();
            _isRunning = false;
        }
        else
        {
            _timer.Start();
            _isRunning = true;
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        ResetTimer();
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        CompleteSession();
    }
}
