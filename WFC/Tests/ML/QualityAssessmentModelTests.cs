using System.IO;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WFC.Models;
using WFC.Models.NeuralNetwork;
using WFC.Services.ML;

namespace WFC.Tests.ML;

[TestClass]
public class QualityAssessmentModelTests
{
    private string _tempModelPath;
    private string _tempTrainingPath;
    private IQualityAssessmentModel _model;
    private AccordNetModelTrainer _trainer;
    private TrainingDataCollector _collector;
    
    [TestInitialize]
    public void Setup()
    {
        // Create temporary directories for testing
        _tempModelPath = Path.Combine(Path.GetTempPath(), $"test_model_{Guid.NewGuid()}.bin");
        _tempTrainingPath = Path.Combine(Path.GetTempPath(), $"test_training_data_{Guid.NewGuid()}");
        
        // Clean up if files exist
        if (File.Exists(_tempModelPath))
            File.Delete(_tempModelPath);
        if (Directory.Exists(_tempTrainingPath))
            Directory.Delete(_tempTrainingPath, true);
            
        Directory.CreateDirectory(_tempTrainingPath);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        // Clean up temporary files
        if (File.Exists(_tempModelPath))
            File.Delete(_tempModelPath);
        if (Directory.Exists(_tempTrainingPath))
            Directory.Delete(_tempTrainingPath, true);
    }
    
