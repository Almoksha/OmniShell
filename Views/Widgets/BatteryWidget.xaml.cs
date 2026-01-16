using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace OmniShell.Views.Widgets;

public partial class BatteryWidget : System.Windows.Controls.UserControl
{
    private readonly DispatcherTimer _timer;
    
    public int BatteryPercentage { get; private set; }
    public event EventHandler? BatteryUpdated;

    public BatteryWidget()
    {
        InitializeComponent();
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _timer.Tick += (s, e) => UpdateBatteryStatus();
        _timer.Start();
        
        Loaded += (s, e) => UpdateBatteryStatus();
        Unloaded += (s, e) => _timer.Stop();
    }

    private void UpdateBatteryStatus()
    {
        try
        {
            var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
            var percent = (int)(powerStatus.BatteryLifePercent * 100);
            
            // Clamp to valid range
            percent = Math.Max(0, Math.Min(100, percent));
            
            PercentageText.Text = $"{percent}%";
            
            // Update status text
            if (powerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online)
            {
                if (percent >= 100)
                    StatusText.Text = "Fully charged";
                else
                    StatusText.Text = "Charging";
                    
                ChargingIcon.Visibility = Visibility.Visible;
            }
            else
            {
                StatusText.Text = "On battery";
                ChargingIcon.Visibility = Visibility.Collapsed;
            }
            
            // Time remaining
            if (powerStatus.BatteryLifeRemaining > 0)
            {
                var remaining = TimeSpan.FromSeconds(powerStatus.BatteryLifeRemaining);
                TimeRemainingText.Text = $"{remaining.Hours}h {remaining.Minutes}m remaining";
            }
            else
            {
                TimeRemainingText.Text = "";
            }
            
            // Update battery fill
            var fillWidth = (44.0 * percent / 100.0);
            BatteryFill.Width = Math.Max(2, fillWidth);
            
            // Color based on percentage
            if (percent <= 20)
            {
                PercentageText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEF, 0x44, 0x44));
                BatteryFill.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEF, 0x44, 0x44));
            }
            else if (percent <= 50)
            {
                PercentageText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF9, 0x73, 0x16));
                BatteryFill.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF9, 0x73, 0x16));
            }
            else
            {
                PercentageText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81));
                BatteryFill.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81));
            }
            
            // Update property and notify
            BatteryPercentage = percent;
            BatteryUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            PercentageText.Text = "N/A";
            StatusText.Text = "No battery detected";
            TimeRemainingText.Text = "";
        }
    }
}
