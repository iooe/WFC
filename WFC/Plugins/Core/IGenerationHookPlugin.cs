using WFC.Models;

namespace WFC.Plugins;

/// <summary>
/// Plugin interface for hooking into the generation process
/// </summary>
public interface IGenerationHookPlugin : IPlugin
{
    /// <summary>
    /// Called before generation starts
    /// </summary>
    /// <param name="settings">Current WFC settings</param>
    void OnBeforeGeneration(WFCSettings settings);
    
    /// <summary>
    /// Called when a cell is about to be collapsed
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="possibleStates">Current possible states</param>
    /// <param name="context">Generation context</param>
    /// <returns>Modified possible states or null to use default</returns>
    IEnumerable<int> OnBeforeCollapse(int x, int y, IEnumerable<int> possibleStates, GenerationContext context);
    
    /// <summary>
    /// Called after a cell has been collapsed
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="state">Collapsed state</param>
    /// <param name="context">Generation context</param>
    void OnAfterCollapse(int x, int y, int state, GenerationContext context);
    
    /// <summary>
    /// Called after generation is complete but before post-processing
    /// </summary>
    /// <param name="grid">Generated tile grid</param>
    /// <param name="context">Generation context</param>
    void OnAfterGeneration(Tile[,] grid, GenerationContext context);
    
    /// <summary>
    /// Called during the post-processing phase
    /// </summary>
    /// <param name="grid">Generated tile grid</param>
    /// <param name="context">Generation context</param>
    /// <returns>Modified tile grid</returns>
    Tile[,] OnPostProcess(Tile[,] grid, GenerationContext context);
}