namespace WFC.Services;

public class WFCProgressEventArgs : EventArgs
{
    public float Progress { get; }
    public string Status { get; }

    public WFCProgressEventArgs(float progress, string status)
    {
        Progress = progress;
        Status = status;
    }
}