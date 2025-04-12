using System.Windows.Controls;
using WFC.ViewModels;
using Image = System.Windows.Controls.Image;
namespace WFC.Services.Export;

public abstract class ExporterBase : IExporter
{
    // Общая логика для всех экспортеров
    protected Canvas CreateExportCanvas(IEnumerable<TileDisplay> tiles, int width, int height)
    {
        // Создаем Canvas для экспорта с нужными размерами
        var exportCanvas = new Canvas { Width = width * 100, Height = height * 100 };

        // Для каждой плитки создаем и позиционируем изображение
        foreach (var tile in tiles)
        {
            // Создаем новое изображение для каждой плитки
            var img = new Image
            {
                Source = tile.Image,
                Width = 100,
                Height = 100
            };

            // Позиционируем его правильно
            Canvas.SetLeft(img, tile.X);
            Canvas.SetTop(img, tile.Y);

            // Добавляем на наш canvas для экспорта
            exportCanvas.Children.Add(img);
        }

        return exportCanvas;
    }

    // Абстрактный метод, который должны реализовать наследники
    public abstract Task<string> ExportAsync(IEnumerable<TileDisplay> tiles, int gridWidth, int gridHeight);
}