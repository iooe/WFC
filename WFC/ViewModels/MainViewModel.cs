using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WFC.Models;
using WFC.Services;

namespace WFC.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IWFCService _wfcService;
    private readonly Random random;
    private CancellationTokenSource _cancellationTokenSource;

    public event PropertyChangedEventHandler PropertyChanged;

    public ICommand GenerateCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetCommand { get; }

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

    public MainViewModel(IWFCService wfcService)
    {
        _wfcService = wfcService;
        random = new Random();
        _wfcService.ProgressChanged += OnProgressChanged;

        Tiles = new ObservableCollection<Tile>();
        GridTiles = new ObservableCollection<TileDisplay>();
        GenerateCommand = new AsyncRelayCommand(GenerateAsync);
        CancelCommand = new RelayCommand(Cancel);
        ResetCommand = new RelayCommand(Reset);

        InitializeDefaultTiles();
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

                // Skip if already a transition tile
                if (currentId >= TileTypes.PAVEMENT_GRASS_LEFT && currentId <= TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT)
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

        return grid;
    }

    private void InitializeDefaultTiles()
    {
        // Basic tiles
        Tiles.Add(new Tile(TileTypes.GRASS, "Grass", "grass"));
        Tiles.Add(new Tile(TileTypes.FLOWERS, "Flowers", "flowers"));
        Tiles.Add(new Tile(TileTypes.PAVEMENT, "Pavement", "pavement"));

        // Transition tiles
        Tiles.Add(new Tile(TileTypes.PAVEMENT_GRASS_LEFT, "Pavement-Grass Left", "pavement-transitions/left"));
        Tiles.Add(new Tile(TileTypes.PAVEMENT_GRASS_RIGHT, "Pavement-Grass Right", "pavement-transitions/right"));
        Tiles.Add(new Tile(TileTypes.PAVEMENT_GRASS_TOP, "Pavement-Grass Top", "pavement-transitions/top"));
        Tiles.Add(new Tile(TileTypes.PAVEMENT_GRASS_BOTTOM, "Pavement-Grass Bottom", "pavement-transitions/bottom"));

        // Corner transition tiles
        Tiles.Add(new Tile(TileTypes.PAVEMENT_GRASS_TOP_LEFT, "Pavement-Grass Top-Left",
            "pavement-transitions/top-left"));
        Tiles.Add(new Tile(TileTypes.PAVEMENT_GRASS_TOP_RIGHT, "Pavement-Grass Top-Right",
            "pavement-transitions/top-right"));
        Tiles.Add(new Tile(TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, "Pavement-Grass Bottom-Left",
            "pavement-transitions/bottom-left"));
        Tiles.Add(new Tile(TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, "Pavement-Grass Bottom-Right",
            "pavement-transitions/bottom-right"));
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
                    UpdateGridDisplay(result.Grid, settings);
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
    }

// In UpdateGridDisplay method, add a call to PostProcessGrid:
    private void UpdateGridDisplay(Tile[,] grid, WFCSettings settings)
    {
        // Post-process the grid to add transition tiles
        grid = PostProcessGrid(grid, settings.Width, settings.Height);

        GridTiles.Clear();

        for (int y = 0; y < settings.Height; y++)
        {
            for (int x = 0; x < settings.Width; x++)
            {
                var tile = grid[x, y] ?? Tiles[0]; // Default to first tile if null

                // Create a copy of the tile to get a fresh random image
                // This ensures each tile placement gets a different random image
                var tileCopy = new Tile(tile.Id, tile.Name, tile.FolderPath);

                // Calculate position using regular grid layout
                double xPos = x * 100;
                double yPos = y * 100;

                // Create a new TileDisplay with the newly created tile
                GridTiles.Add(new TileDisplay(tileCopy, (float)xPos, (float)yPos));
            }
        }
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
}