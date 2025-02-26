using WFC.Models;

namespace WFC.Services;

public interface IWFCService
{
    event EventHandler<WFCProgressEventArgs> ProgressChanged;
    Task<WFCResult> GenerateAsync(WFCSettings settings, CancellationToken token = default);
    void Reset();
}