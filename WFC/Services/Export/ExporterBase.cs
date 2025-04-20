using System.Windows.Controls;
using WFC.ViewModels;
using Image = System.Windows.Controls.Image;

namespace WFC.Services.Export;

public abstract class ExporterBase : IExporter
{
    // Common logic for all exports 
    protected Canvas CreateExportCanvas(IEnumerable<TileDisplay> tiles, int width, int height)
    {
        var exportCanvas = new Canvas { Width = width * 100, Height = height * 100 };

        foreach (var tile in tiles)
        {
            var img = new Image
            {
                Source = tile.Image,
                Width = 100,
                Height = 100
            };

            Canvas.SetLeft(img, tile.X);
            Canvas.SetTop(img, tile.Y);

            exportCanvas.Children.Add(img);
        }

        return exportCanvas;
    }

    public abstract Task<string> ExportAsync(IEnumerable<TileDisplay> tiles, int gridWidth, int gridHeight,
        string exportPath = null);
}