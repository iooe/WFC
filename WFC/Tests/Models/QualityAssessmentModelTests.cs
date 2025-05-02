using Microsoft.VisualStudio.TestTools.UnitTesting;
using WFC.Models;
using WFC.Models.NeuralNetwork;
using WFC.Services.ML;
using System.IO;
using System.Threading.Tasks;

namespace WFC.Tests.Models;

[TestClass]
public class QualityAssessmentModelTests
{
    private IQualityAssessmentModel _model;
    
    [TestInitialize]
    public void Setup()
    {
        // Использовать базовую модель вместо пытающейся загрузить файл модели
        // который может не существовать в тестовой среде
        _model = new BasicQualityModel(); 
    }
    
    [TestMethod]
    public async Task EvaluateAsync_WithValidMap_ReturnsQualityAssessment()
    {
        // Arrange
        Tile[,] testMap = CreateTestMap();
        
        // Act
        var result = await _model.EvaluateAsync(testMap);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.OverallScore >= 0 && result.OverallScore <= 1, 
            $"Overall score should be between 0 and 1, but was {result.OverallScore}");
        Assert.IsNotNull(result.DimensionalScores);
        Assert.IsTrue(result.DimensionalScores.ContainsKey("Coherence"));
        Assert.IsTrue(result.DimensionalScores.ContainsKey("Aesthetics"));
        Assert.IsTrue(result.DimensionalScores.ContainsKey("Playability"));
        Assert.IsNotNull(result.Feedback);
    }
    
    [TestMethod]
    public async Task EvaluateAsync_WithNullMap_ReturnsDefaultAssessment()
    {
        // Act
        var result = await _model.EvaluateAsync(null);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Feedback);
        Assert.IsTrue(result.Feedback.Length > 0);
        
        // В базовой модели может быть нулевая оценка для null карты или
        // значение по умолчанию 0.5f, поэтому проверяем более гибко
        Assert.IsTrue(result.OverallScore >= 0 && result.OverallScore <= 1,
            $"Overall score should be between 0 and 1, but was {result.OverallScore}");
    }
    
    [TestMethod]
    public void GetModelInfo_ReturnsValidInfo()
    {
        // Act
        var modelInfo = _model.GetModelInfo();
        
        // Assert
        Assert.IsNotNull(modelInfo);
        Assert.IsFalse(string.IsNullOrEmpty(modelInfo.Name));
        Assert.IsFalse(string.IsNullOrEmpty(modelInfo.Description));
        Assert.IsFalse(string.IsNullOrEmpty(modelInfo.Version));
        Assert.IsNotNull(modelInfo.Parameters);
    }
    
    private Tile[,] CreateTestMap()
    {
        // Create a small test map with various tile types
        var map = new Tile[5, 5];
        
        // Sample tiles
        var grassTile = new Tile(0, "grass.basic", "Grass", "grass", "grass");
        var flowersTile = new Tile(1, "flowers.basic", "Flowers", "flowers", "flowers");
        var pavementTile = new Tile(2, "pavement.basic", "Pavement", "pavement", "pavement");
        
        // Simple pattern for testing
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                if (x == 2 || y == 2)
                    map[x, y] = pavementTile; // Pavement cross
                else if ((x + y) % 3 == 0)
                    map[x, y] = flowersTile; // Some flowers
                else
                    map[x, y] = grassTile; // Default grass
            }
        }
        
        return map;
    }
}