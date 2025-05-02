using WFC.Models;
using WFC.Plugins;

namespace WFC.Services;

/// <summary>
/// Default implementation of the WFC algorithm
/// </summary>
public class DefaultWFCService : IWFCService
{
    private readonly PluginManager _pluginManager;
    private Random _random;
    private Cell[,] _grid;
    private WFCSettings _settings;
    private GenerationContext _context;
    private float _totalCells;
    private int _collapsedCells;

    public event EventHandler<WFCProgressEventArgs> ProgressChanged;

    public DefaultWFCService(PluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    /// <summary>
    /// Generate a new WFC grid
    /// </summary>
    public async Task<WFCResult> GenerateAsync(WFCSettings settings, CancellationToken token = default)
    {
        try 
        {
            // Create random number generator
            _random = settings.Seed.HasValue 
                ? new Random(settings.Seed.Value) 
                : new Random(Guid.NewGuid().GetHashCode());

            _settings = settings;
            InitializeGrid();
            _totalCells = settings.Width * settings.Height;
            _collapsedCells = 0;

            // Create generation context and add to settings for plugin access
            _context = new GenerationContext(_settings, _random, _grid);
            
            // Инициализируем PluginSettings, если он null
            if (settings.PluginSettings == null)
            {
                settings.PluginSettings = new Dictionary<string, object>();
            }
            
            settings.PluginSettings["context"] = _context;

            // Выполнение операций в Task.Run для асинхронного выполнения
            return await Task.Run(() =>
            {
                try
                {
                    UpdateProgress("Initializing generation...");

                    // Notify plugins before generation
                    NotifyPluginsBeforeGeneration();

                    // Generate the grid
                    UpdateProgress("Running WFC algorithm...");
                    RunWaveFunction(token);

                    // Get result grid
                    var resultGrid = GetResultGrid();
                    
                    // Apply post-processing from plugins
                    UpdateProgress("Applying post-processing...");
                    resultGrid = ApplyPostProcessing(resultGrid);

                    UpdateProgress("Generation completed successfully");

                    return new WFCResult
                    {
                        Success = true,
                        Grid = resultGrid,
                        ErrorMessage = null
                    };
                }
                catch (OperationCanceledException)
                {
                    return new WFCResult { Success = false, ErrorMessage = "Operation canceled" };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Generation error: {ex}");
                    return new WFCResult { Success = false, ErrorMessage = $"Error: {ex.Message}" };
                }
            }, token);
        }
        catch (OperationCanceledException)
        {
            return new WFCResult { Success = false, ErrorMessage = "Operation canceled" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Generation error: {ex}");
            return new WFCResult { Success = false, ErrorMessage = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Run the core WFC algorithm
    /// </summary>
    private void RunWaveFunction(CancellationToken token)
    {
        bool complete = false;
        int iterations = 0;
        int maxIterations = _settings.Width * _settings.Height * 2; // Safety limit

        try
        {
            while (!complete && iterations < maxIterations)
            {
                token.ThrowIfCancellationRequested();
                iterations++;

                // 1. Find the cell with the minimum entropy
                var (minX, minY) = FindMinEntropyCell();
                
                // If all cells are collapsed, we're done
                if (minX == -1 || minY == -1)
                {
                    complete = true;
                    continue;
                }

                // 2. Collapse the cell with the minimum entropy
                CollapseCell(minX, minY);

                // 3. Propagate constraints
                PropagateConstraints(minX, minY);

                // 4. Update progress
                if (iterations % 10 == 0 || _collapsedCells == _totalCells)
                {
                    UpdateProgress($"Processing: {_collapsedCells}/{_totalCells} cells ({iterations} iterations)");
                }
            }

            if (iterations >= maxIterations)
            {
                // Handle possible issues by collapsing remaining cells randomly
                HandleUncollapsedCells();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during wave function collapse: {ex.Message}");
            throw; // Re-throw to be handled by the caller
        }
    }

    /// <summary>
    /// Find the cell with the minimum non-zero entropy
    /// </summary>
    private (int x, int y) FindMinEntropyCell()
    {
        float minEntropy = float.MaxValue;
        int minX = -1, minY = -1;

        // Find all uncollapsed cells with minimum entropy
        var candidates = new List<(int x, int y)>();

        for (int y = 0; y < _settings.Height; y++)
        {
            for (int x = 0; x < _settings.Width; x++)
            {
                var cell = _grid[x, y];
                
                // Skip already collapsed cells
                if (cell.Collapsed) continue;
                
                // If this cell has lower entropy, reset candidates
                if (cell.Entropy < minEntropy)
                {
                    minEntropy = cell.Entropy;
                    candidates.Clear();
                    candidates.Add((x, y));
                }
                // If this cell has the same entropy, add to candidates
                else if (Math.Abs(cell.Entropy - minEntropy) < 0.0001f)
                {
                    candidates.Add((x, y));
                }
            }
        }

        // If we have candidates, pick one randomly
        if (candidates.Count > 0)
        {
            var selected = candidates[_random.Next(candidates.Count)];
            minX = selected.x;
            minY = selected.y;
        }

        return (minX, minY);
    }

    /// <summary>
    /// Collapse a cell to a single state
    /// </summary>
    private void CollapseCell(int x, int y)
    {
        var cell = _grid[x, y];
        
        // If already collapsed, nothing to do
        if (cell.Collapsed) return;
        
        // Get possible states
        var possibleStates = new List<int>(cell.PossibleStates);
        
        // Let plugins modify possible states
        possibleStates = GetPluginModifiedStates(x, y, possibleStates).ToList();
        
        // If no states are possible, handle contradiction
        if (possibleStates.Count == 0)
        {
            HandleContradiction(x, y);
            return;
        }
        
        // Select a state based on weighted probabilities
        int selectedState = SelectStateWithWeights(possibleStates, x, y);
        
        // Collapse the cell to the selected state
        cell.Collapse(selectedState);
        _collapsedCells++;
        
        // Notify plugins after collapse
        NotifyPluginsAfterCollapse(x, y, selectedState);
    }

    /// <summary>
    /// Select a state from possible states with weights based on neighbor constraints
    /// </summary>
    private int SelectStateWithWeights(List<int> possibleStates, int x, int y)
    {
        
        // Если нет возможных состояний, вернуть 0 (обычно это базовая плитка травы)
        if (possibleStates.Count == 0)
        {
            Console.WriteLine("Warning: No possible states for cell, using default state 0");
            return 0;
        }
        
        // If only one state is possible, return it
        if (possibleStates.Count == 1)
            return possibleStates[0];
            
        // Calculate weights for each state based on neighbor constraints
        var weights = new Dictionary<int, float>();
        
        // Start with equal weights
        foreach (var state in possibleStates)
        {
            weights[state] = 1.0f;
        }
        
        // Check each direction
        foreach (var (nx, ny, direction) in GetNeighbors(x, y))
        {
            // Skip out of bounds
            if (nx < 0 || nx >= _settings.Width || ny < 0 || ny >= _settings.Height)
                continue;
                
            var neighbor = _grid[nx, ny];
            string oppositeDir = GetOppositeDirection(direction);
            
            // Adjust weights based on neighbor constraints
            foreach (var state in possibleStates)
            {
                float weight = weights[state];
                
                // Get possible connections from rules
                if (_settings.Rules.TryGetValue((state, direction), out var possibleConnections))
                {
                    // Calculate total constraint weight
                    float totalWeight = 0;
                    
                    foreach (var (toState, connectionWeight) in possibleConnections)
                    {
                        // If neighbor allows this state, add its weight
                        if (neighbor.PossibleStates.Contains(toState))
                        {
                            totalWeight += connectionWeight;
                        }
                    }
                    
                    // Adjust this state's weight
                    weights[state] *= (totalWeight > 0 ? totalWeight : 0.1f);
                }
            }
        }
        
        // Normalize weights
        float totalWeights = weights.Values.Sum();
        if (totalWeights <= 0)
        {
            // If all weights are zero, use equal probabilities
            return possibleStates[_random.Next(possibleStates.Count)];
        }
        
        // Select state based on weights
        float random = (float)_random.NextDouble() * totalWeights;
        float cumulative = 0;
        
        foreach (var state in possibleStates)
        {
            cumulative += weights[state];
            if (random < cumulative)
                return state;
        }
        
        // Fallback: return last state
        return possibleStates.Last();
    }

    /// <summary>
    /// Propagate constraints from a cell to its neighbors
    /// </summary>
    private void PropagateConstraints(int startX, int startY)
    {
        // Use a queue for breadth-first constraint propagation
        var queue = new Queue<(int x, int y)>();
        queue.Enqueue((startX, startY));
        
        // Set to track processed cells
        var processed = new HashSet<(int x, int y)>();
        
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            
            // Skip if already processed
            if (!processed.Add((x, y)))
                continue;
                
            var cell = _grid[x, y];
            
            // Get each neighbor
            foreach (var (nx, ny, direction) in GetNeighbors(x, y))
            {
                // Skip out of bounds
                if (nx < 0 || nx >= _settings.Width || ny < 0 || ny >= _settings.Height)
                    continue;
                    
                var neighbor = _grid[nx, ny];
                string oppositeDir = GetOppositeDirection(direction);
                
                // Skip if neighbor is already collapsed
                if (neighbor.Collapsed)
                    continue;
                    
                // Apply constraints from this cell to neighbor
                bool changed = ApplyConstraints(cell, neighbor, direction);
                
                // If neighbor changed, propagate further
                if (changed)
                {
                    queue.Enqueue((nx, ny));
                }
            }
        }
    }

    /// <summary>
    /// Apply constraints from source cell to target cell
    /// </summary>
    private bool ApplyConstraints(Cell source, Cell target, string direction)
    {
        if (!source.Collapsed)
            return false;
            
        int sourceState = source.CollapsedState.Value;
        string oppositeDir = GetOppositeDirection(direction);
        
        // Get allowed connections from source to target
        if (!_settings.Rules.TryGetValue((sourceState, direction), out var possibleConnections))
            return false;
            
        // Create set of allowed states
        var allowedStates = new HashSet<int>();
        foreach (var (toState, _) in possibleConnections)
        {
            allowedStates.Add(toState);
        }
        
        // Constrain target to allowed states
        return target.ConstrainToStates(allowedStates);
    }

    /// <summary>
    /// Handle a contradiction by resetting the cell to allow all tiles
    /// </summary>
    private void HandleContradiction(int x, int y)
    {
        Console.WriteLine($"Contradiction at ({x}, {y}) - Resetting cell");
        
        // Create a new cell with all states
        _grid[x, y] = new Cell(_settings.Tiles.Count);
    }

    /// <summary>
    /// Handle any remaining uncollapsed cells by randomly collapsing them
    /// </summary>
    private void HandleUncollapsedCells()
    {
        for (int y = 0; y < _settings.Height; y++)
        {
            for (int x = 0; x < _settings.Width; x++)
            {
                var cell = _grid[x, y];
                if (!cell.Collapsed)
                {
                    CollapseCell(x, y);
                }
            }
        }
    }

    /// <summary>
    /// Initialize the grid with all possible states
    /// </summary>
    private void InitializeGrid()
    {
        _grid = new Cell[_settings.Width, _settings.Height];
        for (int x = 0; x < _settings.Width; x++)
        {
            for (int y = 0; y < _settings.Height; y++)
            {
                _grid[x, y] = new Cell(_settings.Tiles.Count);
            }
        }
    }

    /// <summary>
    /// Get the opposite direction
    /// </summary>
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

    /// <summary>
    /// Get neighbors in all 4 directions
    /// </summary>
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

    /// <summary>
    /// Get the result grid of tiles
    /// </summary>
    private Tile[,] GetResultGrid()
    {
        var result = new Tile[_settings.Width, _settings.Height];

        for (int x = 0; x < _settings.Width; x++)
        {
            for (int y = 0; y < _settings.Height; y++)
            {
                var cell = _grid[x, y];
                if (cell.Collapsed && cell.PossibleStates.Count > 0)
                {
                    result[x, y] = _settings.Tiles[cell.CollapsedState.Value];
                }
                else
                {
                    // Use first tile as default for uncollapsed cells
                    result[x, y] = _settings.Tiles[0];
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Update progress
    ///
    /// <summary>
    /// Update progress
    /// </summary>
    private void UpdateProgress(string status)
    {
        var progress = Math.Min(100f, _collapsedCells / _totalCells * 100);
        ProgressChanged?.Invoke(this, new WFCProgressEventArgs(progress, status));
    }

    /// <summary>
    /// Reset the service
    /// </summary>
    public void Reset()
    {
        _grid = null;
        _settings = null;
        _context = null;
        _collapsedCells = 0;
        UpdateProgress("Ready");
    }
    
    /// <summary>
    /// Safely notify plugins before generation
    /// </summary>
    private void NotifyPluginsBeforeGeneration()
    {
        try
        {
            var plugins = GetHookPlugins();
        
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.OnBeforeGeneration(_settings);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in plugin {plugin.GetType().Name}.OnBeforeGeneration: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error notifying plugins before generation: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Safely notify plugins after collapse
    /// </summary>
    private void NotifyPluginsAfterCollapse(int x, int y, int state)
    {
        try
        {
            var plugins = GetHookPlugins();
        
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.OnAfterCollapse(x, y, state, _context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in plugin {plugin.GetType().Name}.OnAfterCollapse: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error notifying plugins after collapse: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Safely notify plugins after generation
    /// </summary>
    private void NotifyPluginsAfterGeneration(Tile[,] grid)
    {
        try
        {
            var plugins = GetHookPlugins();
        
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.OnAfterGeneration(grid, _context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in plugin {plugin.GetType().Name}.OnAfterGeneration: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error notifying plugins after generation: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Apply post-processing from all plugins
    /// </summary>
    private Tile[,] ApplyPostProcessing(Tile[,] grid)
    {
        if (grid == null)
            return grid;
        
        var result = grid;
    
        try
        {
            // Уведомляем плагины после генерации
            NotifyPluginsAfterGeneration(grid);
        
            // Применяем пост-обработку от плагинов генерации
            var hookPlugins = GetHookPlugins();
        
            foreach (var plugin in hookPlugins)
            {
                try
                {
                    var processed = plugin.OnPostProcess(result, _context);
                    if (processed != null)
                    {
                        result = processed;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in plugin {plugin.GetType().Name}.OnPostProcess: {ex.Message}");
                }
            }
        
            // Применяем пост-обработку от специализированных плагинов
            var postPlugins = GetPostPlugins();
        
            foreach (var plugin in postPlugins)
            {
                try
                {
                    var processed = plugin.ProcessGrid(result, _context);
                    if (processed != null)
                    {
                        result = processed;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in plugin {plugin.GetType().Name}.ProcessGrid: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in post-processing: {ex.Message}");
        }
    
        return result;
    }
    
    /// <summary>
    /// Safely get plugin modified states
    /// </summary>
private IEnumerable<int> GetPluginModifiedStates(int x, int y, IEnumerable<int> possibleStates)
{
    // Если плагин-менеджер не инициализирован или отсутствует, возвращаем исходные состояния
    if (_pluginManager == null)
    {
        return possibleStates;
    }
    
    // Создаем копию состояний для безопасной работы
    var result = possibleStates.ToList();
    
    try
    {
        // Безопасно получаем список плагинов
        var hookPlugins = GetHookPlugins();
        
        // Если плагинов нет, возвращаем исходные состояния
        if (hookPlugins == null || !hookPlugins.Any())
        {
            return result;
        }
        
        // Для каждого плагина пытаемся модифицировать состояния
        foreach (var plugin in hookPlugins)
        {
            try
            {
                // Пропускаем отключенные плагины (хотя фильтрация должна быть в GetHookPlugins)
                if (!plugin.Enabled)
                    continue;
                    
                // Получаем модифицированные состояния от плагина
                var modified = plugin.OnBeforeCollapse(x, y, result, _context);
                
                // Если плагин вернул непустой список, обновляем результат
                if (modified != null && modified.Any())
                {
                    result = modified.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in plugin {plugin.GetType().Name}.OnBeforeCollapse: {ex.Message}");
                // Продолжаем работу с текущими состояниями
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error getting plugin modified states: {ex.Message}");
    }
    
    return result;
}

/// <summary>
/// Получить список плагинов для генерации - безопасный метод для тестирования
/// </summary>
private IEnumerable<IGenerationHookPlugin> GetHookPlugins()
{
    if (_pluginManager == null)
    {
        return Enumerable.Empty<IGenerationHookPlugin>();
    }
    
    try
    {
        // Используем ToList() для избежания проблем с LINQ-запросами в тестах
        return _pluginManager.GenerationHookPlugins.ToList();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error getting hook plugins: {ex.Message}");
        return Enumerable.Empty<IGenerationHookPlugin>();
    }
}

/// <summary>
/// Получить список плагинов пост-обработки - безопасный метод для тестирования
/// </summary>
private IEnumerable<IPostProcessorPlugin> GetPostPlugins()
{
    if (_pluginManager == null)
    {
        return Enumerable.Empty<IPostProcessorPlugin>();
    }
    
    try
    {
        // Используем ToList() для избежания проблем с LINQ-запросами в тестах
        return _pluginManager.PostProcessorPlugins.ToList();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error getting post plugins: {ex.Message}");
        return Enumerable.Empty<IPostProcessorPlugin>();
    }
}

}