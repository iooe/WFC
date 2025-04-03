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

    private int _gridWidth = 10;

    public int GridWidth
    {
        get => _gridWidth;
        set
        {
            _gridWidth = value;
            OnPropertyChanged(nameof(GridWidth));
        }
    }

    private int _gridHeight = 10;

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

    private void InitializeDefaultTiles()
    {
        // Using constants instead of hardcoded IDs
        // Now using folder paths instead of specific image files
        Tiles.Add(new Tile(TileTypes.GRASS, "Grass", "grass"));
        Tiles.Add(new Tile(TileTypes.FLOWERS, "Flowers", "flowers"));
        Tiles.Add(new Tile(TileTypes.PAVEMENT, "Pavement", "pavement"));
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
        return;
        GridTiles.Clear();

        // Use regular grid spacing
        double tileWidth = 100;
        double tileHeight = 100;

        // Create a simple noise-based map
        int[,] tileTypes = new int[width, height];

        // Generate simple noise
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Simple pseudo-noise based on position for grass vs flowers
                double grassFlowerNoise = Math.Sin(x * 0.5) * Math.Cos(y * 0.6) +
                                          Math.Sin(x * 0.3 + y * 0.7) * 0.4;

                // Add randomness
                grassFlowerNoise += random.NextDouble() * 0.5 - 0.25;

                // Different noise for pavement areas
                double pavementNoise = Math.Cos(x * 0.3) * Math.Sin(y * 0.2) +
                                       Math.Cos(x * 0.1 + y * 0.3) * 0.6;
                pavementNoise += random.NextDouble() * 0.3 - 0.15;

                // Pavement areas should be rarer but connected
                bool isPavement = pavementNoise > 0.6 && (x % 3 == 0 || y % 3 == 0);

                // Map to tile types
                if (isPavement)
                {
                    tileTypes[x, y] = TileTypes.PAVEMENT;
                }
                else if (grassFlowerNoise > 0.2) // Higher threshold for flowers to make them rarer
                {
                    tileTypes[x, y] = TileTypes.FLOWERS;
                }
                else
                {
                    tileTypes[x, y] = TileTypes.GRASS;
                }
            }
        }

        // Make sure we have some coherent areas - apply cellular automata to smooth
        for (int pass = 0; pass < 2; pass++)
        {
            int[,] newTileTypes = new int[width, height];

            // Copy current types first
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    newTileTypes[x, y] = tileTypes[x, y];
                }
            }

            // Apply smoothing based on neighbors
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Count neighbor types
                    int[] typeCount = new int[3] { 0, 0, 0 }; // Count of GRASS, FLOWERS, PAVEMENT
                    int totalNeighbors = 0;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                totalNeighbors++;
                                typeCount[tileTypes[nx, ny]]++;
                            }
                        }
                    }

                    // Find the most common type
                    int mostCommonType = 0;
                    for (int i = 1; i < typeCount.Length; i++)
                    {
                        if (typeCount[i] > typeCount[mostCommonType])
                        {
                            mostCommonType = i;
                        }
                    }

                    // 80% chance to conform to the most common neighbor type
                    if (random.NextDouble() < 0.8)
                    {
                        newTileTypes[x, y] = mostCommonType;
                    }
                }
            }

            // Update tile types
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tileTypes[x, y] = newTileTypes[x, y];
                }
            }
        }

        // Refresh tile images for each tile
        foreach (var tile in Tiles)
        {
            if (tile is Tile dynamicTile)
            {
                dynamicTile.LoadRandomImage();
            }
        }

        // Now render the tiles in a regular grid (no offset for odd rows)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var tile = Tiles[tileTypes[x, y]];

                // Regular grid layout (no offset)
                double xPos = x * tileWidth;
                double yPos = y * tileHeight;

                GridTiles.Add(new TileDisplay
                {
                    Image = tile.Image,
                    X = (float)xPos,
                    Y = (float)yPos
                });
            }
        }

        Status = "Created random procedural map";
    }

    private void UpdateGridDisplay(Tile[,] grid, WFCSettings settings)
    {
        GridTiles.Clear();

        // Use regular grid spacing values instead of hexagonal offset
        double tileWidth = 100;
        double tileHeight = 100;

        for (int y = 0; y < settings.Height; y++)
        {
            for (int x = 0; x < settings.Width; x++)
            {
                var tile = grid[x, y] ?? Tiles[0]; // Default to first tile if null

                // Calculate position using regular grid layout (no offset for odd rows)
                double xPos = x * tileWidth;
                double yPos = y * tileHeight;

                // Add the tile to the display
                GridTiles.Add(new TileDisplay
                {
                    Image = tile.Image,
                    X = (float)xPos,
                    Y = (float)yPos
                });
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