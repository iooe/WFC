namespace WFC.Services.BatchGeneration;

/// <summary>
/// Parameters for batch generation
/// </summary>
public class BatchGenerationParameters
{
    public int MapCount { get; set; } = 10;
    public int Width { get; set; } = 32;
    public int Height { get; set; } = 32;
    public bool UseSeed { get; set; } = false;
    public int Seed { get; set; }
    public ExportFormat ExportFormat { get; set; } = ExportFormat.PNG;
}