using System.IO;
using System.Text.Json;
using WFC.Models;

namespace WFC.Services.ML;

public abstract class TrainingDataCollector
{
    private readonly string _dataFolder;
    private List<TrainingExample> _examples = new List<TrainingExample>();
    
    public class TrainingExample
    {
        public string MapId { get; set; }
        public string ImagePath { get; set; }
        public string MetadataPath { get; set; }
        public float UserRating { get; set; }
        public Dictionary<string, float> FeatureValues { get; set; }
    }
    
    public TrainingDataCollector(string dataFolder = null)
    {
        _dataFolder = dataFolder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TrainingData");
        Directory.CreateDirectory(_dataFolder);
    }
    
    public async Task SaveGeneratedMapForTraining(Tile[,] grid, WFCSettings settings, string seedValue)
    {
        string mapId = $"map_{DateTime.Now:yyyyMMdd_HHmmss}_{seedValue}";
        string subfolder = Path.Combine(_dataFolder, mapId);
        Directory.CreateDirectory(subfolder);
        
        // Save image of the map
        string imagePath = Path.Combine(subfolder, "map.png");
        await SaveMapImage(grid, imagePath);
        
        // Save metadata
        string metadataPath = Path.Combine(subfolder, "metadata.json");
        SaveMapMetadata(grid, settings, metadataPath);
        
        // Add to examples list for later labeling
        _examples.Add(new TrainingExample
        {
            MapId = mapId,
            ImagePath = imagePath,
            MetadataPath = metadataPath,
            UserRating = 0, // Will be set later by user
            FeatureValues = ExtractFeatures(grid)
        });
    }
    
    private async Task SaveMapImage(Tile[,] grid, string imagePath)
    {
        // Implementation would use the existing export functionality
        // For simplicity, this is a placeholder
    }
    
    private void SaveMapMetadata(Tile[,] grid, WFCSettings settings, string metadataPath)
    {
        // Save relevant metadata
        var metadata = new
        {
            Width = grid.GetLength(0),
            Height = grid.GetLength(1),
            Seed = settings.Seed,
            TileCount = CountTiles(grid),
            PluginsUsed = settings.PluginSettings.Keys
        };
        
        string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        File.WriteAllText(metadataPath, json);
    }
    
    private Dictionary<string, int> CountTiles(Tile[,] grid)
    {
        var counts = new Dictionary<string, int>();
        
        foreach (var tile in grid)
        {
            if (tile != null)
            {
                string category = tile.Category ?? "unknown";
                if (!counts.ContainsKey(category))
                    counts[category] = 0;
                counts[category]++;
            }
        }
        
        return counts;
    }
    
    private Dictionary<string, float> ExtractFeatures(Tile[,] grid)
    {
        // Extract features for training
        // This would be the same features used in the quality model
        var features = new Dictionary<string, float>();
        
        // Calculate basic features
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        int totalTiles = width * height;
        
        // Count tile types
        var categories = new Dictionary<string, int>();
        foreach (var tile in grid)
        {
            if (tile != null)
            {
                string category = tile.Category ?? "unknown";
                if (!categories.ContainsKey(category))
                    categories[category] = 0;
                categories[category]++;
            }
        }
        
        // Convert counts to ratios
        foreach (var category in categories)
        {
            features[$"Ratio_{category.Key}"] = (float)category.Value / totalTiles;
        }
        
        // Variety score (unique tiles / total)
        var uniqueTileIds = new HashSet<string>();
        foreach (var tile in grid)
        {
            if (tile != null)
                uniqueTileIds.Add(tile.TileId);
        }
        features["VarietyScore"] = (float)uniqueTileIds.Count / totalTiles;
        
        // Add more sophisticated features here...
        
        return features;
    }
    
    public async Task<bool> AddUserRating(string mapId, float rating)
    {
        var example = _examples.FirstOrDefault(e => e.MapId == mapId);
        if (example == null)
            return false;
            
        example.UserRating = rating;
        
        // Save rating to metadata file
        string ratingPath = Path.Combine(_dataFolder, mapId, "rating.json");
        await File.WriteAllTextAsync(ratingPath, JsonSerializer.Serialize(
            new { Rating = rating }));
            
        return true;
    }
    
    public async Task ExportTrainingData(string outputPath)
    {
        // Export all examples with ratings
        var trainingData = _examples.Where(e => e.UserRating > 0).ToList();
        
        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(
            trainingData, new JsonSerializerOptions { WriteIndented = true }));
    }
}