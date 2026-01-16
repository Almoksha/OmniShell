using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using OmniShell.Interop;

namespace OmniShell.Services;

/// <summary>
/// Service for managing an AppBar (docked sidebar) using the Windows Shell API.
/// Fixed: proper screen bounds, unique callback message, proper unregistration.
/// </summary>
public class AppBarService : IDisposable
{
    private IntPtr _hwnd;
    private bool _isRegistered;
    private uint _callbackMessageId;
    private Shell32.APPBARDATA _appBarData;
    private int _edge = Shell32.ABE_RIGHT;
    private int _width = 320;
    private bool _disposed;
    private static uint _uniqueMessageId;

    public event EventHandler<EventArgs>? PositionChanged;

    public bool IsRegistered => _isRegistered;
    public int Width => _width;
    
    public int Edge
    {
        get => _edge;
        set
        {
            if (value != Shell32.ABE_LEFT && value != Shell32.ABE_RIGHT)
                throw new ArgumentException("Edge must be ABE_LEFT or ABE_RIGHT");
            _edge = value;
        }
    }

    /// <summary>
    /// Initializes the AppBar service with a window
    /// </summary>
    public void Initialize(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _hwnd = helper.Handle;
        
        if (_hwnd == IntPtr.Zero)
        {
            window.SourceInitialized += (s, e) =>
            {
                _hwnd = new WindowInteropHelper(window).Handle;
            };
        }
    }

    /// <summary>
    /// Registers the window as an AppBar and reserves screen space
    /// </summary>
    public bool Register(int width)
    {
        if (_hwnd == IntPtr.Zero)
            return false;

        // Unregister first if already registered (prevent stacking)
        if (_isRegistered)
        {
            Unregister();
        }

        _width = width;
        
        // Get unique callback message ID
        if (_uniqueMessageId == 0)
        {
            _uniqueMessageId = User32.RegisterWindowMessage("OmniShell_AppBar_Callback");
        }
        _callbackMessageId = _uniqueMessageId;
        
        _appBarData = new Shell32.APPBARDATA
        {
            cbSize = (uint)Marshal.SizeOf(typeof(Shell32.APPBARDATA)),
            hWnd = _hwnd,
            uCallbackMessage = _callbackMessageId
        };

        // Register with the shell
        uint result = Shell32.SHAppBarMessage(Shell32.ABM_NEW, ref _appBarData);
        if (result == 0)
            return false;

        _isRegistered = true;
        
        // Set the position
        SetPosition();
        
        return true;
    }

    /// <summary>
    /// Updates the AppBar position using full screen bounds
    /// </summary>
    public void SetPosition()
    {
        if (!_isRegistered)
            return;

        // Get DPI scaling factor
        double dpiScale = GetDpiScale();
        int physicalWidth = (int)(_width * dpiScale);
        
        System.Diagnostics.Debug.WriteLine($"[AppBarService] DPI Scale: {dpiScale}, Logical Width: {_width}, Physical Width: {physicalWidth}");

        // Get FULL screen bounds (not work area - Windows will adjust)
        var screenBounds = GetPrimaryScreenFullBounds();

        _appBarData.uEdge = (uint)_edge;
        
        if (_edge == Shell32.ABE_RIGHT)
        {
            _appBarData.rc.Left = screenBounds.Right - physicalWidth;
            _appBarData.rc.Top = screenBounds.Top;
            _appBarData.rc.Right = screenBounds.Right;
            _appBarData.rc.Bottom = screenBounds.Bottom;
        }
        else // ABE_LEFT
        {
            _appBarData.rc.Left = screenBounds.Left;
            _appBarData.rc.Top = screenBounds.Top;
            _appBarData.rc.Right = screenBounds.Left + physicalWidth;
            _appBarData.rc.Bottom = screenBounds.Bottom;
        }

        // Query for available position - Windows adjusts for taskbar
        Shell32.SHAppBarMessage(Shell32.ABM_QUERYPOS, ref _appBarData);
        
        // Adjust based on edge after query
        if (_edge == Shell32.ABE_RIGHT)
        {
            _appBarData.rc.Left = _appBarData.rc.Right - physicalWidth;
        }
        else
        {
            _appBarData.rc.Right = _appBarData.rc.Left + physicalWidth;
        }
        
        // Reserve the space
        Shell32.SHAppBarMessage(Shell32.ABM_SETPOS, ref _appBarData);
        
        PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the reserved rectangle for the AppBar (in WPF logical units)
    /// </summary>
    public Rect GetReservedRect()
    {
        double dpiScale = GetDpiScale();
        
        // Convert from physical pixels back to WPF logical units
        return new Rect(
            _appBarData.rc.Left / dpiScale,
            _appBarData.rc.Top / dpiScale,
            (_appBarData.rc.Right - _appBarData.rc.Left) / dpiScale,
            (_appBarData.rc.Bottom - _appBarData.rc.Top) / dpiScale);
    }

    /// <summary>
    /// Gets the system DPI scaling factor
    /// </summary>
    private double GetDpiScale()
    {
        var source = PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow);
        if (source?.CompositionTarget != null)
        {
            return source.CompositionTarget.TransformToDevice.M11;
        }
        return 1.0;
    }

    /// <summary>
    /// Unregisters the AppBar and releases screen space
    /// </summary>
    public void Unregister()
    {
        if (!_isRegistered)
            return;

        Shell32.SHAppBarMessage(Shell32.ABM_REMOVE, ref _appBarData);
        _isRegistered = false;
    }

    /// <summary>
    /// Gets full screen bounds (not work area)
    /// </summary>
    private Shell32.RECT GetPrimaryScreenFullBounds()
    {
        int screenWidth = User32.GetSystemMetrics(User32.SM_CXSCREEN);
        int screenHeight = User32.GetSystemMetrics(User32.SM_CYSCREEN);
        
        return new Shell32.RECT
        {
            Left = 0,
            Top = 0,
            Right = screenWidth,
            Bottom = screenHeight
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;
            
        Unregister();
        _disposed = true;
    }
}
