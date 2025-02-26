using WFC.Models;

namespace WFC.Services
{
    public class DefaultWFCService : IWFCService
    {
        private Random random = new Random();
        private Cell[,] grid;
        private WFCSettings settings;
        private float totalCells;
        private int collapsedCells;
        private int maxBacktrackAttempts = 100;

        private Stack<(int x, int y, HashSet<int> previousStates)> history =
            new Stack<(int x, int y, HashSet<int> previousStates)>();

        public event EventHandler<WFCProgressEventArgs> ProgressChanged;

        public async Task<WFCResult> GenerateAsync(WFCSettings settings, CancellationToken token = default)
        {
            // Обновляем seed случайных чисел для уникальной генерации
            random = new Random(Guid.NewGuid().GetHashCode());
            
            this.settings = settings;
            InitializeGrid();
            totalCells = settings.Width * settings.Height;
            collapsedCells = 0;
            history.Clear();

            // Устанавливаем разумные ограничения на количество попыток бэктрекинга
            maxBacktrackAttempts = Math.Max(80, settings.Width * settings.Height * 3 / 2);

            try
            {
                // Запускаем алгоритм в фоновом потоке
                return await Task.Run(async () => 
                {
                    // Инициализируем с полностью случайными начальными точками
                    await InitializeRandomly(token);
                    UpdateProgress($"Initialization complete, starting WFC algorithm");

                    int backtrackAttempts = 0;
                    int consecutiveBacktracks = 0;
                    int totalIterations = 0;
                    
                    // Используем разумное ограничение для предотвращения зависаний
                    int maxIterations = settings.Width * settings.Height * 10;

                    while (!IsGridFullyCollapsed() && 
                           backtrackAttempts < maxBacktrackAttempts &&
                           totalIterations < maxIterations)
                    {
                        totalIterations++;
                        
                        // Периодически проверяем отмену
                        if (totalIterations % 20 == 0)
                            token.ThrowIfCancellationRequested();

                        // Находим клетку для схлопывания - новый метод с большей случайностью
                        var (x, y) = FindCellToCollapseWithRandomness();
                        
                        if (x == -1 || y == -1)
                        {
                            if (collapsedCells < totalCells)
                            {
                                // Пробуем бэктрекинг
                                UpdateProgress($"Backtracking (attempt {backtrackAttempts + 1})");
                                if (!AdvancedBacktrack()) // Используем улучшенный бэктрекинг
                                {
                                    // Если слишком много клеток уже схлопнуто, принимаем частичный результат
                                    if (collapsedCells > totalCells * 0.85)
                                    {
                                        UpdateProgress("Accepting partial solution with good coverage");
                                        break;
                                    }
                                    else
                                    {
                                        UpdateProgress("Backtracking failed");
                                        return new WFCResult { Success = false, ErrorMessage = "Backtracking failed" };
                                    }
                                }

                                backtrackAttempts++;
                                consecutiveBacktracks++;

                                // Сбрасываем случайные клетки при множественных бэктреках подряд
                                if (consecutiveBacktracks > 4)
                                {
                                    ResetRandomCells(Math.Min(8, (int)(settings.Width * settings.Height * 0.08)));
                                    consecutiveBacktracks = 0;
                                }
                            }
                            else
                            {
                                UpdateProgress("Grid fully collapsed");
                                break;
                            }
                        }
                        else
                        {
                            // Пробуем схлопнуть клетку с добавленной случайностью
                            if (!await TryCollapseWithRandomness(x, y, token))
                            {
                                UpdateProgress($"Failed to collapse cell [{x},{y}], performing backtracking");
                                if (!AdvancedBacktrack())
                                {
                                    if (collapsedCells > totalCells * 0.85)
                                    {
                                        UpdateProgress("Accepting partial solution");
                                        break; 
                                    }
                                    else
                                    {
                                        UpdateProgress("Backtracking failed");
                                        return new WFCResult { Success = false, ErrorMessage = "Backtracking failed" };
                                    }
                                }

                                backtrackAttempts++;
                                consecutiveBacktracks++;
                            }
                            else
                            {
                                // Если успешно схлопнули, сбрасываем счетчик бэктреков
                                consecutiveBacktracks = 0;
                            }
                        }

                        // Обновляем прогресс каждые N итераций
                        if (totalIterations % 20 == 0)
                        {
                            UpdateProgress($"Progress: {collapsedCells}/{totalCells} cells ({(collapsedCells * 100 / totalCells):F1}%)");
                            await Task.Delay(1); // Даем UI время обновиться
                        }
                    }

                    // Проверяем лимиты
                    if (totalIterations >= maxIterations)
                    {
                        // Принимаем частичный результат если достаточное покрытие
                        if (collapsedCells > totalCells * 0.85)
                        {
                            UpdateProgress("Accepting partial result (iteration limit)");
                            return new WFCResult
                            {
                                Success = true,
                                Grid = GetResultGrid(),
                                ErrorMessage = null
                            };
                        }
                        else
                        {
                            return new WFCResult
                            {
                                Success = false,
                                ErrorMessage = $"Max iterations exceeded ({maxIterations})"
                            };
                        }
                    }

                    if (backtrackAttempts >= maxBacktrackAttempts)
                    {
                        // Принимаем частичный результат если достаточное покрытие
                        if (collapsedCells > totalCells * 0.85)
                        {
                            UpdateProgress("Accepting partial result (backtrack limit)");
                            return new WFCResult
                            {
                                Success = true,
                                Grid = GetResultGrid(),
                                ErrorMessage = null
                            };
                        }
                        else
                        {
                            return new WFCResult
                            {
                                Success = false,
                                ErrorMessage = $"Max backtracking attempts reached"
                            };
                        }
                    }

                    // Проверяем целостность результата
                    bool isValid = ValidateResult();
                    if (!isValid)
                    {
                        return new WFCResult
                        {
                            Success = false,
                            ErrorMessage = "Generated invalid map with inconsistent water/land boundaries"
                        };
                    }

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

        // Полностью случайная инициализация без предсказуемых паттернов
        private async Task InitializeRandomly(CancellationToken token)
        {
            // Используем переменное количество сидов (от 5% до 15% клеток)
            int seedPercentage = random.Next(5, 16);
            int seedPoints = Math.Max(3, (int)(settings.Width * settings.Height * seedPercentage / 100));
            
            // Создаем список всех возможных позиций для равномерного распределения
            var positions = new List<(int x, int y)>();
            for (int x = 0; x < settings.Width; x++)
            {
                for (int y = 0; y < settings.Height; y++)
                {
                    positions.Add((x, y));
                }
            }
            
            // Перемешиваем список позиций
            Shuffle(positions);
            
            // Берем первые N позиций для сидов
            for (int i = 0; i < Math.Min(seedPoints, positions.Count); i++)
            {
                token.ThrowIfCancellationRequested();
                
                var (x, y) = positions[i];
                
                // Выбираем случайный тип тайла с динамическими весами
                int tileType = GetRandomTileType(x, y);
                
                // Схлопываем клетку
                grid[x, y].Collapse(tileType);
                collapsedCells++;
                
                // Распространяем ограничения каждые несколько сидов
                if (i % 3 == 0)
                {
                    await PropagateConstraints(x, y, token);
                    
                    // Короткая задержка для отзывчивости UI
                    if (i % 9 == 0)
                        await Task.Delay(1);
                }
            }
        }

        // Динамические веса для типов тайлов, зависящие от позиции
        private int GetRandomTileType(int x, int y)
        {
            // Добавляем разнообразие в распределение тайлов в зависимости от их позиции
            double landBias = 0.5; // Нейтральное значение по умолчанию
            
            // Никаких паттернов по краям
            if (IsEdgeCell(x, y))
            {
                // На краях полностью случайный выбор типа
                double r = random.NextDouble();
                if (r < 0.45) return TileTypes.EARTH;
                if (r < 0.9) return TileTypes.WATER;
                if (r < 0.95) return TileTypes.SHORE_LEFT_WATER_RIGHT;
                return TileTypes.SHORE_RIGHT_WATER_LEFT;
            }
            else
            {
                // Используем перлин-подобный шум для непредсказуемого распределения суши/воды
                double noiseValue = Math.Sin(x * 0.73 + y * 0.4) * Math.Cos(y * 0.87 + x * 0.26);
                noiseValue = (noiseValue + 1) / 2; // Нормализуем от 0 до 1
                
                // Добавляем случайность
                noiseValue = (noiseValue * 0.7) + (random.NextDouble() * 0.3);
                
                // Определяем тип на основе шума и случайности
                if (noiseValue < 0.45) return TileTypes.EARTH;
                if (noiseValue < 0.9) return TileTypes.WATER;
                if (noiseValue < 0.95) return TileTypes.SHORE_LEFT_WATER_RIGHT;
                return TileTypes.SHORE_RIGHT_WATER_LEFT;
            }
        }

        // Проверка, является ли клетка краевой
        private bool IsEdgeCell(int x, int y)
        {
            return x == 0 || y == 0 || x == settings.Width - 1 || y == settings.Height - 1;
        }

        // Перемешивание списка (алгоритм Фишера-Йейтса)
        private void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        // Сброс случайных клеток для выхода из тупика
        private void ResetRandomCells(int count)
        {
            // Создаем список клеток для возможного сброса
            var cells = new List<(int x, int y)>();
            
            for (int x = 0; x < settings.Width; x++)
            {
                for (int y = 0; y < settings.Height; y++)
                {
                    if (grid[x, y].Collapsed)
                    {
                        cells.Add((x, y));
                    }
                }
            }
            
            // Перемешиваем список
            Shuffle(cells);
            
            // Сбрасываем первые count клеток
            int resetCount = Math.Min(count, cells.Count);
            for (int i = 0; i < resetCount; i++)
            {
                var (x, y) = cells[i];
                grid[x, y] = new Cell(settings.Tiles.Count);
                collapsedCells--;
            }

            UpdateProgress($"Reset {resetCount} random cells to avoid deadlock");
        }

        // Улучшенный поиск клетки для схлопывания с дополнительной случайностью
        private (int x, int y) FindCellToCollapseWithRandomness()
        {
            // Вводим случайность в выбор клетки
            double randomFactor = random.NextDouble();
            
            // С некоторой вероятностью выбираем полностью случайную клетку
            if (randomFactor < 0.15) // 15% шанс на полную случайность
            {
                var candidates = new List<(int x, int y)>();
                
                for (int x = 0; x < settings.Width; x++)
                {
                    for (int y = 0; y < settings.Height; y++)
                    {
                        if (!grid[x, y].Collapsed)
                        {
                            candidates.Add((x, y));
                        }
                    }
                }
                
                if (candidates.Count > 0)
                {
                    return candidates[random.Next(candidates.Count)];
                }
            }
            
            // Иначе используем стандартный подход, но с приоритетом клеток по краям
            float minEntropy = float.MaxValue;
            var cellsWithMinEntropy = new List<(int x, int y, float entropy, bool isEdge)>();
            
            for (int x = 0; x < settings.Width; x++)
            {
                for (int y = 0; y < settings.Height; y++)
                {
                    if (grid[x, y].Collapsed) continue;
                    
                    var cell = grid[x, y];
                    bool hasCollapsedNeighbor = false;
                    bool isEdge = IsEdgeCell(x, y);
                    
                    // Проверяем наличие схлопнутых соседей
                    foreach (var (nx, ny, _) in GetNeighbors(x, y))
                    {
                        if (IsValidPosition(nx, ny) && grid[nx, ny].Collapsed)
                        {
                            hasCollapsedNeighbor = true;
                            break;
                        }
                    }
                    
                    // Если клетка на краю, даем ей преимущество
                    float entropyValue = cell.Entropy;
                    if (isEdge)
                    {
                        // Снижаем энтропию для краевых клеток, чтобы они выбирались чаще
                        entropyValue *= 0.85f;
                    }
                    
                    if (hasCollapsedNeighbor)
                    {
                        // Даем небольшое случайное изменение энтропии
                        float randomOffset = (float)(random.NextDouble() * 0.1 - 0.05);
                        entropyValue += randomOffset;
                        
                        if (entropyValue < minEntropy)
                        {
                            minEntropy = entropyValue;
                            cellsWithMinEntropy.Clear();
                            cellsWithMinEntropy.Add((x, y, entropyValue, isEdge));
                        }
                        else if (Math.Abs(entropyValue - minEntropy) < 0.1f)
                        {
                            cellsWithMinEntropy.Add((x, y, entropyValue, isEdge));
                        }
                    }
                }
            }
            
            // Если нашли клетки с соседями, выбираем из них
            if (cellsWithMinEntropy.Count > 0)
            {
                // Даем предпочтение краевым клеткам
                var edgeCells = cellsWithMinEntropy.Where(c => c.isEdge).ToList();
                if (edgeCells.Count > 0 && random.NextDouble() < 0.7) // 70% шанс выбрать краевую клетку
                {
                    var selected = edgeCells[random.Next(edgeCells.Count)];
                    return (selected.x, selected.y);
                }
                else
                {
                    var selected = cellsWithMinEntropy[random.Next(cellsWithMinEntropy.Count)];
                    return (selected.x, selected.y);
                }
            }
            
            // Если нет клеток с соседями, ищем любую клетку
            cellsWithMinEntropy.Clear();
            minEntropy = float.MaxValue;
            
            for (int x = 0; x < settings.Width; x++)
            {
                for (int y = 0; y < settings.Height; y++)
                {
                    if (grid[x, y].Collapsed) continue;
                    
                    var cell = grid[x, y];
                    bool isEdge = IsEdgeCell(x, y);
                    
                    // Небольшая случайность в энтропии
                    float entropyValue = cell.Entropy * (float)(0.95 + random.NextDouble() * 0.1);
                    
                    // Приоритет краевым клеткам
                    if (isEdge)
                    {
                        entropyValue *= 0.85f;
                    }
                    
                    if (entropyValue < minEntropy)
                    {
                        minEntropy = entropyValue;
                        cellsWithMinEntropy.Clear();
                        cellsWithMinEntropy.Add((x, y, entropyValue, isEdge));
                    }
                    else if (Math.Abs(entropyValue - minEntropy) < 0.1f)
                    {
                        cellsWithMinEntropy.Add((x, y, entropyValue, isEdge));
                    }
                }
            }
            
            if (cellsWithMinEntropy.Count > 0)
            {
                // Даем предпочтение краевым клеткам
                var edgeCells = cellsWithMinEntropy.Where(c => c.isEdge).ToList();
                if (edgeCells.Count > 0 && random.NextDouble() < 0.7) // 70% шанс выбрать краевую клетку
                {
                    var selected = edgeCells[random.Next(edgeCells.Count)];
                    return (selected.x, selected.y);
                }
                else
                {
                    var selected = cellsWithMinEntropy[random.Next(cellsWithMinEntropy.Count)];
                    return (selected.x, selected.y);
                }
            }
            
            return (-1, -1); // Нет клеток для схлопывания
        }

        // Улучшенный метод схлопывания клетки с дополнительной случайностью
        private async Task<bool> TryCollapseWithRandomness(int x, int y, CancellationToken token)
        {
            var cell = grid[x, y];
            if (cell.Collapsed) return true;

            // Получаем возможные состояния
            var possibleStates = GetPossibleStatesWithWeights(x, y);
            
            if (possibleStates.Count == 0)
            {
                return false;
            }
            
            // Сохраняем для бэктрекинга
            var statesToTry = new HashSet<int>(possibleStates.Select(p => p.state));
            
            // Выбираем состояние
            int selectedState;
            
            // Добавляем случайность в выбор состояния
            if (possibleStates.Count > 1)
            {
                // Добавляем случайные вариации в веса
                var randomizedWeights = possibleStates.Select(p => 
                {
                    float randomFactor = (float)(0.8 + random.NextDouble() * 0.4);
                    return (p.state, p.weight * randomFactor);
                }).ToList();
                
                float totalWeight = randomizedWeights.Sum(p => p.Item2);
                
                if (totalWeight > 0)
                {
                    float randomValue = (float)(random.NextDouble() * totalWeight);
                    float cumulativeWeight = 0;
                    selectedState = randomizedWeights[0].Item1; // По умолчанию
                    
                    foreach (var (state, weight) in randomizedWeights)
                    {
                        cumulativeWeight += weight;
                        if (randomValue <= cumulativeWeight)
                        {
                            selectedState = state;
                            break;
                        }
                    }
                }
                else
                {
                    // Равномерный выбор если все веса нулевые
                    selectedState = statesToTry.ElementAt(random.Next(statesToTry.Count));
                }
            }
            else
            {
                // Только одно возможное состояние
                selectedState = possibleStates[0].state;
            }
            
            // Удаляем выбранное состояние из списка для бэктрекинга
            statesToTry.Remove(selectedState);
            
            // Сохраняем оставшиеся состояния для бэктрекинга
            if (statesToTry.Count > 0)
            {
                history.Push((x, y, statesToTry));
            }
            
            // Схлопываем клетку
            cell.Collapse(selectedState);
            collapsedCells++;
            
            // Распространяем ограничения
            return await PropagateConstraints(x, y, token);
        }

        // Улучшенный бэктрекинг с большей случайностью
        private bool AdvancedBacktrack()
        {
            if (history.Count == 0)
            {
                return false;
            }
            
            const int maxResetDepth = 3; // Максимальная глубина сброса
            int currentResetDepth = 0;
            
            while (currentResetDepth < maxResetDepth)
            {
                if (history.Count == 0)
                {
                    // Если история пуста, сбрасываем случайные клетки
                    ResetRandomCells(5);
                    return true;
                }
                
                var (x, y, remainingStates) = history.Pop();
                
                // Проверяем, что клетка все еще релевантна
                if (!grid[x, y].Collapsed)
                {
                    // Если клетка уже сброшена, пропускаем
                    continue;
                }
                
                if (remainingStates.Count == 0)
                {
                    // Сбрасываем клетку полностью
                    grid[x, y] = new Cell(settings.Tiles.Count);
                    collapsedCells--;
                    
                    // Сбрасываем и соседей с некоторой вероятностью
                    if (currentResetDepth > 0)
                    {
                        foreach (var (nx, ny, _) in GetNeighbors(x, y))
                        {
                            if (IsValidPosition(nx, ny) && grid[nx, ny].Collapsed && random.NextDouble() < 0.3)
                            {
                                grid[nx, ny] = new Cell(settings.Tiles.Count);
                                collapsedCells--;
                            }
                        }
                    }
                    
                    currentResetDepth++;
                    continue;
                }
                
                // Выбираем новое состояние с приоритетом по весам
                List<(int state, float weight)> stateWeights = GetStateWeights(x, y, remainingStates);
                
                int stateToTry;
                float totalWeight = stateWeights.Sum(s => s.weight);
                
                if (totalWeight > 0)
                {
                    // Используем взвешенный выбор
                    float randomValue = (float)(random.NextDouble() * totalWeight);
                    float cumulativeWeight = 0;
                    stateToTry = stateWeights[0].state; // По умолчанию
                    
                    foreach (var (state, weight) in stateWeights)
                    {
                        cumulativeWeight += weight;
                        if (randomValue <= cumulativeWeight)
                        {
                            stateToTry = state;
                            break;
                        }
                    }
                }
                else
                {
                    // Равномерный выбор при нулевых весах
                    stateToTry = remainingStates.ElementAt(random.Next(remainingStates.Count));
                }
                
                // Удаляем выбранное состояние
                remainingStates.Remove(stateToTry);
                
                // Сохраняем оставшиеся состояния
                if (remainingStates.Count > 0)
                {
                    history.Push((x, y, remainingStates));
                }
                
                // Схлопываем клетку в новое состояние
                grid[x, y].Collapse(stateToTry);
                
                return true;
            }
            
            // Если достигли предела глубины сброса, сбрасываем случайные клетки
            ResetRandomCells(8);
            return true;
        }

        // Вычисление весов состояний для бэктрекинга
        private List<(int state, float weight)> GetStateWeights(int x, int y, HashSet<int> states)
        {
            var result = new List<(int state, float weight)>();
            
            foreach (var state in states)
            {
                float weight = 1.0f; // Начальный вес
                
                // Проверяем совместимость с соседями
                foreach (var (nx, ny, direction) in GetNeighbors(x, y))
                {
                    if (!IsValidPosition(nx, ny)) continue;
                    
                    var neighbor = grid[nx, ny];
                    var oppDirection = GetOppositeDirection(direction);
                    
                    if (neighbor.Collapsed)
                    {
                        int neighborState = neighbor.CollapsedState.Value;
                        
                        // Проверяем типы поверхностей
                        var mySurface = TileTypes.GetSurfaceType(state, direction);
                        var neighborSurface = TileTypes.GetSurfaceType(neighborState, oppDirection);
                        
                        if (mySurface == neighborSurface)
                        {
                            weight += 1.0f;
                        }
                        else
                        {
                            weight -= 1.0f;
                        }
                    }
                }
                
                // Добавляем случайность
                weight *= (float)(0.8 + random.NextDouble() * 0.4);
                
                // Предотвращаем отрицательные веса
                weight = Math.Max(0.1f, weight);
                
                result.Add((state, weight));
            }
            
            return result;
        }

        // Получение взвешенных возможных состояний
        private List<(int state, float weight)> GetPossibleStatesWithWeights(int x, int y)
        {
            var cell = grid[x, y];
            var result = new List<(int state, float weight)>();
            
            if (cell.Collapsed)
            {
                result.Add((cell.CollapsedState.Value, 1.0f));
                return result;
            }
            
            // Добавляем все возможные состояния с базовыми весами
            foreach (var state in cell.PossibleStates)
            {
                result.Add((state, 0.1f));
            }
            
            // Улучшаем веса на основе ограничений от соседей
            foreach (var (nx, ny, direction) in GetNeighbors(x, y))
            {
                if (!IsValidPosition(nx, ny)) continue;
                
                var neighbor = grid[nx, ny];
                var oppositeDirection = GetOppositeDirection(direction);
                
                if (neighbor.Collapsed)
                {
                    int neighborState = neighbor.CollapsedState.Value;
                    var key = (neighborState, oppositeDirection);
                    
                    if (settings.Rules.TryGetValue(key, out var allowedStates))
                    {
                        foreach (var tuple in allowedStates)
                        {
                            int allowedState = tuple.Item1;
                            float weight = tuple.Item2;
                            
                            // Проверяем поверхность для согласованности
                            var neighborSurface = TileTypes.GetSurfaceType(neighborState, oppositeDirection);
                            var stateSurface = TileTypes.GetSurfaceType(allowedState, direction);
                            
                            if (neighborSurface == stateSurface && cell.PossibleStates.Contains(allowedState))
                            {
                                // Увеличиваем вес для этого состояния
                                for (int i = 0; i < result.Count; i++)
                                {
                                    if (result[i].state == allowedState)
                                    {
                                        result[i] = (allowedState, result[i].weight + weight);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // Если все состояния отфильтрованы, возвращаем все возможные с низкими весами
            if (result.Count == 0 && cell.PossibleStates.Count > 0)
            {
                foreach (var state in cell.PossibleStates)
                {
                    result.Add((state, 0.1f));
                }
            }
            
            return result;
        }

        // Распространение ограничений после схлопывания клетки
        private async Task<bool> PropagateConstraints(int startX, int startY, CancellationToken token)
        {
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((startX, startY));
            var processedCells = new HashSet<(int x, int y)>();
            
            while (queue.Count > 0)
            {
                // Периодически проверяем отмену
                if (processedCells.Count % 20 == 0)
                {
                    token.ThrowIfCancellationRequested();
                    
                    // Короткая задержка для UI
                    if (processedCells.Count % 60 == 0)
                        await Task.Delay(1);
                }
                
                var (x, y) = queue.Dequeue();
                if (!processedCells.Add((x, y))) continue;
                
                var cell = grid[x, y];
                
                foreach (var (nx, ny, direction) in GetNeighbors(x, y))
                {
                    if (!IsValidPosition(nx, ny)) continue;
                    
                    var neighbor = grid[nx, ny];
                    if (neighbor.PossibleStates.Count == 0) return false;
                    
                    // Обновляем ограничения
                    bool changed = UpdateConstraints(cell, neighbor, direction);
                    
                    if (changed)
                    {
                        if (neighbor.PossibleStates.Count == 0)
                        {
                            return false; // Противоречие
                        }
                        
                        if (neighbor.PossibleStates.Count == 1 && !neighbor.Collapsed)
                        {
                            neighbor.Collapse(neighbor.PossibleStates.First());
                            collapsedCells++;
                        }
                        
                        queue.Enqueue((nx, ny));
                    }
                }
                
                // Ограничиваем размер очереди
                if (queue.Count > settings.Width * settings.Height * 2)
                {
                    break;
                }
            }
            
            return true;
        }

        // Обновление ограничений между клетками
        private bool UpdateConstraints(Cell cell, Cell neighbor, string direction)
        {
            bool changed = false;
            var oppDirection = GetOppositeDirection(direction);
            
            if (cell.Collapsed)
            {
                int cellState = cell.CollapsedState.Value;
                var cellSurface = TileTypes.GetSurfaceType(cellState, direction);
                
                // Получаем допустимые состояния из правил
                var key = (cellState, direction);
                var validStates = new HashSet<int>();
                
                if (settings.Rules.TryGetValue(key, out var allowedStates))
                {
                    foreach (var tuple in allowedStates)
                    {
                        if (tuple.Item2 > 0)
                        {
                            validStates.Add(tuple.Item1);
                        }
                    }
                }
                
                // Применяем ограничения к соседу
                foreach (var state in neighbor.PossibleStates.ToList())
                {
                    bool isValid = validStates.Contains(state);
                    
                    if (isValid)
                    {
                        // Проверка типа поверхности
                        var neighborSurface = TileTypes.GetSurfaceType(state, oppDirection);
                        if (cellSurface != neighborSurface)
                        {
                            isValid = false;
                        }
                    }
                    
                    if (!isValid && neighbor.RemoveState(state))
                    {
                        changed = true;
                    }
                }
            }
            else
            {
                // Клетка не схлопнута, проверяем ограничения от всех возможных состояний
                foreach (var neighborState in neighbor.PossibleStates.ToList())
                {
                    bool isCompatible = false;
                    
                    foreach (var cellState in cell.PossibleStates)
                    {
                        var key = (cellState, direction);
                        if (!settings.Rules.TryGetValue(key, out var allowedStates))
                            continue;
                        
                        // Fixed: Using tuple positions instead of property names
                        if (allowedStates.Any(p => p.Item1 == neighborState && p.Item2 > 0))
                        {
                            // Проверка типа поверхности
                            var cellSurface = TileTypes.GetSurfaceType(cellState, direction);
                            var neighborSurface = TileTypes.GetSurfaceType(neighborState, oppDirection);
                            
                            if (cellSurface == neighborSurface)
                            {
                                isCompatible = true;
                                break;
                            }
                        }
                    }
                    
                    if (!isCompatible && neighbor.RemoveState(neighborState))
                    {
                        changed = true;
                    }
                }
            }
            
            return changed;
        }

        // Проверка результата с допустимостью небольших несоответствий
        private bool ValidateResult()
        {
            int inconsistencies = 0;
            int totalChecks = 0;
            int maxInconsistencies = (int)(settings.Width * settings.Height * 0.05); // 5% погрешность
            
            for (int x = 0; x < settings.Width; x++)
            {
                for (int y = 0; y < settings.Height; y++)
                {
                    if (!grid[x, y].Collapsed) continue;
                    
                    int tileId = grid[x, y].CollapsedState.Value;
                    
                    foreach (var (nx, ny, direction) in GetNeighbors(x, y))
                    {
                        if (!IsValidPosition(nx, ny) || !grid[nx, ny].Collapsed) continue;
                        
                        totalChecks++;
                        int neighborId = grid[nx, ny].CollapsedState.Value;
                        string oppositeDir = GetOppositeDirection(direction);
                        
                        var mySurface = TileTypes.GetSurfaceType(tileId, direction);
                        var neighborSurface = TileTypes.GetSurfaceType(neighborId, oppositeDir);
                        
                        if (mySurface != neighborSurface)
                        {
                            inconsistencies++;
                            if (inconsistencies > maxInconsistencies)
                                return false;
                        }
                    }
                }
            }
            
            return inconsistencies <= maxInconsistencies;
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

        // Проверка полного схлопывания сетки
        private bool IsGridFullyCollapsed()
        {
            return collapsedCells >= totalCells;
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
            history.Clear();
            UpdateProgress("Ready");
        }
    }
}