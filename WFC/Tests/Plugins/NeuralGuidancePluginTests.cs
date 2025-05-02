using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WFC.Factories.Model;
using WFC.Models;
using WFC.Models.NeuralNetwork;
using WFC.Plugins.ML;

namespace WFC.Tests.Plugins;

[TestClass]
public class NeuralGuidancePluginTests
{
    private NeuralGuidancePlugin _plugin;
    private Mock<IQualityAssessmentModel> _mockModel;
    private Mock<IServiceProvider> _mockServiceProvider;
    private GenerationContext _context;
    
    [TestInitialize]
    public void Setup()
    {
        _mockModel = new Mock<IQualityAssessmentModel>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        
        // Setup mock model factory
        var mockModelFactory = new Mock<IModelFactory>();
        mockModelFactory.Setup(f => f.CreateModel(It.IsAny<ModelType>(), It.IsAny<string>()))
            .Returns(_mockModel.Object);
        
        _mockServiceProvider.Setup(s => s.GetService(typeof(IModelFactory)))
            .Returns(mockModelFactory.Object);
        
        // Create plugin
        _plugin = new NeuralGuidancePlugin();
        _plugin.Initialize(_mockServiceProvider.Object);
        
        // Create context for testing
        var settings = new WFCSettings
        {
            Width = 5,
            Height = 5,
            Tiles = CreateSampleTiles(),
            TileIndexMap = CreateSampleTileIndexMap(),
            Rules = CreateSampleRules(),
            PluginSettings = new Dictionary<string, object>()
        };
        
        var random = new Random(42); // Fixed seed for testing
        var grid = new Cell[5, 5];
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                grid[x, y] = new Cell(3);
            }
        }
        
        _context = new GenerationContext(settings, random, grid);
    }
    
    [TestMethod]
    public void OnBeforeGeneration_InitializesSharedData()
    {
        // Arrange
        var settings = new WFCSettings
        {
            PluginSettings = new Dictionary<string, object> { { "context", _context } }
        };
        
        // Act
        _plugin.OnBeforeGeneration(settings);
        
        // Assert
        Assert.IsTrue(_context.SharedData.ContainsKey("neural.guidance"));
        Assert.IsNull(_context.SharedData["neural.lastEvaluation"]);
    }
    
    [TestMethod]
    public void OnBeforeCollapse_WithPeriodicEvaluation_ModifiesPossibleStates()
    {
        // Arrange
        var originalStates = new[] { 0, 1, 2 };
        
        // Setup neural model response
        var assessment = new QualityAssessment
        {
            OverallScore = 0.5f,
            DimensionalScores = new Dictionary<string, float>
            {
                { "Coherence", 0.3f }, // Low coherence score
                { "Aesthetics", 0.6f },
                { "Playability", 0.7f }
            }
        };
        
        _mockModel.Setup(m => m.EvaluateAsync(It.IsAny<Tile[,]>()))
            .ReturnsAsync(assessment);
        
        // Set the field that tracks generation counter
        typeof(NeuralGuidancePlugin)
            .GetField("_generationCounter", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(_plugin, 9); // Will be 10 on next call
        
        // Act
        var result = _plugin.OnBeforeCollapse(2, 2, originalStates, _context).ToList();
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count); // Should still have all states
        
        // Should have same states but potentially different order due to prioritization
        CollectionAssert.AreEquivalent(originalStates, result);
        
        // Verify model was called
        _mockModel.Verify(m => m.EvaluateAsync(It.IsAny<Tile[,]>()), Times.Once);
        
        // Verify data was stored in context
        Assert.IsNotNull(_context.SharedData["neural.lastEvaluation"]);
    }
    
    [TestMethod]
    public void OnAfterGeneration_PerformsQualityAssessment()
    {
        // Arrange
        var grid = new Tile[5, 5];
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                grid[x, y] = CreateSampleTiles()[0]; // Use first tile type
            }
        }
        
        var assessment = new QualityAssessment
        {
            OverallScore = 0.8f,
            DimensionalScores = new Dictionary<string, float>
            {
                { "Coherence", 0.7f },
                { "Aesthetics", 0.9f },
                { "Playability", 0.8f }
            }
        };
        
        _mockModel.Setup(m => m.EvaluateAsync(It.IsAny<Tile[,]>()))
            .ReturnsAsync(assessment);
        
        // Act
        _plugin.OnAfterGeneration(grid, _context);
        
        // Assert
        _mockModel.Verify(m => m.EvaluateAsync(It.IsAny<Tile[,]>()), Times.Once);
        Assert.IsNotNull(_context.SharedData["neural.finalAssessment"]);
        Assert.AreEqual(assessment, _context.SharedData["neural.finalAssessment"]);
    }
    
    [TestMethod]
    public void ProcessGrid_WithLowQualityAssessment_AppliesImprovements()
    {
        // Arrange
        var grid = new Tile[5, 5];
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                grid[x, y] = CreateSampleTiles()[0]; // Use first tile type
            }
        }
        
        // Set a low quality assessment
        _context.SharedData["neural.finalAssessment"] = new QualityAssessment
        {
            OverallScore = 0.4f, // Below threshold for improvement
            DimensionalScores = new Dictionary<string, float>
            {
                { "Coherence", 0.3f }, // Lowest dimension
                { "Aesthetics", 0.5f },
                { "Playability", 0.6f }
            }
        };
        
        // Act
        var result = _plugin.ProcessGrid(grid, _context);
        
        // Assert
        Assert.IsNotNull(result);
        
        // Should be a new grid reference (not the same object)
        Assert.AreNotSame(grid, result);
        
        // But still 5x5
        Assert.AreEqual(5, result.GetLength(0));
        Assert.AreEqual(5, result.GetLength(1));
    }
    
    // Helper methods
    private List<Tile> CreateSampleTiles()
    {
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
        
        // Minimal set of rules for testing
        rules.Add((0, "up"), new List<(int, float)> { (0, 1.0f), (1, 0.5f) });
        rules.Add((0, "down"), new List<(int, float)> { (0, 1.0f), (1, 0.5f) });
        
        return rules;
    }
}