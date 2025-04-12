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

    public override async Task<string> ExportAsync(IEnumerable<TileDisplay> tiles, int gridWidth, int gridHeight)
    {
        // Используем диалог для получения имени выходного файла
        var filePath = _dialogService.ShowSaveFileDialog(
            "Export map as PNG",
            "PNG Image|*.png",
            ".png",
            $"WFC_Map_{DateTime.Now:yyyyMMdd_HHmmss}");

        if (string.IsNullOrEmpty(filePath))
            return "Export cancelled";

        // Вычисляем точные размеры
        int width = gridWidth * 100;
        int height = gridHeight * 100;

        // Создаем Canvas для экспорта
        var exportCanvas = CreateExportCanvas(tiles, gridWidth, gridHeight);

        // Используем helper для захвата и сохранения
        _visualHelper.CaptureElementToPng(exportCanvas, filePath, width, height);

        return $"Map exported as PNG to {filePath}";
    }
}