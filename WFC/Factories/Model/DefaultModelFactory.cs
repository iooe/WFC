using WFC.Models.NeuralNetwork;

namespace WFC.Factories.Model;

public class DefaultModelFactory : IModelFactory
{
    public IQualityAssessmentModel CreateModel(ModelType type, string modelPath = null)
    {
        return new BasicQualityModel();
        
        // switch (type)
        // {
        //     case ModelType.Basic:
        //         return new BasicQualityModel();
        //     case ModelType.Advanced:
        //         return new AdvancedQualityModel();
        //     case ModelType.Custom:
        //         if (string.IsNullOrEmpty(modelPath))
        //             throw new ArgumentException("Model path must be provided for custom models");
        //         return new CustomQualityModel(modelPath);
        //     default:
        //         throw new ArgumentException($"Unknown model type: {type}");
        // }
    }
}