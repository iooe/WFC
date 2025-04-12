namespace WFC.Models.NeuralNetwork;

public class ModelInfo
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Version { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
}