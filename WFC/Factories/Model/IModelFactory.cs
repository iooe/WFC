using WFC.Models.NeuralNetwork;

namespace WFC.Factories.Model;

public interface IModelFactory
{
    IQualityAssessmentModel CreateModel(ModelType type, string modelPath = null);
}

public enum ModelType
{
    Basic,
    Advanced,
    Custom
}