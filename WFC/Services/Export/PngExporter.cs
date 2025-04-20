using WFC.ViewModels;

namespace WFC.Services.Export;

public class PngExporter : ExporterBase
{
    private readonly IDialogService _dialogService;
    private readonly IVisualHelper _visualHelper;

    public PngExporter(IDialogService dialogService, IVisualHelper visualHelper)
    {
        _dialogService = dialogService;
        _visualHelper = visualHelper;
    }

    public override async Task<string> ExportAsync(IEnumerable<TileDisplay> tiles, int gridWidth, int gridHeight, string exportPath = null)
    {
        string filePath;
        
        // If export path is provided (batch mode), use it directly
        if (!string.IsNullOrEmpty(exportPath))
        {
            filePath = exportPath;
        }
        else
        {
            // Otherwise use dialog for manual export (single map)
            filePath = _dialogService.ShowSaveFileDialog(
                "Export map as PNG",
                "PNG Image|*.png",
                ".png",
                $"WFC_Map_{DateTime.Now:yyyyMMdd_HHmmss}");

            if (string.IsNullOrEmpty(filePath))
                return "Export cancelled";
        }

        // Calculate iamge sizes
        int width = gridWidth * 100;
        int height = gridHeight * 100;

        // Create canvas
        var exportCanvas = CreateExportCanvas(tiles, gridWidth, gridHeight);

        // Capture and Save
        _visualHelper.CaptureElementToPng(exportCanvas, filePath, width, height);

        return $"Map exported as PNG to {filePath}";
    }
}