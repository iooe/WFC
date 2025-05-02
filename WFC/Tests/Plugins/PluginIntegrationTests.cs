using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WFC.Models;
using WFC.Plugins;
using WFC.Services;
using WFC.Services.Export;

namespace WFC.Tests.Plugins;

[TestClass]
public class PluginIntegrationTests
{
    private TileConfigManager _tileConfigManager;
    private PluginManager _pluginManager;
    private Mock<IServiceProvider> _mockServiceProvider;
    
    [TestInitialize]
    public void Setup()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _pluginManager = new PluginManager(_mockServiceProvider.Object);
        
        var mockTileFactory = new Mock<ITileFactory>();
        mockTileFactory.Setup(f => f.CreateTile(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns((int id, string tileId, string name, string path, string category, Dictionary<string, string> props) => 
                new Tile(id, tileId, name, path, category, props));
                
        _tileConfigManager = new TileConfigManager(_pluginManager, mockTileFactory.Object);
    }
    
    [TestMethod]
    public void WhenPluginsAreLoaded_TileDefinitionsAreAvailable()
    {
        // Arrange
        var mockPlugin = new MockTileSetPlugin();
        mockPlugin.Enabled = true; // Make sure plugin is enabled
        
        // Clear any existing plugins from manager
        var pluginsField = typeof(PluginManager).GetField("_plugins", BindingFlags.NonPublic | BindingFlags.Instance);
        var plugins = new List<IPlugin>();
        pluginsField.SetValue(_pluginManager, plugins);
        
        // Add our mock plugin
        plugins.Add(mockPlugin);
        
        // Initialize tile configuration
        _tileConfigManager.Initialize();
        
        // Act
        var tileDefinitions = _tileConfigManager.GetTileDefinitions();
        
        // Assert
        Assert.IsNotNull(tileDefinitions, "Tile definitions should not be null");
        Assert.IsTrue(tileDefinitions.ContainsKey("test.tile1"), "Should contain test.tile1");
        Assert.IsTrue(tileDefinitions.ContainsKey("test.tile2"), "Should contain test.tile2");
        Assert.AreEqual(2, tileDefinitions.Count, "Should have exactly 2 tile definitions");
    }
    
    [TestMethod]
    public void WhenPluginIsDisabled_ItsDefinitionsAreNotIncluded()
    {
        // Arrange
        var mockPlugin = new MockTileSetPlugin();
        mockPlugin.Enabled = false; // Explicitly disable the plugin
        
        // Clear any existing plugins from manager
        var pluginsField = typeof(PluginManager).GetField("_plugins", BindingFlags.NonPublic | BindingFlags.Instance);
        var plugins = new List<IPlugin>();
        pluginsField.SetValue(_pluginManager, plugins);
        
        // Add our mock plugin
        plugins.Add(mockPlugin);
        
        // Act
        _tileConfigManager.Initialize();
        var tileDefinitions = _tileConfigManager.GetTileDefinitions();
        
        // Assert
        Assert.IsNotNull(tileDefinitions, "Tile definitions should not be null");
        Assert.AreEqual(0, tileDefinitions.Count, "Should have 0 tile definitions since plugin is disabled");
    }
    
    [TestMethod]
    public void CreateSettings_CreatesValidWFCSettings()
    {
        // Arrange
        var mockPlugin = new MockTileSetPlugin();
        mockPlugin.Enabled = true; // Make sure plugin is enabled
        
        // Clear any existing plugins from manager
        var pluginsField = typeof(PluginManager).GetField("_plugins", BindingFlags.NonPublic | BindingFlags.Instance);
        var plugins = new List<IPlugin>();
        pluginsField.SetValue(_pluginManager, plugins);
        
        // Add our mock plugin
        plugins.Add(mockPlugin);
        
        // Initialize tile configuration
        _tileConfigManager.Initialize();
        
        // Act
        var settings = _tileConfigManager.CreateSettings(10, 10);
        
        // Assert
        Assert.IsNotNull(settings, "Settings should not be null");
        Assert.AreEqual(10, settings.Width, "Width should be 10");
        Assert.AreEqual(10, settings.Height, "Height should be 10");
        Assert.IsNotNull(settings.Tiles, "Tiles should not be null");
        Assert.AreEqual(2, settings.Tiles.Count, "Should have 2 tiles from mock plugin");
        Assert.IsNotNull(settings.TileIndexMap, "TileIndexMap should not be null");
        Assert.AreEqual(2, settings.TileIndexMap.Count, "Should have 2 entries in TileIndexMap");
        Assert.IsNotNull(settings.Rules, "Rules should not be null");
        Assert.IsTrue(settings.Rules.Count > 0, "Should have at least one rule");
    }
    
    // Mock implementation for testing
    private class MockTileSetPlugin : ITileSetPlugin
    {
        public string Id => "test.plugin";
        public string Name => "Test Plugin";
        public string Version => "1.0";
        public string Description => "Test Plugin for Unit Testing";
        public bool Enabled { get; set; } = true;
        
        public void Initialize(IServiceProvider serviceProvider) { }
        
        public IEnumerable<TileDefinition> GetTileDefinitions()
        {
            return new List<TileDefinition>
            {
                new TileDefinition 
                { 
                    Id = "test.tile1", 
                    Name = "Test Tile 1",
                    Category = "test",
                    ResourcePath = "test/tile1" 
                },
                new TileDefinition 
                { 
                    Id = "test.tile2", 
                    Name = "Test Tile 2",
                    Category = "test",
                    ResourcePath = "test/tile2" 
                }
            };
        }
        
        public IEnumerable<TileRuleDefinition> GetRuleDefinitions()
        {
            return new List<TileRuleDefinition>
            {
                new TileRuleDefinition
                {
                    FromTileId = "test.tile1",
                    Direction = "up",
                    PossibleConnections = new List<TileConnectionWeight>
                    {
                        new TileConnectionWeight { ToTileId = "test.tile1", Weight = 1.0f },
                        new TileConnectionWeight { ToTileId = "test.tile2", Weight = 0.5f }
                    }
                },
                new TileRuleDefinition
                {
                    FromTileId = "test.tile2",
                    Direction = "up",
                    PossibleConnections = new List<TileConnectionWeight>
                    {
                        new TileConnectionWeight { ToTileId = "test.tile1", Weight = 0.5f },
                        new TileConnectionWeight { ToTileId = "test.tile2", Weight = 1.0f }
                    }
                }
            };
        }
    }
}