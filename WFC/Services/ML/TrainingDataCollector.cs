using System.IO;
using System.Text.Json;
using WFC.Models;

namespace WFC.Services.ML;

/// <summary>
/// Training data collector for neural network
/// </summary>
public class TrainingDataCollector
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
        LoadExistingExamples();
    }

    public async Task SaveGeneratedMapForTraining(Tile[,] grid, WFCSettings settings, string seedValue)
    {
        string mapId = $"map_{DateTime.Now:yyyyMMdd_HHmmss}_{seedValue}";
        string subfolder = Path.Combine(_dataFolder, mapId);
        Directory.CreateDirectory(subfolder);

        // Save metadata
        string metadataPath = Path.Combine(subfolder, "metadata.json");
        SaveMapMetadata(grid, settings, metadataPath);

        // Add to examples list for later labeling
        _examples.Add(new TrainingExample
        {
            MapId = mapId,
            ImagePath = "", // Image would be saved separately by export system
            MetadataPath = metadataPath,
            UserRating = 0, // Will be set later by user
            FeatureValues = ExtractFeatures(grid)
        });

        // Save examples list
        await SaveExamplesList();
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
            TimeGenerated = DateTime.Now.ToString("o")
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

        // Transition count
        int transitions = CountTransitions(grid);
        features["TransitionCount"] = transitions;
        features["TransitionDensity"] = (float)transitions / totalTiles;

        return features;
    }

    private int CountTransitions(Tile[,] grid)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        int transitions = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var current = grid[x, y]?.Category;

                // Check right
                if (x < width - 1)
                {
                    var right = grid[x + 1, y]?.Category;
                    if (current != right && current != null && right != null)
                        transitions++;
                }

                // Check down
                if (y < height - 1)
                {
                    var down = grid[x, y + 1]?.Category;
                    if (current != down && current != null && down != null)
                        transitions++;
                }
            }
        }

        return transitions;
    }

    public async Task<bool> AddUserRating(string mapId, float rating)
    {
        // Find matching example
        var example = _examples.FirstOrDefault(e => e.MapId.Contains(mapId));
        if (example == null)
        {
            // Create a new example if not found
            example = new TrainingExample
            {
                MapId = mapId,
                UserRating = rating,
                FeatureValues = new Dictionary<string, float>()
            };
            _examples.Add(example);
        }
        else
        {
            example.UserRating = rating;
        }

        // Save rating to file
        string ratingPath = Path.Combine(_dataFolder, example.MapId, "rating.json");
        if (!Directory.Exists(Path.GetDirectoryName(ratingPath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ratingPath));
        }

        await File.WriteAllTextAsync(ratingPath, JsonSerializer.Serialize(
            new { Rating = rating }, new JsonSerializerOptions { WriteIndented = true }));

        // Save examples list
        await SaveExamplesList();

        return true;
    }

    private async Task SaveExamplesList()
    {
        try
        {
            string examplesPath = Path.Combine(_dataFolder, "examples.json");
            string json = JsonSerializer.Serialize(
                _examples, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(examplesPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving examples list: {ex.Message}");
            // Продолжаем выполнение даже при ошибке
        }
    }

    private void LoadExistingExamples()
    {
        string examplesPath = Path.Combine(_dataFolder, "examples.json");
        if (File.Exists(examplesPath))
        {
            try
            {
                string json = File.ReadAllText(examplesPath);
                var examples = JsonSerializer.Deserialize<List<TrainingExample>>(json);
                if (examples != null)
                {
                    _examples = examples;
                    Console.WriteLine($"Loaded {_examples.Count} existing training examples");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading training examples: {ex.Message}");
                _examples = new List<TrainingExample>();
            }
        }
    }

    public async Task ExportTrainingData(string outputPath)
    {
        // Export all examples with ratings
        var trainingData = _examples.Where(e => e.UserRating > 0).ToList();

        if (trainingData.Count == 0)
        {
            throw new InvalidOperationException(
                "No rated examples available for training. Please rate some maps first.");
        }

        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(
            trainingData, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"Exported {trainingData.Count} training examples to {outputPath}");
    }
}