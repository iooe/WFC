namespace WFC.Models;

/// <summary>
/// Settings for the WFC algorithm
/// </summary>
public class WFCSettings
{
    /// <summary>
    /// Width of the grid
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// Height of the grid
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// Available tiles
    /// </summary>
    public List<Tile> Tiles { get; set; }
    
    /// <summary>
    /// Map of tile IDs to their index in the Tiles list
    /// </summary>
    public Dictionary<string, int> TileIndexMap { get; set; }
    
    /// <summary>
    /// Connection rules between tiles
    /// </summary>
    public Dictionary<(int fromState, string direction), List<(int toState, float weight)>> Rules { get; set; }
    
    /// <summary>
    /// Random seed for generation (null for random seed)
    /// </summary>
    public int? Seed { get; set; }
    
    /// <summary>
    /// Whether to enable animated debug rendering
    /// </summary>
    public bool EnableDebugRendering { get; set; }
    
    /// <summary>
    /// Additional settings for plugins
    /// </summary>
    public Dictionary<string, object> PluginSettings { get; set; }
    
    public WFCSettings()
    {
        Tiles = new List<Tile>();
        TileIndexMap = new Dictionary<string, int>();
        Rules = new Dictionary<(int fromState, string direction), List<(int toState, float weight)>>();
        PluginSettings = new Dictionary<string, object>();
    }
}