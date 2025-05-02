using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WFC.Models;
using WFC.Plugins;
using WFC.Services;

namespace WFC.Tests.Services;

[TestClass]
public class WFCServiceTests
{
    private Mock<PluginManager> _mockPluginManager;
    private IWFCService _wfcService;
    
    [TestInitialize]
    public void Setup()
    {
        _mockPluginManager = new Mock<PluginManager>(MockBehavior.Strict);
        _wfcService = new DefaultWFCService(_mockPluginManager.Object);
    }
    
    [TestMethod]
    public async Task GenerateAsync_WithValidSettings_ReturnsSuccessResult()
    {
        // Arrange
        var settings = CreateValidSettings();
        _mockPluginManager.Setup(m => m.GenerationHookPlugins).Returns(new List<IGenerationHookPlugin>());
        _mockPluginManager.Setup(m => m.PostProcessorPlugins).Returns(new List<IPostProcessorPlugin>());
        
        // Act
        var result = await _wfcService.GenerateAsync(settings);
        
        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Grid);
        Assert.IsNull(result.ErrorMessage);
    }
    
    [TestMethod]
    public async Task GenerateAsync_WithCancellation_ReturnsCanceledResult()
    {
        // Arrange
        var settings = CreateValidSettings();
        var cancellationTokenSource = new CancellationTokenSource();
        _mockPluginManager.Setup(m => m.GenerationHookPlugins).Returns(new List<IGenerationHookPlugin>());
        
        // Act
        cancellationTokenSource.Cancel();
        var result = await _wfcService.GenerateAsync(settings, cancellationTokenSource.Token);
        
        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Grid);
        Assert.AreEqual("Operation canceled", result.ErrorMessage);
    }
    
    [TestMethod]
    public async Task GenerateAsync_WithPlugins_CallsPluginHooks()
    {
        // Arrange
        var settings = CreateValidSettings();
        var mockPlugin = new Mock<IGenerationHookPlugin>();
        mockPlugin.Setup(p => p.Enabled).Returns(true);
        
        _mockPluginManager.Setup(m => m.GenerationHookPlugins)
            .Returns(new List<IGenerationHookPlugin> { mockPlugin.Object });
        _mockPluginManager.Setup(m => m.PostProcessorPlugins)
            .Returns(new List<IPostProcessorPlugin>());
        
        // Act
        var result = await _wfcService.GenerateAsync(settings);
        
        // Assert
        mockPlugin.Verify(p => p.OnBeforeGeneration(It.IsAny<WFCSettings>()), Times.Once);
        mockPlugin.Verify(p => p.OnAfterGeneration(It.IsAny<Tile[,]>(), It.IsAny<GenerationContext>()), Times.Once);
    }
    
    [TestMethod]
    public void Reset_ClearsState()
    {
        // Arrange
        bool progressEventRaised = false;
        _wfcService.ProgressChanged += (sender, args) => progressEventRaised = true;
        
        // Act
        _wfcService.Reset();
        
        // Assert
        Assert.IsTrue(progressEventRaised);
        // Additional assertions would verify internal state is reset, but may require reflection
    }
    
    private WFCSettings CreateValidSettings()
    {
        return new WFCSettings
        {
            Width = 10,
            Height = 10,
            Tiles = CreateSampleTiles(),
            TileIndexMap = CreateSampleTileIndexMap(),
            Rules = CreateSampleRules(),
            EnableDebugRendering = false,
            PluginSettings = new Dictionary<string, object>()
        };
    }
    
    private List<Tile> CreateSampleTiles()
    {
        // Create sample tiles for testing
        return new List<Tile>
        {
            new Tile(0, "grass.basic", "Grass", "grass"),
            new Tile(1, "flowers.basic", "Flowers", "flowers"),
            new Tile(2, "pavement.basic", "Pavement", "pavement")
        };
    }
    
    private Dictionary<string, int> CreateSampleTileIndexMap()
    {
        return new Dictionary<string, int>
        {
            { "grass.basic", 0 },
            { "flowers.basic", 1 },
            { "pavement.basic", 2 }
        };
    }
    
    private Dictionary<(int fromState, string direction), List<(int toState, float weight)>> CreateSampleRules()
    {
        var rules = new Dictionary<(int fromState, string direction), List<(int toState, float weight)>>();
        
        // Grass can connect to grass in all directions
        rules.Add((0, "up"), new List<(int, float)> { (0, 1.0f), (1, 0.5f) });
        rules.Add((0, "down"), new List<(int, float)> { (0, 1.0f), (1, 0.5f) });
        rules.Add((0, "left"), new List<(int, float)> { (0, 1.0f), (1, 0.5f) });
        rules.Add((0, "right"), new List<(int, float)> { (0, 1.0f), (1, 0.5f) });
        
        // Similar rules for other tile types...
        
        return rules;
    }
}