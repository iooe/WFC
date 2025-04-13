using WFC.Models;

namespace WFC.Plugins.Basic;

/// <summary>
/// Plugin providing pavement tiles
/// </summary>
public class PavementPlugin : ITileSetPlugin
{
    public string Id => "wfc.basic.pavement";
    public string Name => "Pavement";
    public string Version => "1.0";
    public string Description => "Provides pavement tiles and transitions";
    
    private List<TileDefinition> _tileDefinitions;
    private List<TileRuleDefinition> _ruleDefinitions;
    
    public void Initialize(IServiceProvider serviceProvider)
    {
        // Create tile definitions
        _tileDefinitions = new List<TileDefinition>
        {
            // Basic pavement
            new()
            {
                Id = "pavement.basic",
                Name = "Pavement",
                Category = "pavement",
                ResourcePath = "pavement",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "true" },
                    { "surface", "pavement" }
                }
            },
            
            // Transition tiles - pavement and grass
            new()
            {
                Id = "pavement.grass.left",
                Name = "Pavement-Grass Left",
                Category = "transition",
                ResourcePath = "pavement-transitions/left",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "true" },
                    { "transition", "pavement-grass" }
                }
            },
            new()
            {
                Id = "pavement.grass.right",
                Name = "Pavement-Grass Right",
                Category = "transition",
                ResourcePath = "pavement-transitions/right",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "true" },
                    { "transition", "pavement-grass" }
                }
            },
            new()
            {
                Id = "pavement.grass.top",
                Name = "Pavement-Grass Top",
                Category = "transition",
                ResourcePath = "pavement-transitions/top",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "true" },
                    { "transition", "pavement-grass" }
                }
            },
            new()
            {
                Id = "pavement.grass.bottom",
                Name = "Pavement-Grass Bottom",
                Category = "transition",
                ResourcePath = "pavement-transitions/bottom",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "true" },
                    { "transition", "pavement-grass" }
                }
            },
            
            // Corner transitions
            new()
            {
                Id = "pavement.grass.topleft",
                Name = "Pavement-Grass Top-Left",
                Category = "transition",
                ResourcePath = "pavement-transitions/top-left",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "true" },
                    { "transition", "pavement-grass" }
                }
            },
            new()
            {
                Id = "pavement.grass.topright",
                Name = "Pavement-Grass Top-Right",
                Category = "transition",
                ResourcePath = "pavement-transitions/top-right",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "true" },
                    { "transition", "pavement-grass" }
                }
            },
            new()
            {
                Id = "pavement.grass.bottomleft",
                Name = "Pavement-Grass Bottom-Left",
                Category = "transition",
                ResourcePath = "pavement-transitions/bottom-left",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "true" },
                    { "transition", "pavement-grass" }
                }
            },
            new()
            {
                Id = "pavement.grass.bottomright",
                Name = "Pavement-Grass Bottom-Right",
                Category = "transition",
                ResourcePath = "pavement-transitions/bottom-right",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "true" },
                    { "transition", "pavement-grass" }
                }
            }
        };
        
        // Create rule definitions
        _ruleDefinitions = CreateRuleDefinitions();
    }
    
    private List<TileRuleDefinition> CreateRuleDefinitions()
    {
        var rules = new List<TileRuleDefinition>();
        
        // Basic pavement rules
        AddPavementRules(rules);
        
        // Transition rules
        AddTransitionRules(rules);
        
        return rules;
    }
    
    private void AddPavementRules(List<TileRuleDefinition> rules)
    {
        // Pavement to pavement connections
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.basic",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.top", Weight = 0.4f },
                new() { ToTileId = "pavement.grass.topleft", Weight = 0.3f },
                new() { ToTileId = "pavement.grass.topright", Weight = 0.3f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.basic",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.bottom", Weight = 0.4f },
                new() { ToTileId = "pavement.grass.bottomleft", Weight = 0.3f },
                new() { ToTileId = "pavement.grass.bottomright", Weight = 0.3f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.basic",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.left", Weight = 0.4f },
                new() { ToTileId = "pavement.grass.topleft", Weight = 0.3f },
                new() { ToTileId = "pavement.grass.bottomleft", Weight = 0.3f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.basic",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.right", Weight = 0.4f },
                new() { ToTileId = "pavement.grass.topright", Weight = 0.3f },
                new() { ToTileId = "pavement.grass.bottomright", Weight = 0.3f }
            }
        });
        
        // Allow grass to connect to pavement transitions
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "grass.basic",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.grass.bottom", Weight = 0.3f },
                new() { ToTileId = "pavement.grass.bottomleft", Weight = 0.2f },
                new() { ToTileId = "pavement.grass.bottomright", Weight = 0.2f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "grass.basic",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.grass.top", Weight = 0.3f },
                new() { ToTileId = "pavement.grass.topleft", Weight = 0.2f },
                new() { ToTileId = "pavement.grass.topright", Weight = 0.2f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "grass.basic",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.grass.right", Weight = 0.3f },
                new() { ToTileId = "pavement.grass.topright", Weight = 0.2f },
                new() { ToTileId = "pavement.grass.bottomright", Weight = 0.2f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "grass.basic",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.grass.left", Weight = 0.3f },
                new() { ToTileId = "pavement.grass.topleft", Weight = 0.2f },
                new() { ToTileId = "pavement.grass.bottomleft", Weight = 0.2f }
            }
        });
    }
    
    private void AddTransitionRules(List<TileRuleDefinition> rules)
    {
        // Left transition (Pavement with grass on left)
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.left",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "grass.basic", Weight = 1.0f },
                new() { ToTileId = "flowers.basic", Weight = 0.8f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.left",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.right", Weight = 0.2f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.left",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.7f },
                new() { ToTileId = "pavement.grass.left", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.topleft", Weight = 0.5f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.left",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.7f },
                new() { ToTileId = "pavement.grass.left", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.bottomleft", Weight = 0.5f }
            }
        });
        
        // Right transition (Pavement with grass on right)
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.right",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "grass.basic", Weight = 1.0f },
                new() { ToTileId = "flowers.basic", Weight = 0.8f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.right",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.left", Weight = 0.2f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.right",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.7f },
                new() { ToTileId = "pavement.grass.right", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.topright", Weight = 0.5f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.right",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.7f },
                new() { ToTileId = "pavement.grass.right", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.bottomright", Weight = 0.5f }
            }
        });
        
        // Top transition (Pavement with grass on top)
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.top",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "grass.basic", Weight = 1.0f },
                new() { ToTileId = "flowers.basic", Weight = 0.8f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.top",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.bottom", Weight = 0.2f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.top",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.7f },
                new() { ToTileId = "pavement.grass.top", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.topleft", Weight = 0.5f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.top",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.7f },
                new() { ToTileId = "pavement.grass.top", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.topright", Weight = 0.5f }
            }
        });
        
        // Bottom transition (Pavement with grass on bottom)
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.bottom",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "grass.basic", Weight = 1.0f },
                new() { ToTileId = "flowers.basic", Weight = 0.8f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.bottom",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.top", Weight = 0.2f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.bottom",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.7f },
                new() { ToTileId = "pavement.grass.bottom", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.bottomleft", Weight = 0.5f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.bottom",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.7f },
                new() { ToTileId = "pavement.grass.bottom", Weight = 1.0f },
                new() { ToTileId = "pavement.grass.bottomright", Weight = 0.5f }
            }
        });
        
        // Top-left corner transition
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.topleft",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "grass.basic", Weight = 1.0f },
                new() { ToTileId = "flowers.basic", Weight = 0.8f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.topleft",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "grass.basic", Weight = 1.0f },
                new() { ToTileId = "flowers.basic", Weight = 0.8f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.topleft",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.8f },
                new() { ToTileId = "pavement.grass.top", Weight = 1.0f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.topleft",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.8f },
                new() { ToTileId = "pavement.grass.left", Weight = 1.0f }
            }
        });
        
        // Top-right corner transition
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.topright",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "grass.basic", Weight = 1.0f },
                new() { ToTileId = "flowers.basic", Weight = 0.8f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.topright",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "grass.basic", Weight = 1.0f },
                new() { ToTileId = "flowers.basic", Weight = 0.8f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.topright",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.8f },
                new() { ToTileId = "pavement.grass.top", Weight = 1.0f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.topright",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.8f },
                new() { ToTileId = "pavement.grass.right", Weight = 1.0f }
            }
        });
        
        // Bottom-left corner transition
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.bottomleft",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "grass.basic", Weight = 1.0f },
                new() { ToTileId = "flowers.basic", Weight = 0.8f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.bottomleft",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "grass.basic", Weight = 1.0f },
                new() { ToTileId = "flowers.basic", Weight = 0.8f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.bottomleft",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.8f },
                new() { ToTileId = "pavement.grass.bottom", Weight = 1.0f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.bottomleft",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.8f },
                new() { ToTileId = "pavement.grass.left", Weight = 1.0f }
            }
        });
        
        // Bottom-right corner transition
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.bottomright",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "grass.basic", Weight = 1.0f },
                new() { ToTileId = "flowers.basic", Weight = 0.8f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.bottomright",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "grass.basic", Weight = 1.0f },
                new() { ToTileId = "flowers.basic", Weight = 0.8f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.bottomright",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.8f },
                new() { ToTileId = "pavement.grass.bottom", Weight = 1.0f }
            }
        });
        
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "pavement.grass.bottomright",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 0.8f },
                new() { ToTileId = "pavement.grass.right", Weight = 1.0f }
            }
        });
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