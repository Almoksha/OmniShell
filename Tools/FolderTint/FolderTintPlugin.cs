using System.Windows;
using OmniShell.Core;

namespace OmniShell.Tools.FolderTint;

/// <summary>
/// Plugin for the Folder Tinting tool - applies custom colors to Windows folder icons.
/// </summary>
[ToolMetadata("folder-tint", "Folder Tinting", order: 1)]
public class FolderTintPlugin : IToolPlugin
{
    public string Id => "folder-tint";
    
    public string Name => "Folder Tinting";
    
    public string IconPathData => "M3,3 L21,3 21,21 3,21 Z M7,7 L7,17 17,17 17,7 Z";
    
    public int Order => 1;
    
    public FrameworkElement CreateView() => new FolderTintPage();
}
