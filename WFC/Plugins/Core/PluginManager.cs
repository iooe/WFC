﻿using System.IO;
using System.Reflection;
using WFC.Models;

namespace WFC.Plugins;

/// <summary>
/// Manager for discovering and loading plugins
/// </summary>
public class PluginManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<IPlugin> _plugins = new();
    private readonly Dictionary<string, TileDefinition> _tileDefinitions = new();
    private readonly List<TileRuleDefinition> _ruleDefinitions = new();
    
    public PluginManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    public IReadOnlyList<IPlugin> Plugins => _plugins.AsReadOnly();
    
    /// <summary>
    /// Get all enabled tile set plugins
    /// </summary>
    public IEnumerable<ITileSetPlugin> TileSetPlugins => 
        _plugins.OfType<ITileSetPlugin>().Where(p => p.Enabled);
        
    /// <summary>
    /// Get all enabled generation hook plugins
    /// </summary>
    public IEnumerable<IGenerationHookPlugin> GenerationHookPlugins =>
        _plugins.OfType<IGenerationHookPlugin>().Where(p => p.Enabled);
        
    /// <summary>
    /// Get all enabled post-processor plugins ordered by priority
    /// </summary>
    public IEnumerable<IPostProcessorPlugin> PostProcessorPlugins =>
        _plugins.OfType<IPostProcessorPlugin>().Where(p => p.Enabled).OrderBy(p => p.Priority);
    
    /// <summary>
    /// Get all tile definitions from loaded plugins
    /// </summary>
    public IReadOnlyDictionary<string, TileDefinition> TileDefinitions => 
        _tileDefinitions;
    
    /// <summary>
    /// Get all rule definitions from loaded plugins
    /// </summary>
    public IReadOnlyList<TileRuleDefinition> RuleDefinitions => 
        _ruleDefinitions.AsReadOnly();
    
    /// <summary>
    /// Load all plugins from the plugins directory
    /// </summary>
    public void LoadPlugins()
    {
        // Clear existing plugins
        _plugins.Clear();
        _tileDefinitions.Clear();
        _ruleDefinitions.Clear();
        
        Console.WriteLine("Loading plugins...");

        
        // Get plugin directory
        string pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        if (!Directory.Exists(pluginDir))
        {
            Directory.CreateDirectory(pluginDir);
            Console.WriteLine($"Created plugins directory: {pluginDir}");
            return;
        }
        
        // Load built-in plugins first
        LoadBuiltInPlugins();
        
        // Then load external plugins
        LoadExternalPlugins(pluginDir);
        
        // Initialize plugins
        foreach (var plugin in _plugins)
        {
            try
            {
                plugin.Initialize(_serviceProvider);
                plugin.Enabled = true; // По умолчанию все плагины включены
                Console.WriteLine($"Initialized plugin: {plugin.Name} ({plugin.Id})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize plugin {plugin.Name}: {ex.Message}");
            }
        }
        
        // Load plugin preferences (enabled/disabled state)
        LoadPluginPreferences();
        
        // Refresh tile definitions
        RefreshTileDefinitions();
    }
    
    /// <summary>
    /// Refresh tile and rule definitions based on enabled plugins
    /// </summary>
    public void RefreshTileDefinitions()
    {
        _tileDefinitions.Clear();
        _ruleDefinitions.Clear();
        
        // Load tile definitions from enabled plugins only
        LoadTileDefinitions();
        
        // Load rule definitions from enabled plugins only
        LoadRuleDefinitions();
        
        Console.WriteLine($"Loaded {_tileDefinitions.Count} tile definitions from enabled plugins");
    
        if (_tileDefinitions.Count == 0)
        {
            Console.WriteLine("WARNING: No tile definitions loaded! Application will likely fail.");
        }
    }
    
    /// <summary>
    /// Load built-in plugins
    /// </summary>
    private void LoadBuiltInPlugins()
    {
        // Get types from current assembly that implement IPlugin
        var pluginTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && typeof(IPlugin).IsAssignableFrom(t))
            .ToList();
            
        foreach (var pluginType in pluginTypes)
        {
            try
            {
                // Create instance
                var plugin = (IPlugin)Activator.CreateInstance(pluginType);
                _plugins.Add(plugin);
                Console.WriteLine($"Loaded built-in plugin: {plugin.Name} ({plugin.Id})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load built-in plugin {pluginType.Name}: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Load external plugins from the plugins directory
    /// </summary>
    private void LoadExternalPlugins(string pluginDir)
    {
        // Get all DLL files
        var pluginFiles = Directory.GetFiles(pluginDir, "*.dll");
        
        foreach (var pluginFile in pluginFiles)
        {
            try
            {
                // Load assembly
                var assembly = Assembly.LoadFrom(pluginFile);
                
                // Get plugin types
                var pluginTypes = assembly.GetTypes()
                    .Where(t => !t.IsInterface && !t.IsAbstract && typeof(IPlugin).IsAssignableFrom(t))
                    .ToList();
                    
                foreach (var pluginType in pluginTypes)
                {
                    try
                    {
                        // Create instance
                        var plugin = (IPlugin)Activator.CreateInstance(pluginType);
                        _plugins.Add(plugin);
                        Console.WriteLine($"Loaded external plugin: {plugin.Name} ({plugin.Id})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load plugin {pluginType.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load plugin assembly {pluginFile}: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Load plugin preferences (enabled/disabled state)
    /// </summary>
    private void LoadPluginPreferences()
    {
        try
        {
            string prefsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "plugins.json");
            if (File.Exists(prefsPath))
            {
                string json = File.ReadAllText(prefsPath);
                var prefs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                
                if (prefs != null)
                {
                    foreach (var plugin in _plugins)
                    {
                        if (prefs.TryGetValue(plugin.Id, out bool enabled))
                        {
                            plugin.Enabled = enabled;
                        }
                    }
                }
                
                Console.WriteLine("Loaded plugin preferences");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading plugin preferences: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Save plugin preferences (enabled/disabled state)
    /// </summary>
    public void SavePluginPreferences()
    {
        try
        {
            string configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
            
            string prefsPath = Path.Combine(configDir, "plugins.json");
            
            var prefs = _plugins.ToDictionary(p => p.Id, p => p.Enabled);
            string json = System.Text.Json.JsonSerializer.Serialize(prefs, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText(prefsPath, json);
            Console.WriteLine("Saved plugin preferences");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving plugin preferences: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Toggle a plugin's enabled state and refresh tile definitions
    /// </summary>
    public void TogglePlugin(string pluginId, bool enabled)
    {
        var plugin = _plugins.FirstOrDefault(p => p.Id == pluginId);
        if (plugin != null)
        {
            // Изменяем состояние
            plugin.Enabled = enabled;
        
            // Полностью обновляем определения плиток и правил
            RefreshTileDefinitions();
        
            // Сохраняем настройки
            SavePluginPreferences();
        
            Console.WriteLine($"Plugin {plugin.Name} ({plugin.Id}) {(enabled ? "enabled" : "disabled")}");
        }
    }
    
    /// <summary>
    /// Load tile definitions from plugins
    /// </summary>
    private void LoadTileDefinitions()
    {
        foreach (var tileSetPlugin in TileSetPlugins)
        {
            try
            {
                var definitions = tileSetPlugin.GetTileDefinitions();
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load tile definitions from plugin {tileSetPlugin.Name}: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Load rule definitions from plugins
    /// </summary>
    private void LoadRuleDefinitions()
    {
        foreach (var tileSetPlugin in TileSetPlugins)
        {
            try
            {
                var definitions = tileSetPlugin.GetRuleDefinitions();
                if (definitions != null)
                {
                    foreach (var rule in definitions)
                    {
                        if (rule != null)
                        {
                            _ruleDefinitions.Add(rule);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load rule definitions from plugin {tileSetPlugin.Name}: {ex.Message}");
            }
        }
    }
}