    [TestMethod]
    public async Task BasicQualityModel_EvaluateAsync_ReturnsDefaultAssessment()
    {
        // Arrange
        _model = new BasicQualityModel();
        Tile[,] testMap = CreateTestMap();
        
        // Act
        var result = await _model.EvaluateAsync(testMap);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0.5f, result.OverallScore, 0.01f); // Float comparison with delta
        Assert.IsNotNull(result.DimensionalScores);
        Assert.AreEqual(3, result.DimensionalScores.Count);
        Assert.AreEqual(0.5f, result.DimensionalScores["Coherence"], 0.01f);
        Assert.AreEqual(0.5f, result.DimensionalScores["Aesthetics"], 0.01f);
        Assert.AreEqual(0.5f, result.DimensionalScores["Playability"], 0.01f);
        Assert.IsNotNull(result.Feedback);
        Assert.IsTrue(result.Feedback.Length > 0);
    }
    
    [TestMethod]
    public async Task AccordNetQualityModel_WithTrainedModel_EvaluatesProperly()
    {
        // Arrange
        await CreateTrainedModelForTest();
        _model = new AccordNetQualityModel(_tempModelPath);
        Tile[,] testMap = CreateTestMap();
        
        // Act
        var result = await _model.EvaluateAsync(testMap);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.OverallScore >= 0 && result.OverallScore <= 1);
        Assert.IsNotNull(result.DimensionalScores);
        Assert.IsTrue(result.DimensionalScores.All(score => score.Value >= 0 && score.Value <= 1));
        Assert.IsNotNull(result.Feedback);
    }
    
    [TestMethod]
    public void AccordNetQualityModel_WithoutModel_GetModelInfo_ReturnsUntrainedInfo()
    {
        // Arrange
        // Attempt to create model without file (will fall back to untrained state)
        _model = new AccordNetQualityModel(Path.GetTempFileName() + ".nonexistent");
        
        // Act
        var modelInfo = _model.GetModelInfo();
        
        // Assert
        Assert.IsNotNull(modelInfo);
        Assert.AreEqual("Accord.NET Neural Network Quality Model", modelInfo.Name);
        Assert.AreEqual("No", modelInfo.Parameters["Trained"]);
    }
    
    [TestMethod]
    public async Task AccordNetModelTrainer_TrainModelAsync_CreatesValidModel()
    {
        // Arrange
        await CreateTrainingDataFileForTest();
        string testDataFile = Path.Combine(_tempTrainingPath, "examples.json");
        _trainer = new AccordNetModelTrainer(testDataFile, _tempModelPath);
        
        // Act
        string modelPath = await _trainer.TrainModelAsync();
        
        // Assert
        Assert.IsTrue(File.Exists(modelPath));
        Assert.AreEqual(_tempModelPath, modelPath);
        
        // Test that model can be loaded and used
        var model = new AccordNetQualityModel(modelPath);
        var modelInfo = model.GetModelInfo();
        Assert.AreEqual("Accord.NET Neural Network Quality Model", modelInfo.Name);
        
        // Test evaluation
        var testMap = CreateTestMap();
        var result = await model.EvaluateAsync(testMap);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.OverallScore >= 0 && result.OverallScore <= 1);
    }
    
    [TestMethod]
    public async Task TrainingDataCollector_AddUserRating_SavesCorrectly()
    {
        // Arrange
        _collector = new TrainingDataCollector(_tempTrainingPath);
        
        // Act
        bool result = await _collector.AddUserRating("test_map_123", 0.8f);
        
        // Assert
        Assert.IsTrue(result);
        
        // Verify examples file was updated
        string examplesPath = Path.Combine(_tempTrainingPath, "examples.json");
        Assert.IsTrue(File.Exists(examplesPath));
        
        // Verify content
        string json = await File.ReadAllTextAsync(examplesPath);
        var examples = JsonSerializer.Deserialize<List<TrainingDataCollector.TrainingExample>>(json);
        
        Assert.IsNotNull(examples);
        Assert.AreEqual(1, examples.Count);
        Assert.AreEqual(0.8f, examples[0].UserRating, 0.01f);
    }
    
    [TestMethod]
    public async Task AccordNetQualityModel_HandlesNullGrid_Gracefully()
    {
        // Arrange
        _model = new BasicQualityModel();
        
        // Act
        var result = await _model.EvaluateAsync(null);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0.5f, result.OverallScore, 0.01f);
        Assert.IsNotNull(result.Feedback);
        Assert.IsTrue(result.Feedback[0].Contains("model has been deleted"));
    }
    
    // Helper methods
    private Tile[,] CreateTestMap()
    {
        var map = new Tile[5, 5];
        var grassTile = new Tile(0, "grass.basic", "Grass", "grass", "grass");
        var flowersTile = new Tile(1, "flowers.basic", "Flowers", "flowers", "flowers");
        var pavementTile = new Tile(2, "pavement.basic", "Pavement", "pavement", "pavement");
        
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                if (x == 2 || y == 2)
                    map[x, y] = pavementTile;
                else if ((x + y) % 3 == 0)
                    map[x, y] = flowersTile;
                else
                    map[x, y] = grassTile;
            }
        }
        
        return map;
    }
    
    private async Task CreateTrainingDataFileForTest()
    {
        var examples = new List<TrainingDataCollector.TrainingExample>
        {
            new TrainingDataCollector.TrainingExample
            {
                MapId = "test_map_1",
                UserRating = 0.9f,
                FeatureValues = new Dictionary<string, float>
                {
                    { "VarietyScore", 0.6f },
                    { "TransitionDensity", 0.4f },
                    { "RatioGrass", 0.5f },
                    { "RatioFlowers", 0.2f },
                    { "RatioPavement", 0.2f },
                    { "RatioBuilding", 0.1f },
                    { "RatioWater", 0.0f },
                    { "TransitionCount", 20f }
                }
            },
            new TrainingDataCollector.TrainingExample
            {
                MapId = "test_map_2",
                UserRating = 0.4f,
                FeatureValues = new Dictionary<string, float>
                {
                    { "VarietyScore", 0.2f },
                    { "TransitionDensity", 0.8f },
                    { "RatioGrass", 0.8f },
                    { "RatioFlowers", 0.1f },
                    { "RatioPavement", 0.1f },
                    { "RatioBuilding", 0.0f },
                    { "RatioWater", 0.0f },
                    { "TransitionCount", 50f }
                }
            }
        };
        
        string examplesPath = Path.Combine(_tempTrainingPath, "examples.json");
        string json = JsonSerializer.Serialize(examples, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(examplesPath, json);
    }
    
    private async Task CreateTrainedModelForTest()
    {
        await CreateTrainingDataFileForTest();
        string examplesPath = Path.Combine(_tempTrainingPath, "examples.json");
        _trainer = new AccordNetModelTrainer(examplesPath, _tempModelPath);
        await _trainer.TrainModelAsync();
    }
}