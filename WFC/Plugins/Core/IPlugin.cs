namespace WFC.Plugins;

/// <summary>
/// Base interface for all WFC plugins
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Unique identifier for the plugin
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Display name of the plugin
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Plugin version
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Plugin description
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Initialize the plugin
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency resolution</param>
    void Initialize(IServiceProvider serviceProvider);
}