namespace WFC.Services.BatchGeneration;

/// <summary>
/// Information about a generated map
/// </summary>
public class GeneratedMapInfo
{
    public int Seed { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime GenerationTime { get; set; }
    public string FilePath { get; set; }
}