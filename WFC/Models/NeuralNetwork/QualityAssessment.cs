namespace WFC.Models.NeuralNetwork;

public class QualityAssessment
{
    public float OverallScore { get; set; }
    public Dictionary<string, float> DimensionalScores { get; set; }
    public string[] Feedback { get; set; }
}