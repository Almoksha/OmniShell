using System.Windows;
using OmniShell.Core;

namespace OmniShell.Tools.Sidebar;

/// <summary>
/// Plugin for the Quick Access Sidebar tool - provides clipboard manager and system monitor.
/// </summary>
[ToolMetadata("sidebar", "Quick Sidebar", order: 4)]
public class SidebarPlugin : IToolPlugin
{
    public string Id => "sidebar";
    
    public string Name => "Quick Sidebar";
    
    // Sidebar icon - vertical bars representing a sidebar panel
    public string IconPathData => "M3,3 L3,21 M7,6 L7,18 M11,3 L21,3 21,21 11,21 Z";
    
    public int Order => 4;
    
    public FrameworkElement CreateView() => new SidebarPage();
}
