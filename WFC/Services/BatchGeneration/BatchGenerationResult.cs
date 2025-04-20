namespace WFC.Services.BatchGeneration;

/// <summary>
/// Result of a batch generation
/// </summary>
public class BatchGenerationResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<GeneratedMapInfo> GeneratedMaps { get; set; } = new List<GeneratedMapInfo>();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
        
    public TimeSpan ElapsedTime => EndTime - StartTime;
}
