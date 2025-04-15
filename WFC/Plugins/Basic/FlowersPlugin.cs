using WFC.Models;

namespace WFC.Plugins.Basic;

/// <summary>
/// Plugin providing flower tiles
/// </summary>
public class FlowersPlugin : ITileSetPlugin
{
    public string Id => "wfc.basic.flowers";
    public string Name => "Flowers";
    public string Version => "1.0";
    public string Description => "Provides flower tiles";
    
    public bool Enabled { get; set; }
    
    private List<TileDefinition> _tileDefinitions;
    private List<TileRuleDefinition> _ruleDefinitions;
    
    public void Initialize(IServiceProvider serviceProvider)
    {
        // Create tile definitions
        _tileDefinitions = new List<TileDefinition>
        {
            new()
            {
                Id = "flowers.basic",
                Name = "Flowers",
                Category = "flowers",
                ResourcePath = "flowers",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "true" },
                    { "surface", "flowers" }
                }
            }
        };
        
        // Create rule definitions
        _ruleDefinitions = new List<TileRuleDefinition>
        {
            // Flowers can connect to flowers in all directions
            new()
            {
                FromTileId = "flowers.basic",
                Direction = "up",
                PossibleConnections = new List<TileConnectionWeight>
                {
                    new() { ToTileId = "flowers.basic", Weight = 1.0f },
                    new() { ToTileId = "grass.basic", Weight = 0.8f }
                }
            },
            new()
            {
                FromTileId = "flowers.basic",
                Direction = "down",
                PossibleConnections = new List<TileConnectionWeight>
                {
                    new() { ToTileId = "flowers.basic", Weight = 1.0f },
                    new() { ToTileId = "grass.basic", Weight = 0.8f }
                }
            },
            new()
            {
                FromTileId = "flowers.basic",
                Direction = "left",
                PossibleConnections = new List<TileConnectionWeight>
                {
                    new() { ToTileId = "flowers.basic", Weight = 1.0f },
                    new() { ToTileId = "grass.basic", Weight = 0.8f }
                }
            },
            new()
            {
                FromTileId = "flowers.basic",
                Direction = "right",
                PossibleConnections = new List<TileConnectionWeight>
                {
                    new() { ToTileId = "flowers.basic", Weight = 1.0f },
                    new() { ToTileId = "grass.basic", Weight = 0.8f }
                }
            },
            
            // Grass can connect to flowers with slightly lower weight
            new()
            {
                FromTileId = "grass.basic",
                Direction = "up",
                PossibleConnections = new List<TileConnectionWeight>
                {
                    new() { ToTileId = "flowers.basic", Weight = 0.7f }
                }
            },
            new()
            {
                FromTileId = "grass.basic",
                Direction = "down",
                PossibleConnections = new List<TileConnectionWeight>
                {
                    new() { ToTileId = "flowers.basic", Weight = 0.7f }
                }
            },
            new()
            {
                FromTileId = "grass.basic",
                Direction = "left",
                PossibleConnections = new List<TileConnectionWeight>
                {
                    new() { ToTileId = "flowers.basic", Weight = 0.7f }
                }
            },
            new()
            {
                FromTileId = "grass.basic",
                Direction = "right",
                PossibleConnections = new List<TileConnectionWeight>
                {
                    new() { ToTileId = "flowers.basic", Weight = 0.7f }
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