
using WFC.ViewModels;

namespace WFC.Services.Export;

public interface IExporter
{
    Task<string> ExportAsync(IEnumerable<TileDisplay> tiles, int gridWidth, int gridHeight);
}