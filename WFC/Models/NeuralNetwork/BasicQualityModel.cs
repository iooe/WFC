namespace WFC.Models.NeuralNetwork;

public class BasicQualityModel : IQualityAssessmentModel
{
    public Task<QualityAssessment> EvaluateAsync(Tile[,] tileMap)
    {
        throw new NotImplementedException();
    }

    public ModelInfo GetModelInfo()
    {
        throw new NotImplementedException();
    }
}