using System.IO;
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
    /// Get all tile set plugins
    /// </summary>
    public IEnumerable<ITileSetPlugin> TileSetPlugins => 
        _plugins.OfType<ITileSetPlugin>();
        
    /// <summary>
    /// Get all generation hook plugins
    /// </summary>
    public IEnumerable<IGenerationHookPlugin> GenerationHookPlugins =>
        _plugins.OfType<IGenerationHookPlugin>();
        
    /// <summary>
    /// Get all post-processor plugins ordered by priority
    /// </summary>
    public IEnumerable<IPostProcessorPlugin> PostProcessorPlugins =>
        _plugins.OfType<IPostProcessorPlugin>().OrderBy(p => p.Priority);
    
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
                Console.WriteLine($"Initialized plugin: {plugin.Name} ({plugin.Id})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize plugin {plugin.Name}: {ex.Message}");
            }
        }
        
        // Load tile definitions from plugins
        LoadTileDefinitions();
        
        // Load rule definitions from plugins
        LoadRuleDefinitions();
        
        Console.WriteLine($"Loaded {_tileDefinitions.Count} tile definitions from plugins");
    
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
    /// Load tile definitions from plugins
    /// </summary>
    private void LoadTileDefinitions()
    {
        foreach (var tileSetPlugin in TileSetPlugins)
        {
            try
            {
                var definitions = tileSetPlugin.GetTileDefinitions();
                foreach (var definition in definitions)
                {
                    // Add or replace existing definition
                    _tileDefinitions[definition.Id] = definition;
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
                _ruleDefinitions.AddRange(definitions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load rule definitions from plugin {tileSetPlugin.Name}: {ex.Message}");
            }
        }
    }
}