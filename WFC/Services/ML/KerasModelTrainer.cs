using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Keras;
using Keras.Layers;
using Keras.Models;
using Keras.Optimizers;
using Numpy;
using System.Text.Json;

namespace WFC.Services.ML
{
    /// <summary>
    /// Keras.NET-based model trainer for quality assessment
    /// </summary>
    public class KerasModelTrainer
    {
        private readonly string _trainingDataPath;
        private readonly string _modelOutputPath;

        public KerasModelTrainer(string trainingDataPath, string modelOutputPath = null)
        {
            _trainingDataPath = trainingDataPath;
            _modelOutputPath = modelOutputPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "keras_quality_model.h5");
        }

        /// <summary>
        /// Train a new Keras model
        /// </summary>
        public async Task<string> TrainModelAsync()
        {
            // Load training data
            var trainingData = await LoadTrainingDataAsync();

            if (trainingData.Count == 0)
            {
                throw new InvalidOperationException("No training data available");
            }

            Console.WriteLine($"Training Keras model with {trainingData.Count} examples");

            // Prepare data for training
            (NDarray x_train, NDarray y_train) = PrepareTrainingData(trainingData);

            // Create output directory if needed
            string modelDirectory = Path.GetDirectoryName(_modelOutputPath);
            if (!Directory.Exists(modelDirectory))
            {
                Directory.CreateDirectory(modelDirectory);
            }

            // Build and train the model
            var model = BuildModel();
            TrainModel(model, x_train, y_train);

            // Save the trained model
            model.Save(_modelOutputPath);
            Console.WriteLine($"Model saved to {_modelOutputPath}");

            return _modelOutputPath;
        }

        /// <summary>
        /// Build the neural network model
        /// </summary>
        private Sequential BuildModel()
        {
            // Create a sequential model
            var model = new Sequential();
            
            // Input and first hidden layer
            model.Add(new Dense(16, activation: "relu", input_shape: new Shape(8)));
            model.Add(new Dropout(0.2));
            
            // Second hidden layer
            model.Add(new Dense(32, activation: "relu"));
            model.Add(new Dropout(0.2));
            
            // Third hidden layer
            model.Add(new Dense(16, activation: "relu"));
            
            // Output layer - single value for quality score (0-1)
            model.Add(new Dense(1, activation: "sigmoid"));

            // Compile the model
            model.Compile(
                optimizer: new Adam(lr: (float)0.001),
                loss: "mse",
                metrics: new string[] { "mae" }
            );

            // Print model summary
            model.Summary();

            return model;
        }

        /// <summary>
        /// Train the model with the provided data
        /// </summary>
        private void TrainModel(Sequential model, NDarray x_train, NDarray y_train)
        {
            // Training parameters
            int batchSize = Math.Max(4, Math.Min(32, x_train.shape[0] / 10)); // Adaptive batch size
            int epochs = 200;
            float validationSplit = 0.2f;

            Console.WriteLine($"Training with batch size: {batchSize}, epochs: {epochs}, validation split: {validationSplit}");

            // Train the model with early stopping
            // в Keras.NET, callbacks передаются как отдельные параметры в Fit
            model.Fit(
                x_train, y_train,
                batch_size: batchSize,
                epochs: epochs,
                verbose: 1,
                validation_split: validationSplit,
                callbacks: [new Keras.Callbacks.EarlyStopping(
                    monitor: "val_loss",
                    patience: 20,
                    verbose: 1,
                    restore_best_weights: true
                )]
            );
        }

        /// <summary>
        /// Prepare training data for Keras
        /// </summary>
        private (NDarray, NDarray) PrepareTrainingData(List<TrainingDataCollector.TrainingExample> examples)
        {
            try
            {
                Console.WriteLine("Preparing training data for Keras...");
                
                // Initialize feature and target arrays
                float[,] features = new float[examples.Count, 8];
                float[] targets = new float[examples.Count];

                // Fill the arrays with data
                for (int i = 0; i < examples.Count; i++)
                {
                    var example = examples[i];
                    
                    // Extract features
                    features[i, 0] = example.FeatureValues.GetValueOrDefault("VarietyScore", 0.5f);
                    features[i, 1] = NormalizeTransitionCount(example.FeatureValues.GetValueOrDefault("TransitionCount", 0f));
                    features[i, 2] = example.FeatureValues.GetValueOrDefault("TransitionDensity", 0.5f);
                    features[i, 3] = example.FeatureValues.GetValueOrDefault("Ratio_grass", 0f);
                    features[i, 4] = example.FeatureValues.GetValueOrDefault("Ratio_flowers", 0f);
                    features[i, 5] = example.FeatureValues.GetValueOrDefault("Ratio_pavement", 0f);
                    features[i, 6] = example.FeatureValues.GetValueOrDefault("Ratio_building", 0f);
                    features[i, 7] = example.FeatureValues.GetValueOrDefault("Ratio_water", 0f);
                    
                    // Use user rating as target
                    targets[i] = example.UserRating;
                }

                // Convert to Numpy arrays
                var x_train = np.array(features);
                var y_train = np.array(targets);

                Console.WriteLine($"Prepared {x_train.shape[0]} training examples with {x_train.shape[1]} features each");
                
                return (x_train, y_train);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error preparing training data: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Normalize transition count to 0-1 range
        /// </summary>
        private float NormalizeTransitionCount(float transitionCount)
        {
            // Assume maximum reasonable transition count is 100
            float maxTransitions = 100f;
            return Math.Min(transitionCount / maxTransitions, 1.0f);
        }

        /// <summary>
        /// Load training data from JSON file
        /// </summary>
        private async Task<List<TrainingDataCollector.TrainingExample>> LoadTrainingDataAsync()
        {
            try
            {
                // Load training data from JSON file
                string json = await File.ReadAllTextAsync(_trainingDataPath);
                var examples = JsonSerializer.Deserialize<List<TrainingDataCollector.TrainingExample>>(json);

                // Filter out unrated examples
                var ratedExamples = examples.Where(e => e.UserRating > 0).ToList();
                
                Console.WriteLine($"Loaded {ratedExamples.Count} rated examples from {examples.Count} total examples");
                
                return ratedExamples;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading training data: {ex}");
                throw;
            }
        }
    }
}