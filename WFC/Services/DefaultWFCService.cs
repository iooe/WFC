using WFC.Models;

namespace WFC.Services
{
    public class DefaultWFCService : IWFCService
    {
        private Random random;
        private Cell[,] grid;
        private WFCSettings settings;
        private float totalCells;
        private int collapsedCells;

        public event EventHandler<WFCProgressEventArgs> ProgressChanged;

        public async Task<WFCResult> GenerateAsync(WFCSettings settings, CancellationToken token = default)
        {
            // Создаем новый генератор случайных чисел с уникальным seed
            random = new Random(Guid.NewGuid().GetHashCode());

            this.settings = settings;
            InitializeGrid();
            totalCells = settings.Width * settings.Height;
            collapsedCells = 0;

            try
            {
                // Запускаем алгоритм в фоновом потоке
                return await Task.Run(() =>
                {
                    // Используем упрощенный WFC алгоритм
                    UpdateProgress("Starting simplified WFC algorithm");

                    // Этап 1: Создаем базовую карту с зашумлением
                    bool[,] isLandMap = CreateNoiseBasedMap(settings.Width, settings.Height);

                    // Этап 2: Преобразуем базовую карту в тайлы с учетом правил соединения
                    for (int y = 0; y < settings.Height; y++)
                    {
                        for (int x = 0; x < settings.Width; x++)
                        {
                            token.ThrowIfCancellationRequested();

                            // Выбираем тип тайла на основе карты и соседей
                            int tileType = DetermineTileType(x, y, isLandMap);

                            // Схлопываем клетку
                            grid[x, y].Collapse(tileType);
                            collapsedCells++;

                            // Обновляем прогресс каждые N ячеек
                            if ((x + y * settings.Width) % 10 == 0)
                            {
                                UpdateProgress($"Processing: {collapsedCells}/{totalCells} cells");
                            }
                        }
                    }

                    // Этап 3: Проверяем и исправляем несоответствия
                    FixInconsistencies();

                    UpdateProgress("WFC generation completed successfully");

                    return new WFCResult
                    {
                        Success = true,
                        Grid = GetResultGrid(),
                        ErrorMessage = null
                    };
                }, token);
            }
            catch (OperationCanceledException)
            {
                return new WFCResult { Success = false, ErrorMessage = "Operation canceled" };
            }
            catch (Exception ex)
            {
                return new WFCResult { Success = false, ErrorMessage = $"Error: {ex.Message}" };
            }
        }

        // Создание шумовой карты с использованием упрощенного алгоритма
// Create a noise-based map for the new tile types
        private bool[,] CreateNoiseBasedMap(int width, int height)
        {
            // For our new system we'll use this to determine where flowers appear
            // true = flowers potential, false = no flowers
            bool[,] flowersPotential = new bool[width, height];

            // Starting values for noise
            double xFreq = 1.0 / (width / 2.0);
            double yFreq = 1.0 / (height / 2.0);

            // Generate the basic "noise" map
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate pseudo-noise for this position
                    double noise = Math.Sin(x * xFreq * 3.1) * Math.Cos(y * yFreq * 2.7) +
                                   Math.Sin((x + y) * xFreq * 1.5) * 0.5 +
                                   Math.Cos(x * xFreq * 2.0 - y * yFreq * 3.0) * 0.25;

                    // Add randomness
                    noise += (random.NextDouble() * 0.8 - 0.4);

                    // Determine terrain type: true = flowers potential, false = no flowers potential
                    // We use a higher threshold here to make flowers less common
                    flowersPotential[x, y] = noise > 0.3;
                }
            }

            // Apply cellular automata to smooth the map
            bool[,] smoothed = new bool[width, height];
            for (int pass = 0; pass < 2; pass++) // 2 smoothing passes
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int flowerNeighbors = 0;
                        int totalNeighbors = 0;

                        // Check neighbors in a 3x3 area
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;

                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    totalNeighbors++;
                                    if (flowersPotential[nx, ny]) flowerNeighbors++;
                                }
                            }
                        }

                        // Smoothing rule: majority determines type
                        double flowerRatio = (double)flowerNeighbors / totalNeighbors;

                        // Add some randomness for variety
                        if (random.NextDouble() < 0.1) // 10% chance for random decision
                        {
                            smoothed[x, y] = random.NextDouble() < 0.3; // Less likely to be flowers
                        }
                        else
                        {
                            // Mostly follow majority rule
                            smoothed[x, y] = flowerRatio > 0.5;
                        }
                    }
                }

                // Copy the smoothed map for the next pass
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        flowersPotential[x, y] = smoothed[x, y];
                    }
                }
            }

            return flowersPotential;
        }

// Determine tile type based on neighbors and noise map
        private int DetermineTileType(int x, int y, bool[,] flowersPotential)
        {
            // Check if this position can have flowers based on our noise map
            bool canHaveFlowers = flowersPotential[x, y];

            // Check for pavement in a more clustered pattern
            // Create a simple deterministic pattern for pavement
            bool isPavementArea = (x % 7 == 0 && y % 7 == 0) || // Create occasional pavement clusters
                                  ((x % 7 == 1 || x % 7 == 2) && y % 7 == 0) || // Extend horizontally
                                  (x % 7 == 0 && (y % 7 == 1 || y % 7 == 2)); // Extend vertically

            // Add some randomness to pavement placement
            double pavementRandom = Math.Sin(x * 0.3) * Math.Cos(y * 0.4) + random.NextDouble() * 0.4;
            bool isPavement = isPavementArea && pavementRandom > 0.2;

            // Determine the tile type based on our logic
            if (isPavement)
            {
                return TileTypes.PAVEMENT;
            }
            else if (canHaveFlowers && random.NextDouble() < 0.6) // 60% chance for flowers in flower potential areas
            {
                return TileTypes.FLOWERS;
            }
            else
            {
                return TileTypes.GRASS;
            }
        }

        // Исправление возможных несоответствий на стыках тайлов
