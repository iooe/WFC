namespace WFC.Models;

/// <summary>
/// Definition of a tile type
/// </summary>
public class TileDefinition
{
    /// <summary>
    /// Unique identifier for the tile
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// Display name of the tile
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Category of the tile
    /// </summary>
    public string Category { get; set; }
    
    /// <summary>
    /// Path to the folder containing tile images
    /// </summary>
    public string ResourcePath { get; set; }
    
    /// <summary>
    /// Properties for the tile (optional)
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();
}