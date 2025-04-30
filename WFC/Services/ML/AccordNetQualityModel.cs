using System.IO;
using Accord.Neuro;
using WFC.Models;
using WFC.Models.NeuralNetwork;

namespace WFC.Services.ML
{
    /// <summary>
    /// Model for quality assessment using real neural networks with Accord.NET
    /// </summary>
    public class AccordNetQualityModel : IQualityAssessmentModel
    {
        private readonly string _modelPath;
        private ActivationNetwork _network;
        private bool _modelLoaded = false;

        /// <summary>
        /// Initialization of quality assessment model based on neural network
        /// </summary>
        /// <param name="modelPath">Path to saved model (optional)</param>
        public AccordNetQualityModel(string modelPath = null)
        {
            _modelPath = modelPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "accord_quality_model.bin");
            
            try
            {
                LoadModel();
                Console.WriteLine($"Neural network loaded from {_modelPath}");
                _modelLoaded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading neural network: {ex.Message}");
                
                // Creating a new network with architecture:
                // Inputs: 8 (map features)
                // Hidden layer 1: 16 neurons
                // Hidden layer 2: 8 neurons
                // Output: 1 (quality assessment)
                _network = new ActivationNetwork(
                    new BipolarSigmoidFunction(2.0), // Bipolar sigmoid activation function
                    8,   // Inputs - number of features
                    16,  // Hidden layer 1 
                    8,   // Hidden layer 2
                    1);  // Output - quality assessment
                
                _modelLoaded = false;
            }
        }

