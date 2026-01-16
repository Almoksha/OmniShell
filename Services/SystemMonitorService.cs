using System;
using System.Diagnostics;
using System.Timers;

namespace OmniShell.Services;

/// <summary>
/// Service for monitoring system CPU and RAM usage
/// </summary>
public class SystemMonitorService
{
    private System.Timers.Timer? _timer;
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _ramCounter;
    private float _cpuUsage;
    private float _ramUsedGB;
    private float _ramTotalGB;
    private bool _isRunning;

    public float CpuUsage => _cpuUsage;
    public float RamUsagePercent => _ramTotalGB > 0 ? (_ramUsedGB / _ramTotalGB) * 100 : 0;
    public float RamUsedGB => _ramUsedGB;
    public float RamTotalGB => _ramTotalGB;

    public event EventHandler? MetricsUpdated;

    public SystemMonitorService()
    {
        // Get total physical memory
        try
        {
            var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
            _ramTotalGB = computerInfo.TotalPhysicalMemory / (1024f * 1024f * 1024f);
        }
        catch
        {
            // Fallback: assume 16GB if we can't get system info
            _ramTotalGB = 16;
        }
    }

    public void Start()
    {
        if (_isRunning) return;

        try
        {
            // Initialize performance counters
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);

            // Initial read (first read is always 0)
            _cpuCounter.NextValue();
            _ramCounter.NextValue();
        }
        catch
        {
            // Performance counters may not be available
        }

        _timer = new System.Timers.Timer(1500); // Update every 1.5 seconds
        _timer.Elapsed += Timer_Elapsed;
        _timer.AutoReset = true;
        _timer.Start();
        _isRunning = true;

        // Initial update
        UpdateMetrics();
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;

        _cpuCounter?.Dispose();
        _cpuCounter = null;

        _ramCounter?.Dispose();
        _ramCounter = null;

        _isRunning = false;
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        UpdateMetrics();
    }

    private void UpdateMetrics()
    {
        try
        {
            // Get CPU usage
            if (_cpuCounter != null)
            {
                _cpuUsage = _cpuCounter.NextValue();
            }

            // Get RAM usage
            if (_ramCounter != null)
            {
                var availableMB = _ramCounter.NextValue();
                var availableGB = availableMB / 1024f;
                _ramUsedGB = _ramTotalGB - availableGB;
            }
        }
        catch
        {
            // Counter access can fail
        }

        MetricsUpdated?.Invoke(this, EventArgs.Empty);
    }
}
