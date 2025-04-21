using System.IO;
using Microsoft.ML;

namespace WFC.Services.ML;

    /// <summary>
    /// Model trainer for neural network
    /// </summary>
    public class ModelTrainer
    {
        private readonly MLContext _mlContext;
        private readonly string _trainingDataPath;

        public ModelTrainer(string trainingDataPath)
        {
            _mlContext = new MLContext(seed: 0);
            _trainingDataPath = trainingDataPath;
        }

        public async Task<ITransformer> TrainModel()
        {
            // Load training data
            var trainingData = await LoadTrainingData();

            if (trainingData.Count == 0)
            {
                throw new InvalidOperationException("No training data available");
            }

            // Create training dataset
            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            // Define ML pipeline
            var pipeline = _mlContext.Transforms.Concatenate("Features",
                    "VarietyScore", "TransitionCount", "TransitionDensity",
                    "RatioGrass", "RatioFlowers", "RatioPavement", "RatioBuilding", "RatioWater")
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.Regression.Trainers.FastTree(
                    labelColumnName: "Rating",
                    numberOfLeaves: 20,
                    numberOfTrees: 100,
                    minimumExampleCountPerLeaf: 1));

            Console.WriteLine("Training model...");
            var model = pipeline.Fit(dataView);
            Console.WriteLine("Model training complete!");

            return model;
        }

        private async Task<List<MapRatingData>> LoadTrainingData()
        {
            // Load training data from JSON file
            string json = await File.ReadAllTextAsync(_trainingDataPath);
            var examples =
                global::System.Text.Json.JsonSerializer.Deserialize<List<TrainingDataCollector.TrainingExample>>(json);

            // Convert to model training data
            var trainingData = new List<MapRatingData>();

            foreach (var example in examples)
            {
                if (example.UserRating <= 0)
                    continue; // Skip unrated examples

                var data = new MapRatingData
                {
                    Rating = example.UserRating,
                    VarietyScore = example.FeatureValues.GetValueOrDefault("VarietyScore", 0.5f),
                    TransitionCount = example.FeatureValues.GetValueOrDefault("TransitionCount", 0),
                    TransitionDensity = example.FeatureValues.GetValueOrDefault("TransitionDensity", 0.5f),
                    RatioGrass = example.FeatureValues.GetValueOrDefault("Ratio_grass", 0),
                    RatioFlowers = example.FeatureValues.GetValueOrDefault("Ratio_flowers", 0),
                    RatioPavement = example.FeatureValues.GetValueOrDefault("Ratio_pavement", 0),
                    RatioBuilding = example.FeatureValues.GetValueOrDefault("Ratio_building", 0),
                    RatioWater = example.FeatureValues.GetValueOrDefault("Ratio_water", 0)
                };

                trainingData.Add(data);
            }

            return trainingData;
        }

        public void SaveModel(ITransformer model, string outputPath)
        {
            // Create directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            // Save model to file
            _mlContext.Model.Save(model, null, outputPath);
            Console.WriteLine($"Model saved to {outputPath}");
        }

        // Data class for ML.NET training
        public class MapRatingData
        {
            public float Rating { get; set; }
            public float VarietyScore { get; set; }
            public float TransitionCount { get; set; }
            public float TransitionDensity { get; set; }
            public float RatioGrass { get; set; }
            public float RatioFlowers { get; set; }
            public float RatioPavement { get; set; }
            public float RatioBuilding { get; set; }
            public float RatioWater { get; set; }
        }
    }