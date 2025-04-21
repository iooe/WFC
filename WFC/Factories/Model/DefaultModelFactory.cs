using System;
using System.IO;
using Microsoft.ML;
using WFC.Models.NeuralNetwork;
using WFC.Services.ML;

namespace WFC.Factories.Model
{
    public class DefaultModelFactory : IModelFactory
    {
        public IQualityAssessmentModel CreateModel(ModelType type, string modelPath = null)
        {
            switch (type)
            {
                case ModelType.Basic:
                    return new BasicQualityModel();
                    
                case ModelType.Advanced:
                    return new AdvancedQualityModel();
                    
                case ModelType.Custom:
                    if (string.IsNullOrEmpty(modelPath))
                    {
                        throw new ArgumentException("Model path must be provided for custom models");
                    }
                    return new AdvancedQualityModel(modelPath);
                    
                default:
                    throw new ArgumentException($"Unknown model type: {type}");
            }
        }
    }
}