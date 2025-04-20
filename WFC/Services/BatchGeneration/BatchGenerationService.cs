using System.IO;
using System.Text.Json;
using WFC.Models;
using WFC.Services.Export;
using WFC.ViewModels;

namespace WFC.Services.BatchGeneration
{
    /// <summary>
    /// Service for generating multiple maps in a batch
    /// </summary>
    public class BatchGenerationService
    {
        private readonly IWFCService _wfcService;
        private readonly IExporterFactory _exporterFactory;
        private readonly TileConfigManager _tileConfigManager;

        public event EventHandler<BatchProgressEventArgs> BatchProgressChanged;

        public BatchGenerationService(
            IWFCService wfcService,
            IExporterFactory exporterFactory,
            TileConfigManager tileConfigManager)
        {
            _wfcService = wfcService;
            _exporterFactory = exporterFactory;
            _tileConfigManager = tileConfigManager;
        }

        /// <summary>
        /// Generate multiple maps in a batch
        /// </summary>
        /// <param name="parameters">Batch generation parameters</param>
        /// <param name="exportPath">Path to export the maps</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Result of the batch generation</returns>
        public async Task<BatchGenerationResult> GenerateBatchAsync(
            BatchGenerationParameters parameters,
            string exportPath,
            CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(exportPath))
                return new BatchGenerationResult { Success = false, Message = "Export path is required" };

            // Ensure export directory exists
            Directory.CreateDirectory(exportPath);

            var result = new BatchGenerationResult
            {
                GeneratedMaps = new List<GeneratedMapInfo>(),
                StartTime = DateTime.Now
            };

            try
            {
                // Loop for each map to generate
                for (int i = 0; i < parameters.MapCount; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        result.Success = false;
                        result.Message = "Operation cancelled";
                        return result;
                    }

                    // Report progress
                    ReportBatchProgress(i, parameters.MapCount, $"Generating map {i + 1} of {parameters.MapCount}...");

                    // Generate a single map
                    var mapResult = await GenerateSingleMapAsync(parameters, i, exportPath, token);

                    if (mapResult != null)
                    {
                        result.GeneratedMaps.Add(mapResult);
                    }
                }

                // Finalize result
                result.Success = true;
                result.EndTime = DateTime.Now;
                result.Message = $"Successfully generated {result.GeneratedMaps.Count} maps";

                ReportBatchProgress(parameters.MapCount, parameters.MapCount, result.Message);
                return result;
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.EndTime = DateTime.Now;
                result.Message = "Operation cancelled";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.EndTime = DateTime.Now;
                result.Message = $"Error generating batch: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Generate a single map and export it
        /// </summary>
        private async Task<GeneratedMapInfo> GenerateSingleMapAsync(
            BatchGenerationParameters parameters,
            int index,
            string exportPath,
            CancellationToken token)
        {
            // Create a unique seed for this map if not specified
            int? seed = parameters.UseSeed ? parameters.Seed : null;
            if (seed == null)
            {
                // Use index and current time to generate a unique seed
                seed = (DateTime.Now.Ticks + index).GetHashCode();
            }

            // Create settings for this map
            var settings = _tileConfigManager.CreateSettings(
                parameters.Width,
                parameters.Height,
                false, // No debug rendering for batch generation
                seed);

            // Generate the map
            var generationResult = await _wfcService.GenerateAsync(settings, token);

            if (!generationResult.Success || generationResult.Grid == null)
            {
                return null;
            }

            // Create map info
            var mapInfo = new GeneratedMapInfo
            {
                Seed = seed.Value,
                Width = parameters.Width,
                Height = parameters.Height,
                GenerationTime = DateTime.Now
            };

            // Export the map
            await ExportMapAsync(generationResult.Grid, mapInfo, index, parameters, exportPath);

            return mapInfo;
        }

        /// <summary>
        /// Export a map to the specified location
        /// </summary>
        private async Task ExportMapAsync(
            Tile[,] grid,
            GeneratedMapInfo mapInfo,
            int index,
            BatchGenerationParameters parameters,
            string exportPath)
        {
            // Create a list of TileDisplay objects for the exporters to use
            var tileDisplays = CreateTileDisplaysFromGrid(grid);

            // Generate filename base
            string filenameBase = $"map_{index + 1:D4}_seed{mapInfo.Seed}";
            string pngFilePath = Path.Combine(exportPath, $"{filenameBase}.png");
            string tilesFolder = Path.Combine(exportPath, filenameBase);

            // Export based on format
            switch (parameters.ExportFormat)
            {
                case ExportFormat.PNG:
                {
                    var exporter = _exporterFactory.CreateExporter(ExportType.Png);
                    string result =
                        await exporter.ExportAsync(tileDisplays, mapInfo.Width, mapInfo.Height, pngFilePath);
                    mapInfo.FilePath = pngFilePath;
                }
                    break;
                case ExportFormat.Tiles:
                {
                    var exporter = _exporterFactory.CreateExporter(ExportType.Tiles);
                    string result =
                        await exporter.ExportAsync(tileDisplays, mapInfo.Width, mapInfo.Height, tilesFolder);
                    mapInfo.FilePath = tilesFolder;
                }
                    break;
                case ExportFormat.Both:
                {
                    var pngExporter = _exporterFactory.CreateExporter(ExportType.Png);
                    await pngExporter.ExportAsync(tileDisplays, mapInfo.Width, mapInfo.Height, pngFilePath);

                    var tilesExporter = _exporterFactory.CreateExporter(ExportType.Tiles);
                    await tilesExporter.ExportAsync(tileDisplays, mapInfo.Width, mapInfo.Height, tilesFolder);

                    mapInfo.FilePath = pngFilePath;
                }
                    break;
            }

            // Export metadata (seed, dimensions, etc.)
            ExportMetadata(mapInfo, filenameBase, exportPath);
        }

        /// <summary>
        /// Create TileDisplay objects from a grid
        /// </summary>
        private List<TileDisplay> CreateTileDisplaysFromGrid(Tile[,] grid)
        {
            var tileDisplays = new List<TileDisplay>();
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var tile = grid[x, y];
                    if (tile != null)
                    {
                        float xPos = x * 100;
                        float yPos = y * 100;
                        tileDisplays.Add(new TileDisplay(tile, xPos, yPos));
                    }
                }
            }

            return tileDisplays;
        }

        /// <summary>
        /// Export metadata for a map
        /// </summary>
        private void ExportMetadata(GeneratedMapInfo mapInfo, string filenameBase, string exportPath)
        {
            var metadata = new
            {
                mapInfo.Seed,
                mapInfo.Width,
                mapInfo.Height,
                mapInfo.GenerationTime,
                mapInfo.FilePath
            };

            string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string metadataPath = Path.Combine(exportPath, $"{filenameBase}.json");
            File.WriteAllText(metadataPath, json);
        }

        /// <summary>
        /// Report batch generation progress
        /// </summary>
        private void ReportBatchProgress(int current, int total, string status)
        {
            float progress = (float)current / total * 100;
            BatchProgressChanged?.Invoke(this, new BatchProgressEventArgs(progress, status));
        }
    }
}