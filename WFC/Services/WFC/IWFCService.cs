using WFC.Models;

namespace WFC.Services;

/// <summary>
/// Interface for the WFC algorithm service
/// </summary>
public interface IWFCService
{
    /// <summary>
    /// Event triggered when generation progress changes
    /// </summary>
    event EventHandler<WFCProgressEventArgs> ProgressChanged;
    
    /// <summary>
    /// Generate a new WFC grid
    /// </summary>
    /// <param name="settings">WFC settings</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Generation result</returns>
    Task<WFCResult> GenerateAsync(WFCSettings settings, CancellationToken token = default);
    
    /// <summary>
    /// Reset the service
    /// </summary>
    void Reset();
}