        /// <summary>
        /// Perform quality assessment of the map
        /// </summary>
        public async Task<QualityAssessment> EvaluateAsync(Tile[,] tileMap)
        {
            try
            {
                // Null check
                if (tileMap == null)
                {
                    Console.WriteLine("Error: tileMap is null in EvaluateAsync");
                    return CreateDefaultAssessment("Cannot assess quality: grid data unavailable");
                }

                Console.WriteLine($"Quality assessment for grid: {tileMap.GetLength(0)}x{tileMap.GetLength(1)}");

                // Extract features from tile map
                var features = ExtractFeatures(tileMap);
                LogFeatures(features);

                QualityAssessment result;

                if (_modelLoaded && _network != null)
                {
                    Console.WriteLine("Using trained neural network for assessment");
                    try
                    {
                        // Perform prediction with neural network
                        float prediction = RunInference(features);

                        // Output prediction result
                        Console.WriteLine($"NEURAL NETWORK PREDICTION: OverallQuality = {prediction}");

                        // Protection against zero values
                        if (Math.Abs(prediction) < 0.001f)
                        {
                            Console.WriteLine("WARNING: Model predicted zero assessment, using heuristic assessment");
                            result = CreateHeuristicAssessment(features);
                        }
                        else
                        {
                            // Create assessment result based on model prediction
                            result = CreateAssessment(prediction, features);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during neural network prediction: {ex.Message}");
                        Console.WriteLine("Switching to heuristic method");
                        // Use heuristic method when error occurs
                        result = CreateHeuristicAssessment(features);
                    }
                }
                else
                {
                    Console.WriteLine("No trained model available, using heuristic assessment");
                    // Use heuristic method if no model is available
                    result = CreateHeuristicAssessment(features);
                }

                // Value check - protection against zero assessments
                if (Math.Abs(result.OverallScore) < 0.001f)
                {
                    Console.WriteLine("WARNING: Final quality assessment is zero! Applying minimum value.");
                    result.OverallScore = 0.3f; // Minimum value

                    // Also check dimensional scores
                    foreach (var key in result.DimensionalScores.Keys.ToList())
                    {
                        if (Math.Abs(result.DimensionalScores[key]) < 0.001f)
                        {
                            result.DimensionalScores[key] = 0.2f; // Minimum for each dimension
                        }
                    }
                }

                // Output final results
                LogFinalResults(result);

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Quality assessment error: {ex}");
                return CreateDefaultAssessment($"Quality assessment error: {ex.Message}");
            }
        }

        /// <summary>
        /// Load model from file
        /// </summary>
        private void LoadModel()
        {
            if (File.Exists(_modelPath))
            {
                // Load saved neural network
                _network = (ActivationNetwork)Network.Load(_modelPath);
            }
            else
            {
                throw new FileNotFoundException($"Model file not found: {_modelPath}");
            }
        }

        /// <summary>
        /// Perform prediction using neural network
        /// </summary>
        private float RunInference(Dictionary<string, float> features)
        {
            try
            {
                // Convert features to network input data
                double[] input = new double[]
                {
                    features["VarietyScore"],
                    features["TransitionDensity"],
                    features["RatioGrass"],
                    features["RatioFlowers"],
                    features["RatioPavement"],
                    features["RatioBuilding"],
                    features["RatioWater"],
                    NormalizeTransitionCount(features["TransitionCount"])
                };

                // Perform prediction using neural network
                double[] output = _network.Compute(input);
                
                // Get prediction (first and only output)
                float prediction = (float)output[0];
                
                // Normalize value to 0-1 range
                // (Accord may output values outside 0-1 range)
                return Math.Max(0, Math.Min(1, prediction));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in inference: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Normalize transition count to 0-1 range
        /// </summary>
        private float NormalizeTransitionCount(float transitionCount)
        {
            // Assume maximum transition count is 100
            float maxTransitions = 100f;
            return Math.Min(transitionCount / maxTransitions, 1.0f);
        }

        private void LogFeatures(Dictionary<string, float> features)
        {
            Console.WriteLine($"Extracted features:");
            Console.WriteLine($"  VarietyScore: {features["VarietyScore"]}");
            Console.WriteLine($"  TransitionCount: {features["TransitionCount"]}");
            Console.WriteLine($"  TransitionDensity: {features["TransitionDensity"]}");
            Console.WriteLine($"  Ratios: Grass={features["RatioGrass"]}, Flowers={features["RatioFlowers"]}, " +
                              $"Pavement={features["RatioPavement"]}, Building={features["RatioBuilding"]}, Water={features["RatioWater"]}");
        }

        private void LogFinalResults(QualityAssessment result)
        {
            Console.WriteLine($"FINAL QUALITY SCORES:");
            Console.WriteLine($"  Overall: {result.OverallScore}");
            foreach (var score in result.DimensionalScores)
            {
                Console.WriteLine($"  {score.Key}: {score.Value}");
            }
        }

        private Dictionary<string, float> ExtractFeatures(Tile[,] tileMap)
        {
            // Handle empty map
            if (tileMap == null)
            {
                Console.WriteLine("Error: tileMap is null in ExtractFeatures");
                return new Dictionary<string, float>
                {
                    { "VarietyScore", 0.5f },
                    { "TransitionCount", 0f },
                    { "TransitionDensity", 0.5f },
                    { "RatioGrass", 0.2f },
                    { "RatioFlowers", 0.1f },
                    { "RatioPavement", 0.2f },
                    { "RatioBuilding", 0.2f },
                    { "RatioWater", 0.1f }
                };
            }

            // Extract features from tile map
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

            // Calculate diversity (unique tiles / total count)
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
            return new Dictionary<string, float>
            {
                { "VarietyScore", varietyScore },
                { "TransitionCount", transitions },
                { "TransitionDensity", transitionDensity },
                { "RatioGrass", totalTiles > 0 ? (float)categories["grass"] / totalTiles : 0f },
                { "RatioFlowers", totalTiles > 0 ? (float)categories["flowers"] / totalTiles : 0f },
                { "RatioPavement", totalTiles > 0 ? (float)categories["pavement"] / totalTiles : 0f },
                { "RatioBuilding", totalTiles > 0 ? (float)categories["building"] / totalTiles : 0f },
                { "RatioWater", totalTiles > 0 ? (float)categories["water"] / totalTiles : 0f }
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

                    // Check below
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

        private QualityAssessment CreateAssessment(float overallScore, Dictionary<string, float> features)
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

        private QualityAssessment CreateHeuristicAssessment(Dictionary<string, float> features)
        {


            var dimensionalScores = new Dictionary<string, float>
            {
                { "Coherence", -1 },
                { "Aesthetics", -1 },
                { "Playability", -1 }
            };

            return new QualityAssessment
            {
                OverallScore = -1,
                DimensionalScores = dimensionalScores,
                Feedback = GenerateFeedback(-1, dimensionalScores, features)
            };
        }

        private float CalculateCoherenceScore(Dictionary<string, float> features)
        {
            // Softer calculation with less penalty
            float idealTransitionDensity = 0.4f;
            float transitionScore = Math.Max(0, 1.0f - Math.Abs(features["TransitionDensity"] - idealTransitionDensity));

            // Soften penalty for diversity
            float varietyScore = Math.Max(0, 1.0f - Math.Abs(features["VarietyScore"] - 0.5f));

            // Calculate final score
            float coherenceScore = (transitionScore * 0.7f) + (varietyScore * 0.3f);

            return Math.Clamp(coherenceScore, 0, 1);
        }

        private float CalculateAestheticsScore(Dictionary<string, float> features)
        {
            // Balance between natural (grass, flowers) and artificial (pavement, buildings) elements
            float naturalRatio = features["RatioGrass"] + features["RatioFlowers"] + features["RatioWater"];
            float constructedRatio = features["RatioPavement"] + features["RatioBuilding"];

            float balanceScore = 1.0f - Math.Abs(naturalRatio - constructedRatio);

            // Variety is important for aesthetics
            float varietyMultiplier = 0.5f + (features["VarietyScore"] * 0.5f);

            return Math.Clamp(balanceScore * varietyMultiplier, 0, 1);
        }

        private float CalculatePlayabilityScore(Dictionary<string, float> features)
        {
            // Gameplay qualities based on good balance of open spaces and obstacles
            float openAreaRatio = features["RatioGrass"] + features["RatioPavement"] + features["RatioFlowers"];
            float obstacleRatio = features["RatioBuilding"] + features["RatioWater"];

            // Ideal ratio about 70% open space, 30% obstacles
            float idealOpen = 0.7f;
            float distanceFromIdeal = Math.Abs(openAreaRatio - idealOpen);

            // Convert to score (1.0 = ideal, 0.0 = worst)
            float ratioScore = 1.0f - (distanceFromIdeal / idealOpen);

            // Also consider transition density (for navigation ease)
            float transitionScore = features["TransitionDensity"] * 0.5f;

            return Math.Clamp((ratioScore * 0.8f) + (transitionScore * 0.2f), 0, 1);
        }

        private string[] GenerateFeedback(float overallScore, Dictionary<string, float> dimensionalScores,
            Dictionary<string, float> features)
        {
            var feedback = new List<string>();

            // Overall quality assessment
            if (overallScore > 0.8f)
                feedback.Add("Excellent map design with good balance and structure.");
            else if (overallScore > 0.6f)
                feedback.Add("Good map design with minor opportunities for improvement.");
            else if (overallScore > 0.4f)
                feedback.Add("Average quality level with several areas for improvement.");
            else
                feedback.Add("This map requires significant improvements to increase quality.");

            // Coherence feedback
            if (dimensionalScores["Coherence"] < 0.5f)
            {
                if (features["TransitionDensity"] > 0.6f)
                    feedback.Add("Too many transitions between tile types creates a chaotic impression.");
                else if (features["TransitionDensity"] < 0.2f)
                    feedback.Add("More variety in tile transitions would improve map coherence.");
            }

            // Aesthetics feedback
            if (dimensionalScores["Aesthetics"] < 0.5f)
            {
                float naturalRatio = features["RatioGrass"] + features["RatioFlowers"] + features["RatioWater"];
                float constructedRatio = features["RatioPavement"] + features["RatioBuilding"];

                if (naturalRatio > 0.8f)
                    feedback.Add(
                        "Map is too saturated with natural elements. Adding constructed objects would improve balance.");
                else if (constructedRatio > 0.8f)
                    feedback.Add(
                        "Map is too saturated with artificial elements. Adding natural objects would improve balance.");

                if (features["VarietyScore"] < 0.2f)
                    feedback.Add("Low tile variety makes the map repetitive.");
            }

            // Gameplay feedback
            if (dimensionalScores["Playability"] < 0.5f)
            {
                float openAreaRatio = features["RatioGrass"] + features["RatioPavement"] + features["RatioFlowers"];

                if (openAreaRatio < 0.4f)
                    feedback.Add("Map has too many obstacles, making navigation difficult.");
                else if (openAreaRatio > 0.9f)
                    feedback.Add("Map has too few obstacles, making movement less interesting.");
            }

            return feedback.ToArray();
        }

        public ModelInfo GetModelInfo()
        {
            return new ModelInfo
            {
                Name = "Accord.NET Neural Network Quality Model",
                Description = "Neural network for evaluating procedurally generated maps",
                Version = "1.0",
                Parameters = new Dictionary<string, string>
                {
                    { "Architecture", "Multi-layer neural network" },
                    { "Activation", "Bipolar sigmoid" },
                    { "Layers", "8-16-8-1" },
                    { "Features", "Variety, Transitions, Tile distribution" },
                    { "Trained", _modelLoaded ? "Yes" : "No" }
                }
            };
        }
    }
}