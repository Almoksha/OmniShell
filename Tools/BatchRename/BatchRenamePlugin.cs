using System.Windows;
using OmniShell.Core;

namespace OmniShell.Tools.BatchRename;

/// <summary>
/// Plugin for the Batch Rename tool - bulk rename files with patterns.
/// </summary>
[ToolMetadata("batch-rename", "Batch Rename", order: 3)]
public class BatchRenamePlugin : IToolPlugin
{
    public string Id => "batch-rename";
    
    public string Name => "Batch Rename";
    
    public string IconPathData => "M11,4H4A2,2,0,0,0,2,6V20a2,2,0,0,0,2,2H18a2,2,0,0,0,2-2V13";
    
    public int Order => 3;
    
    public FrameworkElement CreateView() => new BatchRenamePage();
}
