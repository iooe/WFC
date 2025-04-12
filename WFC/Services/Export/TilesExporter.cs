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

        public override async Task<string> ExportAsync(IEnumerable<TileDisplay> tiles, int gridWidth, int gridHeight)
        {
            // Используем диалог для получения директории вывода
            var folderPath = _dialogService.ShowFolderBrowserDialog("Select folder to export tiles");

            if (string.IsNullOrEmpty(folderPath))
                return "Export cancelled";

            // Создаем поддиректорию с временной меткой
            string subfolder = $"WFC_Tiles_{DateTime.Now:yyyyMMdd_HHmmss}";
            string fullPath = Path.Combine(folderPath, subfolder);
            _fileSystem.CreateDirectory(fullPath);

            // Экспортируем каждую плитку как отдельное изображение
            int count = 0;
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    // Находим плитку в этой позиции
                    var tile = tiles.FirstOrDefault(t =>
                        Math.Abs(t.X - (x * 100)) < 0.1 &&
                        Math.Abs(t.Y - (y * 100)) < 0.1);

                    if (tile != null)
                    {
                        // Создаем имя файла на основе координат
                        string filename = Path.Combine(fullPath, $"tile_x{x}_y{y}.png");

                        // Создаем маленький canvas только с этой плиткой
                        var canvas = new Canvas { Width = 100, Height = 100 };
                        var image = new Image
                        {
                            Source = tile.Image,
                            Width = 100,
                            Height = 100
                        };
                        canvas.Children.Add(image);

                        // Сохраняем плитку как PNG
                        _visualHelper.CaptureElementToPng(canvas, filename, 100, 100);

                        count++;
                    }
                }
            }

            return $"Exported {count} tiles to {fullPath}";
        }
    }