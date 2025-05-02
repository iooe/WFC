namespace WFC.Models.NeuralNetwork;

/// <summary>
/// Basic implementation of the quality assessment model
/// Returns empty results after neural network removal
/// </summary>
public class BasicQualityModel : IQualityAssessmentModel
{
    public async Task<QualityAssessment> EvaluateAsync(Tile[,] tileMap)
    {
        // After neural network deletion, return an empty assessment
        return new QualityAssessment
        {
            OverallScore = 0.5f, // Zeroed score
            DimensionalScores = new Dictionary<string, float>
            {
                { "Coherence", 0.5f },
                { "Aesthetics", 0.5f },
                { "Playability", 0.5f }
            },
            Feedback = new[] { "Neural network model has been deleted. Train a model to get quality assessment." }
        };
    }

    public ModelInfo GetModelInfo()
    {
        return new ModelInfo
        {
            Name = "No Model Available",
            Description = "Neural network has been deleted. New model training required.",
            Version = "1.0",
            Parameters = new Dictionary<string, string>
            {
                { "Type", "Empty" }
            }
        };
    }
}