using System.IO;
using System.Windows.Controls;
using WFC.ViewModels;
using Image = System.Windows.Controls.Image;

namespace WFC.Services.Export;

public class TilesExporter : ExporterBase
{
    private readonly IDialogService _dialogService;
    private readonly IFileSystem _fileSystem;
    private readonly IVisualHelper _visualHelper;

    public TilesExporter(IDialogService dialogService, IFileSystem fileSystem, IVisualHelper visualHelper)
    {
        _dialogService = dialogService;
        _fileSystem = fileSystem;
        _visualHelper = visualHelper;
    }

    public override async Task<string> ExportAsync(IEnumerable<TileDisplay> tiles, int gridWidth, int gridHeight,
        string exportPath = null)
    {
        string folderPath;

        // If export path is provided (batch mode), use it directly
        if (!string.IsNullOrEmpty(exportPath))
        {
            folderPath = exportPath;
            _fileSystem.CreateDirectory(folderPath);
        }
        else
        {
            // Otherwise use dialog for manual export (single map)
            folderPath = _dialogService.ShowFolderBrowserDialog("Select folder to export tiles");

            if (string.IsNullOrEmpty(folderPath))
                return "Export cancelled";

            // Create subdirectory with timestamp
            string subfolder = $"WFC_Tiles_{DateTime.Now:yyyyMMdd_HHmmss}";
            folderPath = Path.Combine(folderPath, subfolder);
            _fileSystem.CreateDirectory(folderPath);
        }

        // Export each tile as a separate image
        int count = 0;
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                // Find tile at this position
                var tile = tiles.FirstOrDefault(t =>
                    Math.Abs(t.X - (x * 100)) < 0.1 &&
                    Math.Abs(t.Y - (y * 100)) < 0.1);

                if (tile != null)
                {
                    // Create filename based on coordinates
                    string filename = Path.Combine(folderPath, $"tile_x{x}_y{y}.png");

                    // Create small canvas with just this tile
                    var canvas = new Canvas { Width = 100, Height = 100 };
                    var image = new Image
                    {
                        Source = tile.Image,
                        Width = 100,
                        Height = 100
                    };
                    canvas.Children.Add(image);

                    // Save tile as PNG
                    _visualHelper.CaptureElementToPng(canvas, filename, 100, 100);

                    count++;
                }
            }
        }

        return $"Exported {count} tiles to {folderPath}";
    }
}