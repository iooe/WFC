namespace WFC.Services.BatchGeneration;

/// <summary>
/// Event args for batch generation progress
/// </summary>
public class BatchProgressEventArgs : EventArgs
{
    public float Progress { get; }
    public string Status { get; }

    public BatchProgressEventArgs(float progress, string status)
    {
        Progress = progress;
        Status = status;
    }
}