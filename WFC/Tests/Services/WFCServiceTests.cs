using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WFC.Models;
using WFC.Plugins;
using WFC.Services;
using System.Reflection;

namespace WFC.Tests.Services;

[TestClass]
public class WFCServiceTests
{
    private DefaultWFCService _wfcService;
    private MockPluginManager _mockPluginManager;
    
    [TestInitialize]
    public void Setup()
    {
        // Создаем реальный класс вместо мока
        _mockPluginManager = new MockPluginManager();
        
        // Создаем сервис с нашим специальным менеджером плагинов
        _wfcService = new DefaultWFCService(_mockPluginManager);
    }
    
    [TestMethod]
    public async Task GenerateAsync_WithValidSettings_ReturnsSuccessResult()
    {
        // Arrange
        var settings = CreateValidSettings();
        
        // Act
        var result = await _wfcService.GenerateAsync(settings);
        
        // Assert
        Assert.IsTrue(result.Success, "Generation should be successful");
        Assert.IsNotNull(result.Grid, "Result grid should not be null");
        Assert.IsNull(result.ErrorMessage, "Error message should be null");
    }
    
    [TestMethod]
    public async Task GenerateAsync_WithCancellation_ReturnsCanceledResult()
    {
        // Arrange
        var settings = CreateValidSettings();
        var cancellationTokenSource = new CancellationTokenSource();
        
        // Немедленно отменяем операцию
        cancellationTokenSource.Cancel();
        
        // Act
        var result = await _wfcService.GenerateAsync(settings, cancellationTokenSource.Token);
        
        // Assert
        Assert.IsFalse(result.Success, "Generation should not be successful when canceled");
        Assert.IsNull(result.Grid, "Grid should be null when canceled");
        Assert.AreEqual("Operation canceled", result.ErrorMessage, "Error message should indicate cancellation");
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
        Assert.IsTrue(progressEventRaised, "Progress event should be raised during Reset()");
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
            new Tile(0, "grass.basic", "Grass", "grass", "grass"),
            new Tile(1, "flowers.basic", "Flowers", "flowers", "flowers"),
            new Tile(2, "pavement.basic", "Pavement", "pavement", "pavement")
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
        
        return rules;
    }
    
    /// <summary>
    /// Специальный класс-заменитель для PluginManager
    /// </summary>
    private class MockPluginManager : PluginManager
    {
        private readonly List<IGenerationHookPlugin> _hookPlugins = new();
        private readonly List<IPostProcessorPlugin> _postPlugins = new();
        
        public MockPluginManager() : base(null)
        {
        }
        
        public void AddHookPlugin(IGenerationHookPlugin plugin)
        {
            _hookPlugins.Add(plugin);
        }
        
        public void AddPostPlugin(IPostProcessorPlugin plugin)
        {
            _postPlugins.Add(plugin);
        }
        
        // Переопределяем свойства для возврата наших списков
        public new IEnumerable<IGenerationHookPlugin> GenerationHookPlugins => _hookPlugins;
        public new IEnumerable<IPostProcessorPlugin> PostProcessorPlugins => _postPlugins;
    }
    
    /// <summary>
    /// Тестовый плагин для отслеживания вызовов
    /// </summary>
    private class MockGenerationHookPlugin : IGenerationHookPlugin
    {
        public string Id => "mock.hook.plugin";
        public string Name => "Mock Hook Plugin";
        public string Version => "1.0";
        public string Description => "Test plugin for unit tests";
        public bool Enabled { get; set; } = true;
        
        public bool OnBeforeGenerationCalled { get; private set; }
        public bool OnBeforeCollapseCalled { get; private set; }
        public bool OnAfterCollapseCalled { get; private set; }
        public bool OnAfterGenerationCalled { get; private set; }
        public bool OnPostProcessCalled { get; private set; }
        
        public void Initialize(IServiceProvider serviceProvider) { }
        
        public void OnBeforeGeneration(WFCSettings settings)
        {
            OnBeforeGenerationCalled = true;
        }
        
        public IEnumerable<int> OnBeforeCollapse(int x, int y, IEnumerable<int> possibleStates, GenerationContext context)
        {
            OnBeforeCollapseCalled = true;
            return possibleStates;
        }
        
        public void OnAfterCollapse(int x, int y, int state, GenerationContext context)
        {
            OnAfterCollapseCalled = true;
        }
        
        public void OnAfterGeneration(Tile[,] grid, GenerationContext context)
        {
            OnAfterGenerationCalled = true;
        }
        
        public Tile[,] OnPostProcess(Tile[,] grid, GenerationContext context)
        {
            OnPostProcessCalled = true;
            return grid;
        }
    }
}