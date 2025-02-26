using WFC.Models;

namespace WFC.Services
{
    public class DefaultWFCService : IWFCService
    {
        private readonly Random random = new Random();
        private Cell[,] grid;
        private WFCSettings settings;
        private float totalCells;
        private int collapsedCells;

        public event EventHandler<WFCProgressEventArgs> ProgressChanged;

        public async Task<WFCResult> GenerateAsync(WFCSettings settings, CancellationToken token = default)
        {
            this.settings = settings;
            InitializeGrid();
            totalCells = settings.Width * settings.Height;
            collapsedCells = 0;

            try
            {
                // Начинаем с левого края - там всегда земля
                for (int y = 0; y < settings.Height; y++)
                {
                    grid[0, y].Collapse(0); // Ставим землю по левому краю
                    collapsedCells++;
                }
        
                UpdateProgress("Starting generation from left edge");

                while (!IsGridFullyCollapsed())
                {
                    token.ThrowIfCancellationRequested();

                    var (x, y) = FindLowestEntropyCell();
                    if (x == -1 || y == -1)
                    {
                        UpdateProgress("No valid cell found");
                        return new WFCResult { Success = false, ErrorMessage = "No valid cell found" };
                    }

                    if (!await CollapseCell(x, y))
                    {
                        UpdateProgress($"Failed to collapse cell at [{x},{y}]");
                        return new WFCResult { Success = false, ErrorMessage = "Failed to collapse cell" };
                    }

                    if (!await PropagateConstraints(x, y, token))
                    {
                        UpdateProgress($"Propagation failed at [{x},{y}]");
                        return new WFCResult { Success = false, ErrorMessage = "Propagation failed" };
                    }
                }

                return new WFCResult 
                { 
                    Success = true, 
                    Grid = GetResultGrid(),
                    ErrorMessage = null 
                };
            }
            catch (OperationCanceledException)
            {
                return new WFCResult { Success = false, ErrorMessage = "Operation cancelled" };
            }
        }
        private (int x, int y) FindLowestEntropyCell()
        {
            var minEntropy = int.MaxValue;
            var cells = new List<(int x, int y)>();

            // Сначала ищем ячейки рядом с уже схлопнутыми
            for (int x = 0; x < settings.Width; x++)
            {
                for (int y = 0; y < settings.Height; y++)
                {
                    if (grid[x, y].Collapsed) continue;

                    // Проверяем, есть ли рядом схлопнутые ячейки
                    bool hasCollapsedNeighbor = GetNeighbors(x, y)
                        .Any(n => IsValidPosition(n.x, n.y) && grid[n.x, n.y].Collapsed);

                    if (!hasCollapsedNeighbor) continue;

                    var entropy = grid[x, y].PossibleStates.Count;
                    if (entropy == 0) continue;

                    if (entropy < minEntropy)
                    {
                        minEntropy = entropy;
                        cells.Clear();
                        cells.Add((x, y));
                    }
                    else if (entropy == minEntropy)
                    {
                        cells.Add((x, y));
                    }
                }
            }

            // Если не нашли ячейки рядом с схлопнутыми, ищем любые несхлопнутые
            if (cells.Count == 0)
            {
                for (int x = 0; x < settings.Width; x++)
                {
                    for (int y = 0; y < settings.Height; y++)
                    {
                        if (grid[x, y].Collapsed) continue;

                        var entropy = grid[x, y].PossibleStates.Count;
                        if (entropy == 0) continue;

                        if (entropy < minEntropy)
                        {
                            minEntropy = entropy;
                            cells.Clear();
                            cells.Add((x, y));
                        }
                        else if (entropy == minEntropy)
                        {
                            cells.Add((x, y));
                        }
                    }
                }
            }

            if (cells.Count == 0) return (-1, -1);
            return cells[random.Next(cells.Count)];
        }


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


