namespace WFC.Models;

/// <summary>
/// Definition of a tile connection rule
/// </summary>
public class TileRuleDefinition
{
    /// <summary>
    /// Source tile ID
    /// </summary>
    public string FromTileId { get; set; }
    
    /// <summary>
    /// Direction of the connection
    /// </summary>
    public string Direction { get; set; }
    
    /// <summary>
    /// Possible target tiles with weights
    /// </summary>
    public List<TileConnectionWeight> PossibleConnections { get; set; } = new();
}

