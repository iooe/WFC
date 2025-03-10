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
        Tiles.Add(new Tile(TileTypes.EARTH, "Earth", "grass_center_e.png"));
        Tiles.Add(new Tile(TileTypes.WATER, "Water Full", "water_center_e.png"));
        Tiles.Add(new Tile(TileTypes.SHORE_LEFT_WATER_RIGHT, "Shore Left Water Right", "grass_waterConcave_W.png"));
        Tiles.Add(new Tile(TileTypes.SHORE_RIGHT_WATER_LEFT, "Shore Right Water Left", "grass_waterConcave_E.png"));
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
            int safeGridWidth = Math.Min(width, 15);
            int safeGridHeight = Math.Min(height, 15);

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
        GridTiles.Clear();

        // Use the new hex spacing values provided by the user
        double hexWidth = 100;
        double hexHeight = 86;
        double horizontalDistance = hexWidth * 0.54;
        double verticalDistance = hexHeight * 0.47;

        // Create a simple noise-based map
        int[,] tileTypes = new int[width, height];
        
        // Generate simple noise
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Simple pseudo-noise based on position
                double noiseValue = Math.Sin(x * 0.5) * Math.Cos(y * 0.6) + 
                                  Math.Sin(x * 0.3 + y * 0.7) * 0.4;
                                  
                // Add randomness
                noiseValue += random.NextDouble() * 0.5 - 0.25;
                
                // Map to tile types: above 0 = land, below 0 = water
                tileTypes[x, y] = noiseValue > 0 ? TileTypes.EARTH : TileTypes.WATER;
            }
        }
        
        // Add shoreline tiles
        for (int pass = 0; pass < 2; pass++) // Two passes to ensure consistency
        {
            int[,] newTileTypes = new int[width, height];
            
            // Copy the current types first
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    newTileTypes[x, y] = tileTypes[x, y];
                }
            }
            
            // Then add shores
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Check right neighbor
                    if (x < width - 1)
                    {
                        if (tileTypes[x, y] == TileTypes.EARTH && tileTypes[x + 1, y] == TileTypes.WATER)
                        {
                            newTileTypes[x, y] = TileTypes.SHORE_LEFT_WATER_RIGHT;
                        }
                        else if (tileTypes[x, y] == TileTypes.WATER && tileTypes[x + 1, y] == TileTypes.EARTH)
                        {
                            newTileTypes[x + 1, y] = TileTypes.SHORE_RIGHT_WATER_LEFT;
                        }
                    }
                }
            }
            
            // Update the tile types
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tileTypes[x, y] = newTileTypes[x, y];
                }
            }
        }
        
        // Now render the tiles
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var tile = Tiles[tileTypes[x, y]];

                double xPos = x * horizontalDistance;
                double yPos = y * verticalDistance;

                if (y % 2 == 1)
                {
                    xPos += horizontalDistance / 2;
                }

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

        // Use the new hex spacing values provided by the user
        double hexWidth = 100;
        double hexHeight = 86;
        double horizontalDistance = hexWidth * 0.54;
        double verticalDistance = hexHeight * 0.47;

        for (int y = 0; y < settings.Height; y++)
        {
            for (int x = 0; x < settings.Width; x++)
            {
                var tile = grid[x, y] ?? Tiles[0]; // Default to Earth if null

                double xPos = x * horizontalDistance;
                double yPos = y * verticalDistance;

                if (y % 2 == 1)
                {
                    xPos += horizontalDistance / 2;
                }

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