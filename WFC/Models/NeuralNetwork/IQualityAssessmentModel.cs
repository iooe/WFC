namespace WFC.Models.NeuralNetwork;

public interface IQualityAssessmentModel
{
    /// <summary>
    /// Evaluates the quality of a generated map
    /// </summary>
    /// <param name="tileMap">The tile map to evaluate</param>
    /// <returns>Assessment results containing scores and feedback</returns>
    Task<QualityAssessment> EvaluateAsync(Tile[,] tileMap);
        
    /// <summary>
    /// Gets information about the model
    /// </summary>
    ModelInfo GetModelInfo();
}