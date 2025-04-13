using System.IO;
using System.Text.Json;
using WFC.Models;
using WFC.Plugins;

namespace WFC.Services;

/// <summary>
/// Manager for tile configurations
/// </summary>
public class TileConfigManager
{
    private readonly PluginManager _pluginManager;
    private readonly ITileFactory _tileFactory;

    private Dictionary<string, TileDefinition> _tileDefinitions = new();
    private List<TileRuleDefinition> _ruleDefinitions = new();

    public TileConfigManager(PluginManager pluginManager, ITileFactory tileFactory)
    {
        _pluginManager = pluginManager;
        _tileFactory = tileFactory;
    }

    /// <summary>
    /// Initialize the tile configuration
    /// </summary>
    public void Initialize()
    {
        // Load tile definitions from plugins
        _tileDefinitions = new Dictionary<string, TileDefinition>(_pluginManager.TileDefinitions);

        // Load rule definitions from plugins
        _ruleDefinitions = new List<TileRuleDefinition>(_pluginManager.RuleDefinitions);

        // Load any additional configurations from file
        LoadConfigFiles();
    }

    /// <summary>
    /// Create settings for a new WFC generation
    /// </summary>
    public WFCSettings CreateSettings(int width, int height, bool enableDebugRendering = false, int? seed = null)
    {
        var settings = new WFCSettings
        {
            Width = width,
            Height = height,
            EnableDebugRendering = enableDebugRendering,
            Seed = seed
        };

        // Создаем плитки из определений
        var tiles = new List<Tile>();
        var tileIndexMap = new Dictionary<string, int>();

        int index = 0;
        foreach (var definition in _tileDefinitions.Values)
        {
            try
            {
                var tile = _tileFactory.CreateTile(
                    index,
                    definition.Id,
                    definition.Name,
                    definition.ResourcePath,
                    definition.Category,
                    definition.Properties
                );

                tiles.Add(tile);
                tileIndexMap[definition.Id] = index;
                index++;

                Console.WriteLine($"Added tile: {definition.Id} with index {index - 1}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating tile {definition.Id}: {ex.Message}");
            }
        }

        settings.Tiles = tiles;
        settings.TileIndexMap = tileIndexMap;

        // Создаем правила из определений с дополнительной проверкой
        var rules = new Dictionary<(int fromState, string direction), List<(int toState, float weight)>>();

        foreach (var ruleDef in _ruleDefinitions)
        {
            try
            {
                // Пропускаем правила для неопределенных плиток
                if (!tileIndexMap.TryGetValue(ruleDef.FromTileId, out int fromState))
                {
                    Console.WriteLine($"Warning: Rule references unknown tile ID: {ruleDef.FromTileId}");
                    continue;
                }

                var key = (fromState, ruleDef.Direction);
                var connections = new List<(int toState, float weight)>();

                foreach (var conn in ruleDef.PossibleConnections)
                {
                    // Пропускаем соединения с неопределенными плитками
                    if (!tileIndexMap.TryGetValue(conn.ToTileId, out int toState))
                    {
                        Console.WriteLine($"Warning: Connection references unknown tile ID: {conn.ToTileId}");
                        continue;
                    }

                    connections.Add((toState, conn.Weight));
                }

                // Добавляем правило только если есть валидные соединения
                if (connections.Count > 0)
                {
                    rules[key] = connections;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing rule for {ruleDef.FromTileId}: {ex.Message}");
            }
        }

        settings.Rules = rules;

        return settings;
    }

    /// <summary>
    /// Get all available tile definitions
    /// </summary>
    public IReadOnlyDictionary<string, TileDefinition> GetTileDefinitions()
    {
        return _tileDefinitions;
    }

    /// <summary>
    /// Get all rule definitions
    /// </summary>
    public IReadOnlyList<TileRuleDefinition> GetRuleDefinitions()
    {
        return _ruleDefinitions;
    }

    /// <summary>
    /// Load additional configuration files
    /// </summary>
    private void LoadConfigFiles()
    {
        string configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
            return;
        }

        // Load tile definitions
        string tilesPath = Path.Combine(configDir, "tiles.json");
        if (File.Exists(tilesPath))
        {
            try
            {
                string json = File.ReadAllText(tilesPath);
                var definitions = JsonSerializer.Deserialize<List<TileDefinition>>(json);

                foreach (var def in definitions)
                {
                    _tileDefinitions[def.Id] = def;
                }

                Console.WriteLine($"Loaded {definitions.Count} tile definitions from config file");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading tile definitions: {ex.Message}");
            }
        }

        // Load rule definitions
        string rulesPath = Path.Combine(configDir, "rules.json");
        if (File.Exists(rulesPath))
        {
            try
            {
                string json = File.ReadAllText(rulesPath);
                var definitions = JsonSerializer.Deserialize<List<TileRuleDefinition>>(json);

                _ruleDefinitions.AddRange(definitions);

                Console.WriteLine($"Loaded {definitions.Count} rule definitions from config file");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading rule definitions: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Save current configuration to files
    /// </summary>
    public void SaveConfigFiles()
    {
        string configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

        // Save tile definitions
        string tilesPath = Path.Combine(configDir, "tiles.json");
        try
        {
            string json = JsonSerializer.Serialize(_tileDefinitions.Values, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(tilesPath, json);
            Console.WriteLine($"Saved {_tileDefinitions.Count} tile definitions to config file");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving tile definitions: {ex.Message}");
        }

        // Save rule definitions
        string rulesPath = Path.Combine(configDir, "rules.json");
        try
        {
            string json = JsonSerializer.Serialize(_ruleDefinitions, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(rulesPath, json);
            Console.WriteLine($"Saved {_ruleDefinitions.Count} rule definitions to config file");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving rule definitions: {ex.Message}");
        }
    }
}