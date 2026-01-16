using System;

namespace OmniShell.Core;

/// <summary>
/// Attribute for marking classes as tool plugins with metadata for discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ToolMetadataAttribute : Attribute
{
    /// <summary>
    /// Unique identifier for the tool
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Display name for the tool
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Sort order in navigation (default: 100)
    /// </summary>
    public int Order { get; }
    
    public ToolMetadataAttribute(string id, string name, int order = 100)
    {
        Id = id;
        Name = name;
        Order = order;
    }
}
