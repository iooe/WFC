using WFC.Models;

namespace WFC.Plugins;

/// <summary>
/// Plugin interface for post-processing generated maps
/// </summary>
public interface IPostProcessorPlugin : IPlugin
{
    /// <summary>
    /// Priority of this post-processor (lower executes first)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Post-process the generated tile grid
    /// </summary>
    /// <param name="grid">Original tile grid</param>
    /// <param name="context">Generation context</param>
    /// <returns>Processed tile grid</returns>
    Tile[,] ProcessGrid(Tile[,] grid, GenerationContext context);
}