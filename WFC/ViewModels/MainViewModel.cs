using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WFC.Models;
using WFC.Services;
using WFC.Factories;
using WFC.Services.Export;

namespace WFC.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IWFCService _wfcService;
    private readonly ITileFactory _tileFactory;
    private readonly IExporterFactory _exporterFactory;
    private CancellationTokenSource _cancellationTokenSource;

    public event PropertyChangedEventHandler PropertyChanged;

    public ICommand GenerateCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand ExportAsPngCommand { get; }
    public ICommand ExportAsTilesCommand { get; }

    private float _progress;

    public float Progress
    {
        get => _progress;
        set
        {
            _progress = value;
            OnPropertyChanged(nameof(Progress));
        }
    }

    private string _status = "Ready";

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged(nameof(Status));
        }
    }

    private int _gridWidth = 15;

    public int GridWidth
    {
        get => _gridWidth;
        set
        {
            _gridWidth = value;
            OnPropertyChanged(nameof(GridWidth));
        }
    }

    private int _gridHeight = 15;

    public int GridHeight
    {
        get => _gridHeight;
        set
        {
            _gridHeight = value;
            OnPropertyChanged(nameof(GridHeight));
        }
    }

    private bool _isAnimationEnabled = true;

    public bool IsAnimationEnabled
    {
        get => _isAnimationEnabled;
        set
        {
            _isAnimationEnabled = value;
            OnPropertyChanged(nameof(IsAnimationEnabled));
        }
    }

    private ObservableCollection<TileDisplay> _gridTiles;

    public ObservableCollection<TileDisplay> GridTiles
    {
        get => _gridTiles;
        set
        {
            _gridTiles = value;
            OnPropertyChanged(nameof(GridTiles));
        }
    }

    public ObservableCollection<Tile> Tiles { get; }

    // Обновленный конструктор с добавлением фабрики экспортеров
    public MainViewModel(IWFCService wfcService, ITileFactory tileFactory, IExporterFactory exporterFactory)
    {
        _wfcService = wfcService;
        _tileFactory = tileFactory;
        _exporterFactory = exporterFactory;
        _wfcService.ProgressChanged += OnProgressChanged;

        Tiles = new ObservableCollection<Tile>();
        GridTiles = new ObservableCollection<TileDisplay>();
        GenerateCommand = new AsyncRelayCommand(GenerateAsync);
        CancelCommand = new RelayCommand(Cancel);
        ResetCommand = new RelayCommand(Reset);

        InitializeDefaultTiles();

        // Инициализируем команды экспорта
        ExportAsPngCommand = new AsyncRelayCommand(ExportAsPngAsync);
        ExportAsTilesCommand = new AsyncRelayCommand(ExportAsTilesAsync);
    }

    private Tile[,] PostProcessGrid(Tile[,] grid, int width, int height)
    {
        // First pass: identify grass and pavement boundaries
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var currentTile = grid[x, y];
                int currentId = currentTile.Id;

                // Skip if already a transition tile or building tile
                if ((currentId >= TileTypes.PAVEMENT_GRASS_LEFT &&
                     currentId <= TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT) ||
                    TileTypes.IsWallTile(currentId) || TileTypes.IsWindowTile(currentId))
                    continue;

                // Only process pavement tiles for conversion to transition tiles
                if (currentId == TileTypes.PAVEMENT)
                {
                    // Check neighbors (with bounds checking)
                    bool topIsGrass = y > 0 && TileTypes.IsGrassLike(grid[x, y - 1].Id);
                    bool rightIsGrass = x < width - 1 && TileTypes.IsGrassLike(grid[x + 1, y].Id);
                    bool bottomIsGrass = y < height - 1 && TileTypes.IsGrassLike(grid[x, y + 1].Id);
                    bool leftIsGrass = x > 0 && TileTypes.IsGrassLike(grid[x - 1, y].Id);

                    // If there's grass nearby, convert to appropriate transition tile
                    if (topIsGrass || rightIsGrass || bottomIsGrass || leftIsGrass)
                    {
                        int newTileType =
                            TileTypes.GetTransitionTile(topIsGrass, rightIsGrass, bottomIsGrass, leftIsGrass);
                        grid[x, y] = Tiles[newTileType]; // Replace with transition tile
                    }
                }
            }
        }

        // Second pass: Ensure building coherence
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var currentTile = grid[x, y];
                int currentId = currentTile.Id;

                // Fix issues with buildings
                if (TileTypes.IsWallTile(currentId) || TileTypes.IsWindowTile(currentId))
                {
                    // Process building tiles
                    grid = EnsureBuildingCoherence(grid, x, y, width, height);
                }
            }
        }

        return grid;
    }

    // Helper method to ensure building tiles connect properly
    private Tile[,] EnsureBuildingCoherence(Tile[,] grid, int x, int y, int width, int height)
    {
        int currentId = grid[x, y].Id;

        // Fix window pairs
        if (currentId == TileTypes.WALL_WINDOW_TOP)
        {
            // Window top should have window bottom below it
            if (y < height - 1 && !TileTypes.IsWindowTile(grid[x, y + 1].Id))
            {
                // Only replace if below is a wall (not another type of tile)
                if (y < height - 1 && TileTypes.IsWallTile(grid[x, y + 1].Id))
                    grid[x, y + 1] = Tiles[TileTypes.WALL_WINDOW_BOTTOM];
                else
                    // If we can't place a window bottom, replace window top with wall
                    grid[x, y] = Tiles[TileTypes.WALL_FRONT_MIDDLE];
            }
        }
        else if (currentId == TileTypes.WALL_WINDOW_BOTTOM)
        {
            // Window bottom should have window top above it
            if (y > 0 && !TileTypes.IsWindowTile(grid[x, y - 1].Id))
            {
                // Only replace if above is a wall (not another type of tile)
                if (y > 0 && TileTypes.IsWallTile(grid[x, y - 1].Id))
                    grid[x, y - 1] = Tiles[TileTypes.WALL_WINDOW_TOP];
                else
                    // If we can't place a window top, replace window bottom with wall
                    grid[x, y] = Tiles[TileTypes.WALL_FRONT_MIDDLE];
            }
        }

        // Building edge checks - walls should connect to other walls or appropriate transitions
        if (TileTypes.IsWallTile(currentId))
        {
            // Ensure wall corners are properly placed
            if (currentId == TileTypes.WALL_FRONT_CORNER_TOP_LEFT)
            {
                // Should have wall to right and below
                EnsureConnectedTileIfWall(grid, x + 1, y, width, height, TileTypes.WALL_FRONT_MIDDLE);
                EnsureConnectedTileIfWall(grid, x, y + 1, width, height, TileTypes.WALL_FRONT_LEFT_END);
            }
            else if (currentId == TileTypes.WALL_FRONT_CORNER_TOP_RIGHT)
            {
                // Should have wall to left and below
                EnsureConnectedTileIfWall(grid, x - 1, y, width, height, TileTypes.WALL_FRONT_MIDDLE);
                EnsureConnectedTileIfWall(grid, x, y + 1, width, height, TileTypes.WALL_FRONT_RIGHT_END);
            }
            else if (currentId == TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT)
            {
                // Should have wall to right and above
                EnsureConnectedTileIfWall(grid, x + 1, y, width, height, TileTypes.WALL_FRONT_MIDDLE);
                EnsureConnectedTileIfWall(grid, x, y - 1, width, height, TileTypes.WALL_FRONT_LEFT_END);
            }
            else if (currentId == TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT)
            {
                // Should have wall to left and above
                EnsureConnectedTileIfWall(grid, x - 1, y, width, height, TileTypes.WALL_FRONT_MIDDLE);
                EnsureConnectedTileIfWall(grid, x, y - 1, width, height, TileTypes.WALL_FRONT_RIGHT_END);
            }
        }

        return grid;
    }

    // Helper method to ensure connected tile is appropriate wall if needed
    private void EnsureConnectedTileIfWall(Tile[,] grid, int x, int y, int width, int height, int defaultTileType)
    {
        // Check bounds
        if (x < 0 || y < 0 || x >= width || y >= height)
            return;

        // If current tile is not a wall or window, replace with default type
        int currentId = grid[x, y].Id;
        if (!TileTypes.IsWallTile(currentId) && !TileTypes.IsWindowTile(currentId))
        {
            grid[x, y] = Tiles[defaultTileType];
        }
    }

    // Метод для инициализации плиток по умолчанию
    private void InitializeDefaultTiles()
    {
        // Basic tiles
        Tiles.Add(_tileFactory.CreateBasicTile(TileTypes.GRASS, "Grass"));
        Tiles.Add(_tileFactory.CreateBasicTile(TileTypes.FLOWERS, "Flowers"));
        Tiles.Add(_tileFactory.CreateBasicTile(TileTypes.PAVEMENT, "Pavement"));

        // Transition tiles
        Tiles.Add(_tileFactory.CreateTransitionTile(TileTypes.PAVEMENT_GRASS_LEFT, "Pavement-Grass Left", "left"));
        Tiles.Add(_tileFactory.CreateTransitionTile(TileTypes.PAVEMENT_GRASS_RIGHT, "Pavement-Grass Right", "right"));
        Tiles.Add(_tileFactory.CreateTransitionTile(TileTypes.PAVEMENT_GRASS_TOP, "Pavement-Grass Top", "top"));
        Tiles.Add(_tileFactory.CreateTransitionTile(TileTypes.PAVEMENT_GRASS_BOTTOM, "Pavement-Grass Bottom",
            "bottom"));

        // Corner transition tiles
        Tiles.Add(_tileFactory.CreateTransitionTile(TileTypes.PAVEMENT_GRASS_TOP_LEFT, "Pavement-Grass Top-Left",
            "top-left"));
        Tiles.Add(_tileFactory.CreateTransitionTile(TileTypes.PAVEMENT_GRASS_TOP_RIGHT, "Pavement-Grass Top-Right",
            "top-right"));
        Tiles.Add(_tileFactory.CreateTransitionTile(TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, "Pavement-Grass Bottom-Left",
            "bottom-left"));
        Tiles.Add(_tileFactory.CreateTransitionTile(TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT,
            "Pavement-Grass Bottom-Right", "bottom-right"));

        // Wall tiles - front-facing
        Tiles.Add(_tileFactory.CreateBuildingTile(TileTypes.WALL_FRONT_CORNER_TOP_LEFT, "Wall Front Top-Left Corner",
            "wall-front-corner-top-left"));
        Tiles.Add(_tileFactory.CreateBuildingTile(TileTypes.WALL_FRONT_CORNER_TOP_RIGHT, "Wall Front Top-Right Corner",
            "wall-front-corner-top-right"));
        Tiles.Add(_tileFactory.CreateBuildingTile(TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT,
            "Wall Front Bottom-Left Corner", "wall-front-corner-bottom-left"));
        Tiles.Add(_tileFactory.CreateBuildingTile(TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT,
            "Wall Front Bottom-Right Corner", "wall-front-corner-bottom-right"));
        Tiles.Add(
            _tileFactory.CreateBuildingTile(TileTypes.WALL_FRONT_MIDDLE, "Wall Front Middle", "wall-front-middle"));
        Tiles.Add(_tileFactory.CreateBuildingTile(TileTypes.WALL_FRONT_TOP_END, "Wall Front Top End",
            "wall-front-top-end"));
        Tiles.Add(_tileFactory.CreateBuildingTile(TileTypes.WALL_FRONT_BOTTOM_END, "Wall Front Bottom End",
            "wall-front-bottom-end"));
        Tiles.Add(_tileFactory.CreateBuildingTile(TileTypes.WALL_FRONT_LEFT_END, "Wall Front Left End",
            "wall-front-left-end"));
        Tiles.Add(_tileFactory.CreateBuildingTile(TileTypes.WALL_FRONT_RIGHT_END, "Wall Front Right End",
            "wall-front-right-end"));

        // Window tiles
        Tiles.Add(_tileFactory.CreateBuildingTile(TileTypes.WALL_WINDOW_TOP, "Wall Window Top", "window-top"));
        Tiles.Add(_tileFactory.CreateBuildingTile(TileTypes.WALL_WINDOW_BOTTOM, "Wall Window Bottom", "window-bottom"));
    }

    private async Task GenerateAsync()
    {
        try
        {
            // Clear any previous generation
            Reset();

            // Create a new cancellation token source
            _cancellationTokenSource = new CancellationTokenSource();

            // Get grid dimensions from UI
            int width = GridWidth;
            int height = GridHeight;

            // Limit grid size to reasonable values
            int safeGridWidth = Math.Min(width, 256);
            int safeGridHeight = Math.Min(height, 256);

            if (safeGridWidth != width || safeGridHeight != height)
            {
                GridWidth = safeGridWidth;
                GridHeight = safeGridHeight;
                Status = $"Grid size limited to {safeGridWidth}x{safeGridHeight} for stability";
                await Task.Delay(10); // Allow UI to update
            }

            var settings = new WFCSettings
            {
                Width = safeGridWidth,
                Height = safeGridHeight,
                Tiles = Tiles.ToList()
            };

            // Generate rules for tile connections
            RuleGenerator.GenerateRules(settings);

            Status = "Starting WFC Generation...";
            await Task.Delay(10); // Allow UI to update

            try
            {
                // Use a generous timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    _cancellationTokenSource.Token, timeoutCts.Token);

                // Run the WFC algorithm
                var result = await _wfcService.GenerateAsync(settings, combinedCts.Token);

                if (result.Success && result.Grid != null)
                {
                    await UpdateGridDisplay(result.Grid, settings);
                    Status = "WFC generation completed successfully";
                }
                else
                {
                    Status = "WFC failed: " + (result.ErrorMessage ?? "Unknown error");
                    await Task.Delay(10); // Allow UI to update

                    // Use a fallback method if WFC fails
                    CreateFallbackGrid(safeGridWidth, safeGridHeight);
                }
            }
            catch (OperationCanceledException)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    Status = "Generation was cancelled";
                }
                else
                {
                    Status = "Generation timed out - Creating fallback map";
                    CreateFallbackGrid(safeGridWidth, safeGridHeight);
                }
            }
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    // Create a fallback grid with randomized patterns
    private void CreateFallbackGrid(int width, int height)
    {
        // В исходном коде этот метод был пустым - оставляем его пустым
    }

    // Updated method for animated rendering
    private async Task UpdateGridDisplay(Tile[,] grid, WFCSettings settings)
    {
        // Post-process the grid to add transition tiles
        grid = PostProcessGrid(grid, settings.Width, settings.Height);

        GridTiles.Clear();

        // If animation is disabled, add all tiles at once
        if (!IsAnimationEnabled)
        {
            for (int y = 0; y < settings.Height; y++)
            {
                for (int x = 0; x < settings.Width; x++)
                {
                    var tile = grid[x, y] ?? Tiles[0]; // Default to first tile if null

                    // Create a copy of the tile to get a fresh random image
                    var tileCopy = new Tile(tile.Id, tile.Name, tile.FolderPath);

                    // Calculate position using regular grid layout
                    double xPos = x * 100;
                    double yPos = y * 100;

                    // Create a new TileDisplay with the newly created tile
                    GridTiles.Add(new TileDisplay(tileCopy, (float)xPos, (float)yPos));
                }
            }
        }
        else
        {
            // Animated rendering
            await AnimateTilesDisplay(grid, settings);
        }
    }

    // Method for animated tile rendering in WFC generation order
    private async Task AnimateTilesDisplay(Tile[,] grid, WFCSettings settings)
    {
        // Calculate delay based on grid size
        int totalTiles = settings.Width * settings.Height;
        int baseDelay = 1; // Base delay in milliseconds

        // Scale delay inversely proportional to grid size
        int delay = Math.Max(1, baseDelay * 100 / totalTiles * 10);

        Status = "Animating WFC algorithm steps...";

        // First phase: initial grid generation
        Status = "Phase 1: Initial WFC grid generation...";
        await Task.Delay(500); // Short delay for reading status

        // Step 1: Main generation - sequentially from left to right, top to bottom
        // This corresponds to the generation order in DefaultWFCService.GenerateAsync
        for (int y = 0; y < settings.Height; y++)
        {
            for (int x = 0; x < settings.Width; x++)
            {
                var tile = grid[x, y] ?? Tiles[0]; // Default to first tile if null

                // Create a copy of the tile to get a fresh random image
                var tileCopy = new Tile(tile.Id, tile.Name, tile.FolderPath);

                // Calculate position using regular grid layout
                double xPos = x * 100;
                double yPos = y * 100;

                // Create a new TileDisplay with the newly created tile
                GridTiles.Add(new TileDisplay(tileCopy, (float)xPos, (float)yPos));

                // Add delay between tiles for visualization
                await Task.Delay(delay);
            }

            // Update status every few rows
            if (y % 5 == 0 || y == settings.Height - 1)
            {
                Status = $"Phase 1: Generating row {y + 1}/{settings.Height}...";
            }
        }

        // Status before transitioning to post-processing phase
        Status = "Phase 2: Post-processing transitions and buildings...";
        await Task.Delay(1000); // Longer pause before next phase

        // Step 2: Show post-processing by updating existing tiles
        // First identify which tiles changed during post-processing
        List<(int x, int y, Tile tile)> changedTiles = new List<(int x, int y, Tile tile)>();

        // Identify likely changes from post-processing
        // First check transition tiles between grass and pavement
        for (int y = 0; y < settings.Height; y++)
        {
            for (int x = 0; x < settings.Width; x++)
            {
                var currentTile = grid[x, y];
                int currentId = currentTile.Id;

                // Check if tile is a transition or building
                bool isTransition = currentId >= TileTypes.PAVEMENT_GRASS_LEFT &&
                                    currentId <= TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT;
                bool isBuilding = TileTypes.IsWallTile(currentId) || TileTypes.IsWindowTile(currentId);

                // If tile is a transition or building, it was likely changed during post-processing
                if (isTransition || isBuilding)
                {
                    changedTiles.Add((x, y, currentTile));
                }
            }
        }

        // If there are changes from post-processing, animate them
        if (changedTiles.Count > 0)
        {
            Status = $"Phase 2: Processing {changedTiles.Count} transitions and buildings...";

            // Group by type for better visualization
            // First transition tiles (between grass and pavement)
            var transitionTiles = changedTiles.Where(t =>
                t.tile.Id >= TileTypes.PAVEMENT_GRASS_LEFT &&
                t.tile.Id <= TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT).ToList();

            if (transitionTiles.Count > 0)
            {
                Status = $"Phase 2a: Adding {transitionTiles.Count} terrain transitions...";
                await Task.Delay(500);

                // Animate updating transition tiles
                foreach (var (x, y, tile) in transitionTiles)
                {
                    // Find tile in collection and update it
                    var tileDisplay = GridTiles.FirstOrDefault(t =>
                        Math.Abs(t.X - (x * 100)) < 0.1 &&
                        Math.Abs(t.Y - (y * 100)) < 0.1);

                    if (tileDisplay != null)
                    {
                        // Create new tile with updated data
                        var tileCopy = new Tile(tile.Id, tile.Name, tile.FolderPath);

                        // Update image
                        var index = GridTiles.IndexOf(tileDisplay);
                        GridTiles[index] = new TileDisplay(tileCopy, tileDisplay.X, tileDisplay.Y);

                        // Delay for effect
                        await Task.Delay(delay * 2);
                    }
                }
            }

            // Now buildings
            var buildingTiles = changedTiles.Where(t =>
                TileTypes.IsWallTile(t.tile.Id) || TileTypes.IsWindowTile(t.tile.Id)).ToList();

            if (buildingTiles.Count > 0)
            {
                Status = $"Phase 2b: Building structures ({buildingTiles.Count} tiles)...";
                await Task.Delay(500);

                // First basic building parts
                var wallTiles = buildingTiles.Where(t => TileTypes.IsWallTile(t.tile.Id)).ToList();

                foreach (var (x, y, tile) in wallTiles)
                {
                    // Find tile in collection and update it
                    var tileDisplay = GridTiles.FirstOrDefault(t =>
                        Math.Abs(t.X - (x * 100)) < 0.1 &&
                        Math.Abs(t.Y - (y * 100)) < 0.1);

                    if (tileDisplay != null)
                    {
                        // Create new tile with updated data
                        var tileCopy = new Tile(tile.Id, tile.Name, tile.FolderPath);

                        // Update image
                        var index = GridTiles.IndexOf(tileDisplay);
                        GridTiles[index] = new TileDisplay(tileCopy, tileDisplay.X, tileDisplay.Y);

                        // Delay for effect
                        await Task.Delay(delay * 3);
                    }
                }

                // Now windows
                var windowTiles = buildingTiles.Where(t => TileTypes.IsWindowTile(t.tile.Id)).ToList();

                if (windowTiles.Count > 0)
                {
                    Status = $"Phase 2c: Adding windows ({windowTiles.Count} tiles)...";
                    await Task.Delay(500);

                    foreach (var (x, y, tile) in windowTiles)
                    {
                        // Find tile in collection and update it
                        var tileDisplay = GridTiles.FirstOrDefault(t =>
                            Math.Abs(t.X - (x * 100)) < 0.1 &&
                            Math.Abs(t.Y - (y * 100)) < 0.1);

                        if (tileDisplay != null)
                        {
                            // Create new tile with updated data
                            var tileCopy = new Tile(tile.Id, tile.Name, tile.FolderPath);

                            // Update image
                            var index = GridTiles.IndexOf(tileDisplay);
                            GridTiles[index] = new TileDisplay(tileCopy, tileDisplay.X, tileDisplay.Y);

                            // Delay for effect
                            await Task.Delay(delay * 4);
                        }
                    }
                }
            }
        }

        Status = "WFC generation visualization completed";
    }

    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        Status = "Operation cancelled";
    }

    private void Reset()
    {
        _wfcService.Reset();
        GridTiles.Clear();
        Progress = 0;
        Status = "Ready";
    }

    private void OnProgressChanged(object sender, WFCProgressEventArgs e)
    {
        Progress = e.Progress;
        Status = e.Status;
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Обновленный метод экспорта в PNG, использующий новую систему
    private async Task ExportAsPngAsync()
    {
        try
        {
            Status = "Exporting map as PNG...";
            await Task.Delay(10); // Позволяем UI обновиться

            // Используем фабрику для создания нужного экспортера
            var exporter = _exporterFactory.CreateExporter(ExportType.Png);

            // Делегируем задачу экспорта
            var result = await exporter.ExportAsync(GridTiles, GridWidth, GridHeight);

            // Обновляем статус
            Status = result;
        }
        catch (Exception ex)
        {
            Status = $"Error exporting map: {ex.Message}";
        }
    }

    // Обновленный метод экспорта отдельных тайлов, использующий новую систему
    private async Task ExportAsTilesAsync()
    {
        try
        {
            Status = "Exporting tiles...";
            await Task.Delay(10); // Позволяем UI обновиться

            // Используем фабрику для создания нужного экспортера
            var exporter = _exporterFactory.CreateExporter(ExportType.Tiles);

            // Делегируем задачу экспорта
            var result = await exporter.ExportAsync(GridTiles, GridWidth, GridHeight);

            // Обновляем статус
            Status = result;
        }
        catch (Exception ex)
        {
            Status = $"Error exporting tiles: {ex.Message}";
        }
    }
}