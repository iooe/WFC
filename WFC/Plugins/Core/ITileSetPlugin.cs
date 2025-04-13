using WFC.Models;

namespace WFC.Plugins;

/// <summary>
/// Plugin interface for providing tile sets
/// </summary>
public interface ITileSetPlugin : IPlugin
{
    /// <summary>
    /// Get all tiles provided by this plugin
    /// </summary>
    /// <returns>Collection of tiles</returns>
    IEnumerable<TileDefinition> GetTileDefinitions();
    
    /// <summary>
    /// Get rule definitions for tiles provided by this plugin
    /// </summary>
    /// <returns>Collection of rule definitions</returns>
    IEnumerable<TileRuleDefinition> GetRuleDefinitions();
}