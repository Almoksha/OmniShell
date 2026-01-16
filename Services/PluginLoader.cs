using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OmniShell.Core;

namespace OmniShell.Services;

/// <summary>
/// Service that discovers and loads all tool plugins implementing IToolPlugin.
/// </summary>
public class PluginLoader
{
    private readonly List<IToolPlugin> _plugins = new();
    
    /// <summary>
    /// Gets all discovered and loaded plugins, sorted by their Order property
    /// </summary>
    public IReadOnlyList<IToolPlugin> Plugins => _plugins.AsReadOnly();
    
    /// <summary>
    /// Discovers all classes implementing IToolPlugin in the current assembly
    /// and instantiates them.
    /// </summary>
    public void DiscoverPlugins()
    {
        _plugins.Clear();
        
        var assembly = Assembly.GetExecutingAssembly();
        var pluginInterface = typeof(IToolPlugin);
        
        // Find all types that implement IToolPlugin
        var pluginTypes = assembly.GetTypes()
            .Where(t => pluginInterface.IsAssignableFrom(t) 
                        && t.IsClass 
                        && !t.IsAbstract);
        
        foreach (var pluginType in pluginTypes)
        {
            try
            {
                // Try to create an instance using parameterless constructor
                if (Activator.CreateInstance(pluginType) is IToolPlugin plugin)
                {
                    _plugins.Add(plugin);
                    Log($"Loaded plugin: {plugin.Name} (Id: {plugin.Id}, Order: {plugin.Order})");
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to load plugin {pluginType.Name}: {ex.Message}");
            }
        }
        
        // Sort plugins by Order
        _plugins.Sort((a, b) => a.Order.CompareTo(b.Order));
        
        Log($"Plugin discovery complete. Loaded {_plugins.Count} plugins.");
    }
    
    /// <summary>
    /// Gets a plugin by its ID
    /// </summary>
    public IToolPlugin? GetPlugin(string id)
    {
        return _plugins.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
    
    private void Log(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[PluginLoader] {message}");
    }
}
