using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WFC.Models;
using WFC.Plugins;
using WFC.Services;
using WFC.Services.Export;

namespace WFC.Tests;

[TestClass]

// Это пометка для себя; Полный цикл генерации WFC с плагинами
public class CoreIntegrationTests
{
    private TileConfigManager _tileConfigManager;
    private PluginManager _pluginManager;
    private Mock<IServiceProvider> _mockServiceProvider;
    private DefaultWFCService _wfcService;

    [TestInitialize]
    public void Setup()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _pluginManager = new PluginManager(_mockServiceProvider.Object);
        _wfcService = new DefaultWFCService(_pluginManager);

        var mockTileFactory = new Mock<ITileFactory>();
        mockTileFactory.Setup(f => f.CreateTile(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns((int id, string tileId, string name, string path, string category, Dictionary<string, string> props) => 
                new Tile(id, tileId, name, path, category, props));
                
        _tileConfigManager = new TileConfigManager(_pluginManager, mockTileFactory.Object);
    }
    
    [TestMethod]
    public async Task GenerateMap_WithPlugins_Success()
    {
        // Arrange
        _pluginManager.LoadPlugins();
        _tileConfigManager.Initialize();
        
        // Act
        var settings = _tileConfigManager.CreateSettings(10, 10);
        var result = await _wfcService.GenerateAsync(settings);
        
        // Assert
        Assert.IsTrue(result.Success, "Generation failed");
        Assert.IsNotNull(result.Grid, "Grid is null");
        Assert.AreEqual(10, result.Grid.GetLength(0), "Wrong grid width");
        Assert.AreEqual(10, result.Grid.GetLength(1), "Wrong grid height");
        
        // Verify all cells are filled
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                Assert.IsNotNull(result.Grid[x, y], $"Cell at ({x},{y}) is null");
            }
        }
    }
    
    [TestMethod]
    public void TogglePlugin_AffectsTileDefinitions()
    {
        // Arrange
        _pluginManager.LoadPlugins();
        var grassPlugin = _pluginManager.Plugins.First(p => p.Id == "wfc.basic.grass");
    
        // Act - Disable plugin
        _pluginManager.TogglePlugin(grassPlugin.Id, false);
        _tileConfigManager.Initialize();
        var tileDefinitionsWithoutGrass = _tileConfigManager.GetTileDefinitions();
    
        // Assert - No grass tiles
        Assert.IsFalse(tileDefinitionsWithoutGrass.ContainsKey("grass.basic"));
    
        // Act - Enable plugin back
        _pluginManager.TogglePlugin(grassPlugin.Id, true);
        _tileConfigManager.Initialize();
        var tileDefinitionsWithGrass = _tileConfigManager.GetTileDefinitions();
    
        // Assert - Grass tiles restored
        Assert.IsTrue(tileDefinitionsWithGrass.ContainsKey("grass.basic"));
    }
    
}