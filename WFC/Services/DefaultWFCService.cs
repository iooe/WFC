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
        private bool[,] CreateNoiseBasedMap(int width, int height)
        {
            bool[,] isLand = new bool[width, height];
            
            // Стартовые значения для шума
            double xFreq = 1.0 / (width / 2.0);
            double yFreq = 1.0 / (height / 2.0);
            
            // Генерируем базовую "шумовую" карту
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Вычисляем псевдо-шум для данной позиции
                    double noise = Math.Sin(x * xFreq * 3.1) * Math.Cos(y * yFreq * 2.7) +
                                  Math.Sin((x + y) * xFreq * 1.5) * 0.5 +
                                  Math.Cos(x * xFreq * 2.0 - y * yFreq * 3.0) * 0.25;
                    
                    // Добавляем случайность
                    noise += (random.NextDouble() * 0.8 - 0.4);
                    
                    // Определяем тип местности: true = земля, false = вода
                    isLand[x, y] = noise > 0.0;
                }
            }
            
            // Применяем клеточные автоматы для сглаживания карты
            bool[,] smoothed = new bool[width, height];
            for (int pass = 0; pass < 2; pass++) // 2 прохода сглаживания
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int landNeighbors = 0;
                        int totalNeighbors = 0;
                        
                        // Проверяем соседей в 3x3 области
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;
                                
                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    totalNeighbors++;
                                    if (isLand[nx, ny]) landNeighbors++;
                                }
                            }
                        }
                        
                        // Правило сглаживания: большинство определяет тип
                        double landRatio = (double)landNeighbors / totalNeighbors;
                        
                        // Добавляем немного случайности для разнообразия
                        if (random.NextDouble() < 0.1) // 10% шанс на случайное решение
                        {
                            smoothed[x, y] = random.NextDouble() < 0.5;
                        }
                        else
                        {
                            // В основном следуем правилу большинства
                            smoothed[x, y] = landRatio > 0.5;
                        }
                    }
                }
                
                // Копируем сглаженную карту для следующего прохода
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        isLand[x, y] = smoothed[x, y];
                    }
                }
            }
            
            return isLand;
        }

        // Определение типа тайла на основе соседей
        private int DetermineTileType(int x, int y, bool[,] isLand)
        {
            bool isCurrentLand = isLand[x, y];
            
            // Проверяем соседей слева и справа
            bool hasLeftNeighbor = x > 0;
            bool hasRightNeighbor = x < settings.Width - 1;
            
            bool isLeftLand = hasLeftNeighbor ? isLand[x - 1, y] : isCurrentLand;
            bool isRightLand = hasRightNeighbor ? isLand[x + 1, y] : isCurrentLand;
            
            // Определяем тип тайла на основе текущей клетки и соседей
            if (isCurrentLand)
            {
                // Текущая клетка - земля
                if (!isRightLand && hasRightNeighbor)
                {
                    // Справа вода - ставим берег с водой справа
                    return TileTypes.SHORE_LEFT_WATER_RIGHT;
                }
                else if (!isLeftLand && hasLeftNeighbor)
                {
                    // Слева вода - ставим берег с водой слева
                    return TileTypes.SHORE_RIGHT_WATER_LEFT;
                }
                else
                {
                    // Иначе просто земля
                    return TileTypes.EARTH;
                }
            }
            else
            {
                // Текущая клетка - вода
                if (isRightLand && hasRightNeighbor)
                {
                    // Справа земля - ставим берег с водой слева
                    return TileTypes.SHORE_RIGHT_WATER_LEFT;
                }
                else if (isLeftLand && hasLeftNeighbor)
                {
                    // Слева земля - ставим берег с водой справа
                    return TileTypes.SHORE_LEFT_WATER_RIGHT;
                }
                else
                {
                    // Иначе просто вода
                    return TileTypes.WATER;
                }
            }
        }

        // Исправление возможных несоответствий на стыках тайлов
        private void FixInconsistencies()
        {
            for (int y = 0; y < settings.Height; y++)
            {
                for (int x = 0; x < settings.Width; x++)
                {
                    int tileId = grid[x, y].CollapsedState.Value;
                    
                    // Проверяем соседей и исправляем несоответствия
                    foreach (var (nx, ny, direction) in GetNeighbors(x, y))
                    {
                        if (!IsValidPosition(nx, ny)) continue;
                        
                        int neighborId = grid[nx, ny].CollapsedState.Value;
                        string oppositeDir = GetOppositeDirection(direction);
                        
                        // Проверяем соответствие поверхностей
                        var mySurface = TileTypes.GetSurfaceType(tileId, direction);
                        var neighborSurface = TileTypes.GetSurfaceType(neighborId, oppositeDir);
                        
                        if (mySurface != neighborSurface)
                        {
                            // Нашли несоответствие, решаем случайным образом
                            if (random.NextDouble() < 0.5)
                            {
                                // 50% шанс исправить текущую клетку
                                int newTileId = GetCompatibleTile(direction, neighborSurface);
                                grid[x, y] = new Cell(settings.Tiles.Count);
                                grid[x, y].Collapse(newTileId);
                            }
                            else
                            {
                                // 50% шанс исправить соседа
                                int newNeighborId = GetCompatibleTile(oppositeDir, mySurface);
                                grid[nx, ny] = new Cell(settings.Tiles.Count);
                                grid[nx, ny].Collapse(newNeighborId);
                            }
                        }
                    }
                }
            }
        }

        // Получить совместимый тайл для данного направления и поверхности
        private int GetCompatibleTile(string direction, SurfaceType targetSurface)
        {
            if (direction == "left" || direction == "right")
            {
                if (targetSurface == SurfaceType.Water)
                {
                    // Нужна вода в этом направлении
                    return direction == "left" ? TileTypes.SHORE_LEFT_WATER_RIGHT : TileTypes.SHORE_RIGHT_WATER_LEFT;
                }
                else
                {
                    // Нужна земля в этом направлении
                    return direction == "left" ? TileTypes.SHORE_RIGHT_WATER_LEFT : TileTypes.SHORE_LEFT_WATER_RIGHT;
                }
            }
            else
            {
                // Для верха и низа используем простые типы
                return targetSurface == SurfaceType.Water ? TileTypes.WATER : TileTypes.EARTH;
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