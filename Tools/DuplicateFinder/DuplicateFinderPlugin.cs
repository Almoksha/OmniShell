using System.Windows;
using OmniShell.Core;

namespace OmniShell.Tools.DuplicateFinder;

/// <summary>
/// Plugin for the Duplicate Finder tool - finds and removes duplicate files.
/// </summary>
[ToolMetadata("duplicate-finder", "Duplicate Finder", order: 2)]
public class DuplicateFinderPlugin : IToolPlugin
{
    public string Id => "duplicate-finder";
    
    public string Name => "Duplicate Finder";
    
    public string IconPathData => "M4,4 L16,4 16,16 4,16 Z M8,8 L20,8 20,20 8,20 Z";
    
    public int Order => 2;
    
    public FrameworkElement CreateView() => new DuplicateFinderPage();
}
