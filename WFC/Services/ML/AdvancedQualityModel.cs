using System.IO;
using Microsoft.ML;
using Microsoft.ML.Data;
using WFC.Models;
using WFC.Models.NeuralNetwork;

namespace WFC.Services.ML
{
    public class AdvancedQualityModel : IQualityAssessmentModel
    {
        private readonly MLContext _mlContext;
        private PredictionEngine<MapFeatures, QualityPrediction> _predictionEngine;
        private readonly string _modelPath;
        private bool _modelLoaded = false;

        // Model input class
        public class MapFeatures
        {
            // Основные показатели разнообразия и структуры
            public float VarietyScore { get; set; }
            public float TransitionCount { get; set; }
            public float TransitionDensity { get; set; }

            // Распределение типов тайлов (соотношения)
            public float RatioGrass { get; set; }
            public float RatioFlowers { get; set; }
            public float RatioPavement { get; set; }
            public float RatioBuilding { get; set; }
            public float RatioWater { get; set; }

            // Дополнительное свойство для обучения
            [ColumnName("Rating")] public float UserRating { get; set; }
        }

        // Model output class
        public class QualityPrediction
        {
            [ColumnName("Score")] public float OverallQuality { get; set; }
        }

        public AdvancedQualityModel(string modelPath = null)
        {
            _mlContext = new MLContext(seed: 0);
            _modelPath = modelPath ??
                         Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "quality_model.zip");