// Fix potential inconsistencies between adjacent tiles
        private void FixInconsistencies()
        {
            // For our new tile system, we have simpler rules for consistency
            // We'll still check each tile to ensure transitions look good
            for (int y = 0; y < settings.Height; y++)
            {
                for (int x = 0; x < settings.Width; x++)
                {
                    int tileId = grid[x, y].CollapsedState.Value;

                    // Check neighbors and fix inconsistencies
                    foreach (var (nx, ny, direction) in GetNeighbors(x, y))
                    {
                        if (!IsValidPosition(nx, ny)) continue;

                        int neighborId = grid[nx, ny].CollapsedState.Value;
                        string oppositeDir = GetOppositeDirection(direction);

                        // Get the surface types
                        var mySurface = TileTypes.GetSurfaceType(tileId, direction);
                        var neighborSurface = TileTypes.GetSurfaceType(neighborId, oppositeDir);

                        // In our new system, we don't have strict rules for transitions
                        // But we want to avoid too many abrupt changes

                        // Apply a small chance to fix transitions based on weights
                        // This creates smoother transitions

                        // For high contrast transitions (like pavement to flowers)
                        // we apply a higher chance of smoothing
                        bool isHighContrastTransition =
                            (mySurface == SurfaceType.Pavement && neighborSurface == SurfaceType.Flowers) ||
                            (mySurface == SurfaceType.Flowers && neighborSurface == SurfaceType.Pavement);

                        // The chance to smooth depends on the transition type
                        double smoothingChance = isHighContrastTransition ? 0.4 : 0.2;

                        if (mySurface != neighborSurface && random.NextDouble() < smoothingChance)
                        {
                            // Choose whether to change current tile or neighbor
                            if (random.NextDouble() < 0.5)
                            {
                                // 50% chance to fix current tile
                                // Use grass more often as a transition tile
                                int newTileId = random.NextDouble() < 0.7
                                    ? TileTypes.GRASS
                                    : GetCompatibleTile(direction, neighborSurface);

                                grid[x, y] = new Cell(settings.Tiles.Count);
                                grid[x, y].Collapse(newTileId);
                            }
                            else
                            {
                                // 50% chance to fix neighbor
                                // Use grass more often as a transition tile
                                int newNeighborId = random.NextDouble() < 0.7
                                    ? TileTypes.GRASS
                                    : GetCompatibleTile(oppositeDir, mySurface);

                                grid[nx, ny] = new Cell(settings.Tiles.Count);
                                grid[nx, ny].Collapse(newNeighborId);
                            }
                        }
                    }
                }
            }
        }

        // Получить совместимый тайл для данного направления и поверхности
// Get a compatible tile type for a given direction and target surface
        private int GetCompatibleTile(string direction, SurfaceType targetSurface)
        {
            // For our new system, we simply return the tile type that matches the target surface
            // Since we don't have "shore" tiles or directional tiles anymore

            switch (targetSurface)
            {
                case SurfaceType.Grass:
                    return TileTypes.GRASS;

                case SurfaceType.Flowers:
                    return TileTypes.FLOWERS;

                case SurfaceType.Pavement:
                    return TileTypes.PAVEMENT;

                default:
                    // Default to grass as fallback
                    return TileTypes.GRASS;
            }
        }

        // Инициализация сетки пустыми клетками
        private void InitializeGrid()
        {
            grid = new Cell[settings.Width, settings.Height];
            for (int x = 0; x < settings.Width; x++)
            {
                for (int y = 0; y < settings.Height; y++)
                {
                    grid[x, y] = new Cell(settings.Tiles.Count);
                }
            }
        }

        // Получение противоположного направления
        private string GetOppositeDirection(string direction)
        {
            return direction switch
            {
                "right" => "left",
                "left" => "right",
                "up" => "down",
                "down" => "up",
                _ => throw new ArgumentException($"Invalid direction: {direction}")
            };
        }

        // Проверка валидности позиции
        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < settings.Width && y >= 0 && y < settings.Height;
        }

        // Получение соседей во всех 4 направлениях
        private List<(int x, int y, string direction)> GetNeighbors(int x, int y)
        {
            return new List<(int x, int y, string direction)>
            {
                (x - 1, y, "left"),
                (x + 1, y, "right"),
                (x, y - 1, "up"),
                (x, y + 1, "down")
            };
        }

        // Получение итоговой сетки тайлов
        private Tile[,] GetResultGrid()
        {
            var result = new Tile[settings.Width, settings.Height];

            for (int x = 0; x < settings.Width; x++)
            {
                for (int y = 0; y < settings.Height; y++)
                {
                    var cell = grid[x, y];
                    if (cell.Collapsed && cell.PossibleStates.Count > 0)
                    {
                        result[x, y] = settings.Tiles[cell.CollapsedState.Value];
                    }
                    else
                    {
                        // Для несхлопнутых клеток используем тайл земли по умолчанию
                        result[x, y] = settings.Tiles[0];
                    }
                }
            }

            return result;
        }

        // Обновление прогресса
        private void UpdateProgress(string status)
        {
            var progress = Math.Min(100f, collapsedCells / totalCells * 100);
            ProgressChanged?.Invoke(this, new WFCProgressEventArgs(progress, status));
        }

        // Сброс сервиса
        public void Reset()
        {
            grid = null;
            settings = null;
            collapsedCells = 0;
            UpdateProgress("Ready");
        }
    }
}