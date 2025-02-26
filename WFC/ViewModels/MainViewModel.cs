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

            // Limit grid size to prevent hanging
            int safeGridWidth = Math.Min(GridWidth, 12);
            int safeGridHeight = Math.Min(GridHeight, 12);

            if (safeGridWidth != GridWidth || safeGridHeight != GridHeight)
            {
                GridWidth = safeGridWidth;
                GridHeight = safeGridHeight;
                Status = $"Grid size limited to {safeGridWidth}x{safeGridHeight} for stability";
                // Tiny delay to allow status to update
                await Task.Delay(10);
            }

            var settings = new WFCSettings
            {
                Width = safeGridWidth,
                Height = safeGridHeight,
                Tiles = Tiles.ToList()
            };

            // Generate rules for tile connections
            RuleGenerator.GenerateRules(settings);

            Status = "Starting generation...";
            await Task.Delay(10); // Allow UI to update

            // Try multiple times with different random seeds if needed
            const int maxGenerationAttempts = 3;
            bool success = false;
            WFCResult result = null;

            for (int attempt = 1; attempt <= maxGenerationAttempts && !success; attempt++)
            {
                try
                {
                    Status = $"Generation attempt {attempt}/{maxGenerationAttempts}...";

                    // Set a reasonable timeout
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        _cancellationTokenSource.Token, timeoutCts.Token);

                    // Run the WFC algorithm
                    result = await _wfcService.GenerateAsync(settings, combinedCts.Token);
                    
                    // Check if generation was successful
                    if (result.Success && result.Grid != null)
                    {
                        success = true;
                    }
                    else if (result.Grid != null)
                    {
                        // If we have a partial result, count it as success
                        if (CountFilledCells(result.Grid) >= safeGridWidth * safeGridHeight * 0.9)
                        {
                            success = true;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // If cancelled by user, exit loop
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        Status = "Generation cancelled";
                        return;
                    }
                    
                    // If timed out, try again
                    Status = $"Attempt {attempt} timed out, trying again...";
                    await Task.Delay(10);
                }
            }

            // Display the generated map if successful
            if (success && result?.Grid != null)
            {
                UpdateGridDisplay(result.Grid);
                Status = "Generation completed with WFC algorithm";
            }
            else
            {
                // No viable map was generated after all attempts, create a truly random one
                Status = "Creating random map...";
                CreateRandomMap(safeGridWidth, safeGridHeight);
            }
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    // Count how many cells in the grid are filled
    private int CountFilledCells(Tile[,] grid)
    {
        int filled = 0;
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y] != null)
                {
                    filled++;
                }
            }
        }
        return filled;
    }

    // Create a truly random map without any patterns
    private void CreateRandomMap(int width, int height)
    {
        GridTiles.Clear();

        double hexWidth = 100;
        double hexHeight = 86;
        double horizontalDistance = hexWidth * 0.75;
        double verticalDistance = hexHeight * 0.87;

        // First create a completely random map
        int[,] tileTypes = new int[width, height];
        
        // Generate random terrain with cellular automata
        // First fill with random water/land
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Random distribution of land and water, with roughly 50/50 chance
                tileTypes[x, y] = random.NextDouble() < 0.5 ? TileTypes.EARTH : TileTypes.WATER;
            }
        }
        
        // Run a few iterations of cellular automata for more natural terrain
        for (int iteration = 0; iteration < 3; iteration++)
        {
            int[,] newTileTypes = new int[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Count neighbors of each type
                    double waterNeighbors = 0;
                    double landNeighbors = 0;
                    
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                if (tileTypes[nx, ny] == TileTypes.WATER)
                                    waterNeighbors++;
                                else if (tileTypes[nx, ny] == TileTypes.EARTH)
                                    landNeighbors++;
                            }
                            else
                            {
                                // For edge cells, count outside as 50/50
                                waterNeighbors += 0.5;
                                landNeighbors += 0.5;
                            }
                        }
                    }
                    
                    // Apply cellular automata rules
                    if (tileTypes[x, y] == TileTypes.WATER)
                    {
                        // Water stays water if surrounded by mostly water
                        newTileTypes[x, y] = waterNeighbors >= 4 ? TileTypes.WATER : TileTypes.EARTH;
                    }
                    else
                    {
                        // Land stays land if surrounded by mostly land
                        newTileTypes[x, y] = landNeighbors >= 4 ? TileTypes.EARTH : TileTypes.WATER;
                    }
                }
            }
            
            // Update tile types for next iteration
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tileTypes[x, y] = newTileTypes[x, y];
                }
            }
        }
        
        // Create shorelines where land meets water
        int[,] finalTileTypes = new int[width, height];
        
        // First just copy the base types
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                finalTileTypes[x, y] = tileTypes[x, y];
            }
        }
        
        // Then add shores
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Only look at horizontal neighbors for shore placement
                if (x > 0 && tileTypes[x, y] == TileTypes.EARTH && tileTypes[x-1, y] == TileTypes.WATER)
                {
                    // Earth with water to the left = Shore Right Water Left
                    finalTileTypes[x, y] = TileTypes.SHORE_RIGHT_WATER_LEFT;
                }
                else if (x < width - 1 && tileTypes[x, y] == TileTypes.EARTH && tileTypes[x+1, y] == TileTypes.WATER)
                {
                    // Earth with water to the right = Shore Left Water Right
                    finalTileTypes[x, y] = TileTypes.SHORE_LEFT_WATER_RIGHT;
                }
            }
        }
        
        // Now render the final map
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var tile = Tiles[finalTileTypes[x, y]];

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

    private void UpdateGridDisplay(Tile[,] grid)
    {
        GridTiles.Clear();

        double hexWidth = 100;
        double hexHeight = 86;
        double horizontalDistance = hexWidth * 0.75;
        double verticalDistance = hexHeight * 0.87;

        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
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