        private async Task<bool> CollapseCell(int x, int y)
        {
            var cell = grid[x, y];
            if (cell.Collapsed) return true;

            var possibleStates = new List<(int state, float weight)>();

            // Собираем все возможные состояния и их веса
            float totalWeight;
            foreach (var state in cell.PossibleStates)
            {
                totalWeight = 0f;
                int validNeighbors = 0;

                foreach (var (nx, ny, direction) in GetNeighbors(x, y))
                {
                    if (!IsValidPosition(nx, ny)) continue;

                    var key = (state, direction);
                    if (settings.Rules.TryGetValue(key, out var rules))
                    {
                        totalWeight += rules.Sum(r => r.weight);
                        validNeighbors++;
                    }
                }

                if (validNeighbors > 0)
                {
                    possibleStates.Add((state, totalWeight / validNeighbors));
                }
            }

            if (possibleStates.Count == 0) return false;

            // Выбираем состояние с учетом весов
            totalWeight = possibleStates.Sum(p => p.weight);
            float randomValue = (float)(random.NextDouble() * totalWeight);
            float currentWeight = 0;

            foreach (var (state, weight) in possibleStates)
            {
                currentWeight += weight;
                if (randomValue <= currentWeight)
                {
                    cell.Collapse(state);
                    collapsedCells++;
                    return true;
                }
            }

            // Если почему-то не выбрали состояние, берем первое
            cell.Collapse(possibleStates[0].state);
            collapsedCells++;
            return true;
        }

        private async Task<bool> PropagateConstraints(int startX, int startY, CancellationToken token)
        {
            var stack = new Stack<(int x, int y)>();
            stack.Push((startX, startY));
            var processedCells = new HashSet<(int x, int y)>();

            while (stack.Count > 0)
            {
                token.ThrowIfCancellationRequested();

                var (x, y) = stack.Pop();
                if (!processedCells.Add((x, y))) continue;

                var cell = grid[x, y];
                if (!cell.Collapsed) continue;

                foreach (var (nx, ny, direction) in GetNeighbors(x, y))
                {
                    if (!IsValidPosition(nx, ny)) continue;

                    var neighbor = grid[nx, ny];
                    if (neighbor.Collapsed) continue;

                    if (UpdateConstraints(cell, neighbor, direction))
                    {
                        if (neighbor.PossibleStates.Count == 0)
                        {
                            return false;
                        }

                        stack.Push((nx, ny));
                    }
                }
            }

            return true;
        }

        private HashSet<int> GetValidStates(Cell cell, string direction)
        {
            var validStates = new HashSet<int>();
            var currentState = cell.CollapsedState.Value;

            // Проверяем правила для текущего состояния и направления
            var key = (currentState, direction);
            if (settings.Rules.TryGetValue(key, out var possibleStates))
            {
                foreach (var (state, weight) in possibleStates)
                {
                    if (weight > 0)
                    {
                        validStates.Add(state);
                    }
                }
            }

            return validStates;
        }

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

// Добавьте этот метод для проверки позиции
        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < settings.Width && y >= 0 && y < settings.Height;
        }

// Обновленный метод получения соседей
        private List<(int x, int y, string direction)> GetNeighbors(int x, int y)
        {
            return new List<(int x, int y, string direction)>
            {
                (x - 1, y, "right"), // Левый сосед (смотрит вправо)
                (x + 1, y, "left"), // Правый сосед (смотрит влево)
                (x, y - 1, "down"), // Верхний сосед (смотрит вниз)
                (x, y + 1, "up") // Нижний сосед (смотрит вверх)
            };
        }


        private bool UpdateConstraints(Cell cell, Cell neighbor, string direction)
        {
            if (!cell.Collapsed) return false;

            var validStates = GetValidStates(cell, direction);
            bool changed = false;

            foreach (var state in neighbor.PossibleStates.ToList())
            {
                if (!validStates.Contains(state))
                {
                    changed |= neighbor.RemoveState(state);
                }
            }

            return changed;
        }

        private bool IsGridFullyCollapsed()
        {
            return collapsedCells == totalCells;
        }

        private Tile[,] GetResultGrid()
        {
            var result = new Tile[settings.Width, settings.Height];
            for (int x = 0; x < settings.Width; x++)
            {
                for (int y = 0; y < settings.Height; y++)
                {
                    var cell = grid[x, y];
                    if (cell.Collapsed)
                    {
                        result[x, y] = settings.Tiles[cell.CollapsedState.Value];
                    }
                }
            }

            return result;
        }

        private void UpdateProgress(string status)
        {
            var progress = (collapsedCells / totalCells) * 100;
            ProgressChanged?.Invoke(this, new WFCProgressEventArgs(progress, status));
        }

        public void Reset()
        {
            grid = null;
            settings = null;
            collapsedCells = 0;
            UpdateProgress("Ready");
        }
    }
}