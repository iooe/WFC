using WFC.Models;

namespace WFC.Plugins.Basic;

/// <summary>
/// Basic plugin providing grass tiles
/// </summary>
public class GrassPlugin : ITileSetPlugin
{
    public string Id => "wfc.basic.grass";
    public string Name => "Basic Grass";
    public string Version => "1.0";
    public string Description => "Provides basic grass tiles";
    
    private List<TileDefinition> _tileDefinitions;
    private List<TileRuleDefinition> _ruleDefinitions;
    
    public void Initialize(IServiceProvider serviceProvider)
    {
        // Create tile definitions
        _tileDefinitions = new List<TileDefinition>
        {
            new TileDefinition
            {
                Id = "grass.basic",
                Name = "Grass",
                Category = "grass",
                ResourcePath = "grass",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "true" },
                    { "surface", "grass" }
                }
            }
        };
        
        // Create rule definitions
        _ruleDefinitions = new List<TileRuleDefinition>
        {
            // Grass can connect to grass in all directions
            new()
            {
                FromTileId = "grass.basic",
                Direction = "up",
                PossibleConnections = new List<TileConnectionWeight>
                {
                    new() { ToTileId = "grass.basic", Weight = 1.0f }
                }
            },
            new()
            {
                FromTileId = "grass.basic",
                Direction = "down",
                PossibleConnections = new List<TileConnectionWeight>
                {
                    new() { ToTileId = "grass.basic", Weight = 1.0f }
                }
            },
            new()
            {
                FromTileId = "grass.basic",
                Direction = "left",
                PossibleConnections = new List<TileConnectionWeight>
                {
                    new() { ToTileId = "grass.basic", Weight = 1.0f }
                }
            },
            new()
            {
                FromTileId = "grass.basic",
                Direction = "right",
                PossibleConnections = new List<TileConnectionWeight>
                {
                    new() { ToTileId = "grass.basic", Weight = 1.0f }
                }
            }
        };
    }
    
    public IEnumerable<TileDefinition> GetTileDefinitions()
    {
        return _tileDefinitions;
    }
    
    public IEnumerable<TileRuleDefinition> GetRuleDefinitions()
    {
        return _ruleDefinitions;
    }
}