            // Load pre-trained model if it exists
            if (File.Exists(_modelPath))
            {
                try
                {
                    LoadModel();
                    Console.WriteLine($"Loaded quality model from {_modelPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading model: {ex.Message}");
                    _modelLoaded = false;

                    // Backup and remove corrupted model
                    try
                    {
                        string backupPath = _modelPath + ".backup";
                        File.Copy(_modelPath, backupPath, true);
                        File.Delete(_modelPath);
                        Console.WriteLine($"Corrupted model backed up to {backupPath} and removed");
                    }
                    catch (Exception fileEx)
                    {
                        Console.WriteLine($"Could not remove corrupted model: {fileEx.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("No trained model found, using default quality assessment");
                _modelLoaded = false;
            }
        }

        private void LoadModel()
        {
            try
            {
                var model = _mlContext.Model.Load(_modelPath, out var _);
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<MapFeatures, QualityPrediction>(model);
                _modelLoaded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading model: {ex.Message}");
                _modelLoaded = false;
                throw; // Rethrow to handle in constructor
            }
        }

        public async Task<QualityAssessment> EvaluateAsync(Tile[,] tileMap)
        {
            try
            {
                // Handle null tileMap
                if (tileMap == null)
                {
                    Console.WriteLine("Error: tileMap is null in EvaluateAsync");
                    return CreateDefaultAssessment("Unable to assess quality: grid data unavailable");
                }

                Console.WriteLine($"Assessing quality for grid: {tileMap.GetLength(0)}x{tileMap.GetLength(1)}");

                // Extract features from tilemap
                var features = ExtractFeatures(tileMap);
                Console.WriteLine($"Features extracted successfully:");
                Console.WriteLine($"  VarietyScore: {features.VarietyScore}");
                Console.WriteLine($"  TransitionCount: {features.TransitionCount}");
                Console.WriteLine($"  TransitionDensity: {features.TransitionDensity}");
                Console.WriteLine($"  Ratios: Grass={features.RatioGrass}, Flowers={features.RatioFlowers}, " +
                                  $"Pavement={features.RatioPavement}, Building={features.RatioBuilding}, Water={features.RatioWater}");

                QualityAssessment result;

                if (_modelLoaded)
                {
                    Console.WriteLine("Using trained quality model for assessment");
                    try
                    {
                        // Make prediction with trained model
                        var prediction = _predictionEngine.Predict(features);

                        // Log prediction result
                        Console.WriteLine($"MODEL PREDICTION: OverallQuality = {prediction.OverallQuality}");

                        // Protect against zero values
                        if (Math.Abs(prediction.OverallQuality) < 0.001f)
                        {
                            Console.WriteLine(
                                "WARNING: Model predicted zero score, using heuristic assessment instead");
                            result = CreateHeuristicAssessment(features);
                        }
                        else
                        {
                            // Create assessment result using model prediction
                            result = CreateAssessment(prediction.OverallQuality, features);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during prediction: {ex.Message}");
                        Console.WriteLine("Falling back to heuristic method");
                        // Fall back to heuristic method
                        result = CreateHeuristicAssessment(features);
                    }
                }
                else
                {
                    Console.WriteLine("No trained model available, using heuristic assessment");
                    // Use heuristic-based assessment if no model is available
                    result = CreateHeuristicAssessment(features);
                }

                // Final quality check - protect against zero values
                if (Math.Abs(result.OverallScore) < 0.001f)
                {
                    Console.WriteLine("WARNING: Final quality score is zero! Applying safety minimum.");
                    result.OverallScore = 0.3f; // Safety minimum value

                    // Also check dimensional scores
                    foreach (var key in result.DimensionalScores.Keys.ToList())
                    {
                        if (Math.Abs(result.DimensionalScores[key]) < 0.001f)
                        {
                            result.DimensionalScores[key] = 0.2f; // Minimum per dimension
                        }
                    }
                }

                // FINAL RESULTS DIAGNOSTICS
                Console.WriteLine($"FINAL QUALITY SCORES:");
                Console.WriteLine($"  Overall: {result.OverallScore}");
                foreach (var score in result.DimensionalScores)
                {
                    Console.WriteLine($"  {score.Key}: {score.Value}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error evaluating quality: {ex}");
                return CreateDefaultAssessment($"Error evaluating quality: {ex.Message}");
            }
        }

        private MapFeatures ExtractFeatures(Tile[,] tileMap)
        {
            // Handle null tileMap
            if (tileMap == null)
            {
                Console.WriteLine("Error: tileMap is null in ExtractFeatures");
                return new MapFeatures
                {
                    VarietyScore = 0.5f,
                    TransitionCount = 0,
                    TransitionDensity = 0.5f,
                    RatioGrass = 0.2f,
                    RatioFlowers = 0.1f,
                    RatioPavement = 0.2f,
                    RatioBuilding = 0.2f,
                    RatioWater = 0.1f
                };
            }

            // Extract relevant features from the tile map
            int width = tileMap.GetLength(0);
            int height = tileMap.GetLength(1);
            int totalTiles = width * height;

            // Count tile categories
            var categories = new Dictionary<string, int>
            {
                { "grass", 0 },
                { "flowers", 0 },
                { "pavement", 0 },
                { "building", 0 },
                { "water", 0 },
                { "transition", 0 },
                { "unknown", 0 }
            };

            // Count tile types
            int nonNullTiles = 0;
            foreach (var tile in tileMap)
            {
                if (tile != null)
                {
                    nonNullTiles++;
                    string category = tile.Category?.ToLowerInvariant() ?? "unknown";
                    if (categories.ContainsKey(category))
                        categories[category]++;
                    else
                        categories["unknown"]++;
                }
            }

            Console.WriteLine($"Grid contains {nonNullTiles} non-null tiles");

            // Calculate variety score (unique tiles / total)
            var uniqueTileIds = new HashSet<string>();
            foreach (var tile in tileMap)
            {
                if (tile != null)
                    uniqueTileIds.Add(tile.TileId);
            }

            float varietyScore = totalTiles > 0 ? (float)uniqueTileIds.Count / totalTiles : 0.5f;

            // Count transitions between different tile types
            int transitions = CountTransitions(tileMap);
            float transitionDensity = totalTiles > 0 ? (float)transitions / totalTiles : 0.5f;

            // Create feature vector
            return new MapFeatures
            {
                VarietyScore = varietyScore,
                TransitionCount = transitions,
                TransitionDensity = transitionDensity,
                RatioGrass = totalTiles > 0 ? (float)categories["grass"] / totalTiles : 0,
                RatioFlowers = totalTiles > 0 ? (float)categories["flowers"] / totalTiles : 0,
                RatioPavement = totalTiles > 0 ? (float)categories["pavement"] / totalTiles : 0,
                RatioBuilding = totalTiles > 0 ? (float)categories["building"] / totalTiles : 0,
                RatioWater = totalTiles > 0 ? (float)categories["water"] / totalTiles : 0
            };
        }

        private int CountTransitions(Tile[,] tileMap)
        {
            if (tileMap == null) return 0;

            int width = tileMap.GetLength(0);
            int height = tileMap.GetLength(1);
            int transitions = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var current = tileMap[x, y]?.Category?.ToLowerInvariant();

                    // Check right
                    if (x < width - 1)
                    {
                        var right = tileMap[x + 1, y]?.Category?.ToLowerInvariant();
                        if (current != right && current != null && right != null)
                            transitions++;
                    }

                    // Check down
                    if (y < height - 1)
                    {
                        var down = tileMap[x, y + 1]?.Category?.ToLowerInvariant();
                        if (current != down && current != null && down != null)
                            transitions++;
                    }
                }
            }

            return transitions;
        }

        private QualityAssessment CreateDefaultAssessment(string message)
        {
            return new QualityAssessment
            {
                OverallScore = 0.5f,
                DimensionalScores = new Dictionary<string, float>
                {
                    { "Coherence", 0.5f },
                    { "Aesthetics", 0.5f },
                    { "Playability", 0.5f }
                },
                Feedback = new[] { message }
            };
        }

        private QualityAssessment CreateAssessment(float overallScore, MapFeatures features)
        {
            // Calculate dimensional scores based on features
            var dimensionalScores = new Dictionary<string, float>
            {
                { "Coherence", CalculateCoherenceScore(features) },
                { "Aesthetics", CalculateAestheticsScore(features) },
                { "Playability", CalculatePlayabilityScore(features) }
            };

            return new QualityAssessment
            {
                OverallScore = overallScore,
                DimensionalScores = dimensionalScores,
                Feedback = GenerateFeedback(overallScore, dimensionalScores, features)
            };
        }

        private QualityAssessment CreateHeuristicAssessment(MapFeatures features)
        {
            // Calculate scores based on heuristics
            float coherenceScore = CalculateCoherenceScore(features);
            float aestheticsScore = CalculateAestheticsScore(features);
            float playabilityScore = CalculatePlayabilityScore(features);

            // Overall score is weighted average of dimensional scores
            float overallScore = (coherenceScore * 0.3f) + (aestheticsScore * 0.4f) + (playabilityScore * 0.3f);

            var dimensionalScores = new Dictionary<string, float>
            {
                { "Coherence", coherenceScore },
                { "Aesthetics", aestheticsScore },
                { "Playability", playabilityScore }
            };

            return new QualityAssessment
            {
                OverallScore = overallScore,
                DimensionalScores = dimensionalScores,
                Feedback = GenerateFeedback(overallScore, dimensionalScores, features)
            };
        }

        private float CalculateCoherenceScore(MapFeatures features)
        {
            // Вывод значений для диагностики
            Console.WriteLine(
                $"TransitionDensity: {features.TransitionDensity}, VarietyScore: {features.VarietyScore}");

            // Менее строгий расчет с меньшим штрафом
            float idealTransitionDensity = 0.4f;
            float transitionScore = Math.Max(0, 1.0f - Math.Abs(features.TransitionDensity - idealTransitionDensity));

            // Смягчить штраф за разнообразие
            float varietyScore = Math.Max(0, 1.0f - Math.Abs(features.VarietyScore - 0.5f));

            // Вывод промежуточных результатов
            Console.WriteLine($"TransitionScore: {transitionScore}, VarietyScore: {varietyScore}");

            // Рассчитать окончательное значение
            float coherenceScore = (transitionScore * 0.7f) + (varietyScore * 0.3f);
            Console.WriteLine($"Final CoherenceScore: {coherenceScore}");

            return Math.Clamp(coherenceScore, 0, 1);
        }

        private float CalculateAestheticsScore(MapFeatures features)
        {
            // Balance between natural (grass, flowers) and constructed (pavement, building) elements
            float naturalRatio = features.RatioGrass + features.RatioFlowers + features.RatioWater;
            float constructedRatio = features.RatioPavement + features.RatioBuilding;

            float balanceScore = 1.0f - Math.Abs(naturalRatio - constructedRatio);

            // Variety is important for aesthetics
            float varietyMultiplier = 0.5f + (features.VarietyScore * 0.5f);

            return Math.Clamp(balanceScore * varietyMultiplier, 0, 1);
        }

        private float CalculatePlayabilityScore(MapFeatures features)
        {
            // Playability is based on having good balance of open areas and obstacles
            float openAreaRatio = features.RatioGrass + features.RatioPavement + features.RatioFlowers;
            float obstacleRatio = features.RatioBuilding + features.RatioWater;

            // Ideal ratio around 70% open, 30% obstacles
            float idealOpen = 0.7f;
            float distanceFromIdeal = Math.Abs(openAreaRatio - idealOpen);

            // Convert to score (1.0 = perfect, 0.0 = worst)
            float ratioScore = 1.0f - (distanceFromIdeal / idealOpen);

            // Also factor in transition density (for navigability)
            float transitionScore = features.TransitionDensity * 0.5f;

            return Math.Clamp((ratioScore * 0.8f) + (transitionScore * 0.2f), 0, 1);
        }

        private string[] GenerateFeedback(float overallScore, Dictionary<string, float> dimensionalScores,
            MapFeatures features)
        {
            var feedback = new List<string>();

            // Overall quality feedback
            if (overallScore > 0.8f)
                feedback.Add("Excellent map design with great balance and structure.");
            else if (overallScore > 0.6f)
                feedback.Add("Good map design with minor room for improvement.");
            else if (overallScore > 0.4f)
                feedback.Add("Average map quality with several areas for improvement.");
            else
                feedback.Add("This map needs significant improvements to enhance quality.");

            // Coherence feedback
            if (dimensionalScores["Coherence"] < 0.5f)
            {
                if (features.TransitionDensity > 0.6f)
                    feedback.Add("Too many transitions between tile types creates a chaotic appearance.");
                else if (features.TransitionDensity < 0.2f)
                    feedback.Add("More variety in tile transitions would improve the map's coherence.");
            }

            // Aesthetics feedback
            if (dimensionalScores["Aesthetics"] < 0.5f)
            {
                float naturalRatio = features.RatioGrass + features.RatioFlowers + features.RatioWater;
                float constructedRatio = features.RatioPavement + features.RatioBuilding;

                if (naturalRatio > 0.8f)
                    feedback.Add(
                        "The map is too dominated by natural elements. Adding more constructed features would improve balance.");
                else if (constructedRatio > 0.8f)
                    feedback.Add(
                        "The map is too dominated by constructed elements. Adding more natural features would improve balance.");

                if (features.VarietyScore < 0.2f)
                    feedback.Add("Low variety of tiles makes the map look repetitive.");
            }

            // Playability feedback
            if (dimensionalScores["Playability"] < 0.5f)
            {
                float openAreaRatio = features.RatioGrass + features.RatioPavement + features.RatioFlowers;

                if (openAreaRatio < 0.4f)
                    feedback.Add("The map has too many obstacles, making navigation difficult.");
                else if (openAreaRatio > 0.9f)
                    feedback.Add("The map has too few obstacles, making it less interesting to navigate.");
            }

            return feedback.ToArray();
        }

        public ModelInfo GetModelInfo()
        {
            return new ModelInfo
            {
                Name = "Advanced Quality Assessment Model",
                Description = "Evaluates procedurally generated maps on multiple quality dimensions",
                Version = "1.0",
                Parameters = new Dictionary<string, string>
                {
                    { "Architecture", "FastTree Regression" },
                    { "Features", "Variety, Transitions, Tile Distribution" },
                    { "Trained", _modelLoaded ? "Yes" : "No" }
                }
            };
        }
    }
}