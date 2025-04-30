using System.IO;
using Accord.Neuro;
using System.Text.Json;
using Accord.Neuro;
using Accord.Neuro.Learning;

namespace WFC.Services.ML
{
    /// <summary>
    /// Neural network trainer based on Accord.NET for quality assessment
    /// </summary>
    public class AccordNetModelTrainer
    {
        private readonly string _trainingDataPath;
        private readonly string _modelOutputPath;
        
        // Training parameters
        private readonly int _iterations = 2000;
        private readonly double _learningRate = 0.1;
        private readonly double _momentum = 0.5;

        public AccordNetModelTrainer(string trainingDataPath, string modelOutputPath = null)
        {
            _trainingDataPath = trainingDataPath;
            _modelOutputPath = modelOutputPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "accord_quality_model.bin");
        }

        /// <summary>
        /// Train a new neural network model
        /// </summary>
        public async Task<string> TrainModelAsync()
        {
            // Load training data
            var trainingData = await LoadTrainingDataAsync();

            if (trainingData.Count == 0)
            {
                throw new InvalidOperationException("No training data available");
            }

            Console.WriteLine($"Training neural network with {trainingData.Count} examples");

            // Prepare data for training
            (double[][] inputs, double[] outputs) = PrepareTrainingData(trainingData);

            // Create output directory if necessary
            string modelDirectory = Path.GetDirectoryName(_modelOutputPath);
            if (!Directory.Exists(modelDirectory))
            {
                Directory.CreateDirectory(modelDirectory);
            }

            // Create and train neural network
            var network = BuildAndTrainNetwork(inputs, outputs);

            // Save trained model
            network.Save(_modelOutputPath);
            Console.WriteLine($"Model saved to {_modelOutputPath}");

            return _modelOutputPath;
        }

        /// <summary>
        /// Build and train the neural network
        /// </summary>
        private ActivationNetwork BuildAndTrainNetwork(double[][] inputs, double[] outputs)
        {
            // Create multilayer neural network with BipolarSigmoid activation function
            // Architecture: 8 inputs -> 16 neurons -> 8 neurons -> 1 output
            var network = new ActivationNetwork(
                new BipolarSigmoidFunction(2.0), // Activation function
                8,   // Input neurons (number of features)
                16,  // First hidden layer neurons
                8,   // Second hidden layer neurons
                1);  // Output neuron (quality assessment)

            // Initialize with random values
            new NguyenWidrow(network).Randomize();

            // Create backpropagation learning algorithm
            var teacher = new BackPropagationLearning(network)
            {
                LearningRate = _learningRate,
                Momentum = _momentum
            };

            // Train neural network
            Console.WriteLine("Starting neural network training...");
            double error = double.MaxValue;
            
            // Training loop
            for (int i = 0; i < _iterations; i++)
            {
                // Perform one training epoch
                double[][] outputArrays = new double[outputs.Length][];
                
                for (int i2 = 0; i2 < outputs.Length; i2++)
                {
                    outputArrays[i2] = new double[] { outputs[i2] };
                }

                error = teacher.RunEpoch(inputs, outputArrays);           
                
                // Output progress every 100 iterations
                if ((i + 1) % 100 == 0 || i == 0 || i == _iterations - 1)
                {
                    Console.WriteLine($"Iteration {i + 1}/{_iterations}: error = {error:F6}");
                }
                
                // Early stopping when low error is achieved
                if (error < 0.001)
                {
                    Console.WriteLine($"Training stopped at iteration {i + 1} with error {error:F6}");
                    break;
                }
            }

            Console.WriteLine($"Neural network training completed with final error: {error:F6}");
            
            return network;
        }

        /// <summary>
        /// Prepare data for neural network training
        /// </summary>
        private (double[][] inputs, double[] outputs) PrepareTrainingData(List<TrainingDataCollector.TrainingExample> examples)
        {
            try
            {
                Console.WriteLine("Preparing data for neural network training...");
                
                // Create arrays for training
                var inputs = new double[examples.Count][];
                var outputs = new double[examples.Count];
                
                // Fill arrays with data
                for (int i = 0; i < examples.Count; i++)
                {
                    var example = examples[i];
                    
                    // Extract features
                    inputs[i] = new double[8]
                    {
                        example.FeatureValues.GetValueOrDefault("VarietyScore", 0.5f),
                        example.FeatureValues.GetValueOrDefault("TransitionDensity", 0.5f),
                        example.FeatureValues.GetValueOrDefault("Ratio_grass", 0f),
                        example.FeatureValues.GetValueOrDefault("Ratio_flowers", 0f),
                        example.FeatureValues.GetValueOrDefault("Ratio_pavement", 0f),
                        example.FeatureValues.GetValueOrDefault("Ratio_building", 0f),
                        example.FeatureValues.GetValueOrDefault("Ratio_water", 0f),
                        NormalizeTransitionCount(example.FeatureValues.GetValueOrDefault("TransitionCount", 0f))
                    };
                    
                    // Use user rating as target value
                    outputs[i] = example.UserRating;
                }
                
                Console.WriteLine($"Prepared {inputs.Length} training examples with {inputs[0].Length} features");
                
                return (inputs, outputs);
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
        private double NormalizeTransitionCount(float transitionCount)
        {
            // Assume maximum reasonable transition count is 100
            float maxTransitions = 100f;
            return Math.Min(transitionCount / maxTransitions, 1.0);
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

                // Filter unrated examples
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