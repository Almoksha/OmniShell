using System.Windows;

namespace OmniShell.Core;

/// <summary>
/// Interface that all tool plugins must implement to be discovered and loaded by the application.
/// </summary>
public interface IToolPlugin
{
    /// <summary>
    /// Unique identifier for the tool (e.g., "folder-tint", "duplicate-finder")
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Display name shown in the navigation sidebar
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// SVG path data for the navigation icon
    /// </summary>
    string IconPathData { get; }
    
    /// <summary>
    /// Sort order in the navigation sidebar (lower values appear first)
    /// </summary>
    int Order { get; }
    
    /// <summary>
    /// Creates and returns the tool's main UI content
    /// </summary>
    FrameworkElement CreateView();
}
