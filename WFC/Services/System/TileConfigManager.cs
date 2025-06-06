﻿using System.IO;
using System.Text.Json;
using WFC.Models;
using WFC.Plugins;

namespace WFC.Services.Export;

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
        // Clear existing definitions
        _tileDefinitions = new Dictionary<string, TileDefinition>();
        _ruleDefinitions = new List<TileRuleDefinition>();
    
        // Load tile definitions only from active plugins
        foreach (var plugin in _pluginManager.TileSetPlugins)
        {
            try
            {
                if (!plugin.Enabled)
                    continue;
                    
                var definitions = plugin.GetTileDefinitions();
                if (definitions != null)
                {
                    foreach (var definition in definitions)
                    {
                        if (definition != null && !string.IsNullOrEmpty(definition.Id))
                        {
                            // Add or replace existing definition
                            _tileDefinitions[definition.Id] = definition;
                        }
                    }
                }
                //Console.WriteLine($"Loaded {definitions.Count()} tile definitions from plugin {plugin.Name}");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error loading tile definitions from plugin {plugin.Name}: {ex.Message}");
            }
        }

        // Load rule definitions only from active plugins
        foreach (var plugin in _pluginManager.TileSetPlugins)
        {
            try
            {
                if (!plugin.Enabled)
                    continue;
                    
                var definitions = plugin.GetRuleDefinitions();
                if (definitions != null)
                {
                    _ruleDefinitions.AddRange(definitions);
                }
                //Console.WriteLine($"Loaded {definitions.Count()} rule definitions from plugin {plugin.Name}");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error loading rule definitions from plugin {plugin.Name}: {ex.Message}");
            }
        }

        // Load any additional configurations from file (optional in tests)
        try
        {
            LoadConfigFiles();
        }
        catch (Exception)
        {
            // Ignore errors in test environment
        }
    
        //Console.WriteLine($"Tile configuration initialized with {_tileDefinitions.Count} tiles and {_ruleDefinitions.Count} rules");
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
            Seed = seed,
            PluginSettings = new Dictionary<string, object>()
        };

        // Create tiles from definitions
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

                //Console.WriteLine($"Added tile: {definition.Id} with index {index - 1}");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error creating tile {definition.Id}: {ex.Message}");
            }
        }

        settings.Tiles = tiles;
        settings.TileIndexMap = tileIndexMap;

        // Create rules from definitions with additional checking
        var rules = new Dictionary<(int fromState, string direction), List<(int toState, float weight)>>();

        foreach (var ruleDef in _ruleDefinitions)
        {
            try
            {
                // Skip rules for undefined tiles
                if (ruleDef == null || string.IsNullOrEmpty(ruleDef.FromTileId) || 
                    !tileIndexMap.TryGetValue(ruleDef.FromTileId, out int fromState))
                {
                    //Console.WriteLine($"Warning: Rule references unknown tile ID: {ruleDef?.FromTileId}");
                    continue;
                }

                var key = (fromState, ruleDef.Direction);
                var connections = new List<(int toState, float weight)>();

                if (ruleDef.PossibleConnections != null)
                {
                    foreach (var conn in ruleDef.PossibleConnections)
                    {
                        // Skip connections with undefined tiles
                        if (conn == null || string.IsNullOrEmpty(conn.ToTileId) || 
                            !tileIndexMap.TryGetValue(conn.ToTileId, out int toState))
                        {
                            //Console.WriteLine($"Warning: Connection references unknown tile ID: {conn?.ToTileId}");
                            continue;
                        }

                        connections.Add((toState, conn.Weight));
                    }
                }

                // Add rule only if there are valid connections
                if (connections.Count > 0)
                {
                    rules[key] = connections;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error processing rule for {ruleDef?.FromTileId}: {ex.Message}");
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

                if (definitions != null)
                {
                    foreach (var def in definitions)
                    {
                        if (def != null && !string.IsNullOrEmpty(def.Id))
                        {
                            _tileDefinitions[def.Id] = def;
                        }
                    }
                }

                //Console.WriteLine($"Loaded {definitions?.Count ?? 0} tile definitions from config file");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error loading tile definitions: {ex.Message}");
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

                if (definitions != null)
                {
                    _ruleDefinitions.AddRange(definitions);
                }

                //Console.WriteLine($"Loaded {definitions?.Count ?? 0} rule definitions from config file");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error loading rule definitions: {ex.Message}");
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
            //Console.WriteLine($"Saved {_tileDefinitions.Count} tile definitions to config file");
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"Error saving tile definitions: {ex.Message}");
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
            //Console.WriteLine($"Saved {_ruleDefinitions.Count} rule definitions to config file");
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"Error saving rule definitions: {ex.Message}");
        }
    }
}