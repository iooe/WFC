namespace WFC.Models;

/// <summary>
/// Weighted connection to another tile
/// </summary>
public class TileConnectionWeight
{
    /// <summary>
    /// Target tile ID
    /// </summary>
    public string ToTileId { get; set; }
    
    /// <summary>
    /// Weight of this connection (higher = more likely)
    /// </summary>
    public float Weight { get; set; }
}