using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Threading;

namespace OmniShell.Views.Widgets;

public partial class NetworkWidget : System.Windows.Controls.UserControl
{
    private readonly DispatcherTimer _timer;
    private long _lastBytesReceived;
    private long _lastBytesSent;
    private DateTime _lastUpdate;

    public NetworkWidget()
    {
        InitializeComponent();
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => UpdateNetworkStats();
        _timer.Start();
        
        Loaded += (s, e) =>
        {
            InitializeCounters();
            UpdateIpAddress();
        };
        Unloaded += (s, e) => _timer.Stop();
    }

    private void InitializeCounters()
    {
        try
        {
            var stats = GetNetworkStats();
            _lastBytesReceived = stats.bytesReceived;
            _lastBytesSent = stats.bytesSent;
            _lastUpdate = DateTime.Now;
        }
        catch { }
    }

    private void UpdateNetworkStats()
    {
        try
        {
            var stats = GetNetworkStats();
            var now = DateTime.Now;
            var elapsed = (now - _lastUpdate).TotalSeconds;
            
            if (elapsed > 0)
            {
                var downloadSpeed = (stats.bytesReceived - _lastBytesReceived) / elapsed;
                var uploadSpeed = (stats.bytesSent - _lastBytesSent) / elapsed;
                
                DownloadSpeedText.Text = FormatSpeed(downloadSpeed);
                UploadSpeedText.Text = FormatSpeed(uploadSpeed);
            }
            
            _lastBytesReceived = stats.bytesReceived;
            _lastBytesSent = stats.bytesSent;
            _lastUpdate = now;
        }
        catch
        {
            DownloadSpeedText.Text = "-- KB/s";
            UploadSpeedText.Text = "-- KB/s";
        }
    }

    private (long bytesReceived, long bytesSent) GetNetworkStats()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && 
                       n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
        
        long totalReceived = 0;
        long totalSent = 0;
        
        foreach (var ni in interfaces)
        {
            var stats = ni.GetIPv4Statistics();
            totalReceived += stats.BytesReceived;
            totalSent += stats.BytesSent;
        }
        
        return (totalReceived, totalSent);
    }

    private void UpdateIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList
                .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            
            IpAddressText.Text = ip?.ToString() ?? "Not connected";
        }
        catch
        {
            IpAddressText.Text = "Unknown";
        }
    }

    private string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond >= 1024 * 1024)
            return $"{bytesPerSecond / (1024 * 1024):F1} MB/s";
        else if (bytesPerSecond >= 1024)
            return $"{bytesPerSecond / 1024:F0} KB/s";
        else
            return $"{bytesPerSecond:F0} B/s";
    }
}
