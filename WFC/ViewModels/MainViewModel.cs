using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WFC.Models;
using WFC.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WFC.Helpers;
using Application = System.Windows.Application;
using Image = System.Windows.Controls.Image;
using Size = System.Windows.Size;

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


// Initialize these commands in the constructor:
        ExportAsPngCommand = new AsyncRelayCommand(ExportAsPngAsync);
        ExportAsTilesCommand = new AsyncRelayCommand(ExportAsTilesAsync);
    }

    // Add these properties to your MainViewModel class
    public ICommand ExportAsPngCommand { get; }
    public ICommand ExportAsTilesCommand { get; }

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


// Обновить метод InitializeDefaultTiles в MainViewModel.cs:

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

        // Wall tiles - front-facing
        Tiles.Add(new Tile(TileTypes.WALL_FRONT_CORNER_TOP_LEFT, "Wall Front Top-Left Corner",
            "buildings/wall-front-corner-top-left"));
        Tiles.Add(new Tile(TileTypes.WALL_FRONT_CORNER_TOP_RIGHT, "Wall Front Top-Right Corner",
            "buildings/wall-front-corner-top-right"));
        Tiles.Add(new Tile(TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT, "Wall Front Bottom-Left Corner",
            "buildings/wall-front-corner-bottom-left"));
        Tiles.Add(new Tile(TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT, "Wall Front Bottom-Right Corner",
            "buildings/wall-front-corner-bottom-right"));
        Tiles.Add(new Tile(TileTypes.WALL_FRONT_MIDDLE, "Wall Front Middle",
            "buildings/wall-front-middle"));
        Tiles.Add(new Tile(TileTypes.WALL_FRONT_TOP_END, "Wall Front Top End",
            "buildings/wall-front-top-end"));
        Tiles.Add(new Tile(TileTypes.WALL_FRONT_BOTTOM_END, "Wall Front Bottom End",
            "buildings/wall-front-bottom-end"));
        Tiles.Add(new Tile(TileTypes.WALL_FRONT_LEFT_END, "Wall Front Left End",
            "buildings/wall-front-left-end"));
        Tiles.Add(new Tile(TileTypes.WALL_FRONT_RIGHT_END, "Wall Front Right End",
            "buildings/wall-front-right-end"));

        // Window tiles
        Tiles.Add(new Tile(TileTypes.WALL_WINDOW_TOP, "Wall Window Top",
            "buildings/window-top"));
        Tiles.Add(new Tile(TileTypes.WALL_WINDOW_BOTTOM, "Wall Window Bottom",
            "buildings/window-bottom"));
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


    // Improved export as PNG method (add to MainViewModel)
    private async Task ExportAsPngAsync()
    {
        try
        {
            // Use SaveFileDialog to get output filename
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export map as PNG",
                Filter = "PNG Image|*.png",
                DefaultExt = ".png",
                FileName = $"WFC_Map_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                Status = "Exporting map as PNG...";
                await Task.Delay(10); // Allow UI to update

                // Get a reference to the main grid container (using a method in MainWindow)
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                {
                    Status = "Error: Cannot access the main window";
                    return;
                }

                // Get a reference to the Canvas or ItemsControl that contains all tiles
                var tileContainer = mainWindow.GetTileContainer();
                if (tileContainer == null)
                {
                    Status = "Error: Cannot find the tile container";
                    return;
                }

                // Calculate the exact dimensions needed
                int width = GridWidth * 100;
                int height = GridHeight * 100;

                // Create a Canvas specifically for export
                var exportCanvas = new Canvas { Width = width, Height = height };

                // For each tile position, create and position an image
                foreach (var tile in GridTiles)
                {
                    // Create new image for each tile
                    var img = new Image
                    {
                        Source = tile.Image,
                        Width = 100,
                        Height = 100
                    };

                    // Position it correctly
                    Canvas.SetLeft(img, tile.X);
                    Canvas.SetTop(img, tile.Y);

                    // Add to our export canvas
                    exportCanvas.Children.Add(img);
                }

                // Use the helper to capture and save
                VisualHelper.CaptureElementToPng(exportCanvas, dialog.FileName, width, height);

                Status = $"Map exported as PNG to {dialog.FileName}";
            }
        }
        catch (Exception ex)
        {
            Status = $"Error exporting map: {ex.Message}";
        }
    }

    // Use this instead of the Windows.Forms FolderBrowserDialog
    private string GetFolderPath(string description)
    {
        // Since WPF doesn't have a built-in folder picker, we'll use the 
        // SaveFileDialog with a workaround to select folders
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = description,
            Filter = "Folder|*.folder", // Not actually used
            FileName = "Select Folder" // Initial filename
        };

        // Event to handle when dialog is shown
        dialog.FileOk += (sender, e) =>
        {
            e.Cancel = true; // Always cancel actual file selection
        };

        if (dialog.ShowDialog() == true)
        {
            // Get the selected path but remove the filename portion
            return Path.GetDirectoryName(dialog.FileName);
        }

        return null;
    }

// Improved export as individual tiles method (add to MainViewModel)
// Improved export as individual tiles method (add to MainViewModel)
    private async Task ExportAsTilesAsync()
    {
        try
        {
            // Use FolderBrowserDialog to get output directory
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select folder to export tiles",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Status = "Exporting tiles...";
                await Task.Delay(10); // Allow UI to update

                string folderPath = dialog.SelectedPath;

                // Create a subdirectory with timestamp
                string subfolder = $"WFC_Tiles_{DateTime.Now:yyyyMMdd_HHmmss}";
                string fullPath = Path.Combine(folderPath, subfolder);
                Directory.CreateDirectory(fullPath);

                // Export each tile as an individual image
                int count = 0;
                for (int y = 0; y < GridHeight; y++)
                {
                    for (int x = 0; x < GridWidth; x++)
                    {
                        // Find the tile at this position
                        var tile = GridTiles.FirstOrDefault(t =>
                            Math.Abs(t.X - (x * 100)) < 0.1 &&
                            Math.Abs(t.Y - (y * 100)) < 0.1);

                        if (tile != null)
                        {
                            // Create a filename based on coordinates
                            string filename = Path.Combine(fullPath, $"tile_x{x}_y{y}.png");

                            // Create a small canvas with just this tile
                            var canvas = new Canvas { Width = 100, Height = 100 };
                            var image = new Image
                            {
                                Source = tile.Image,
                                Width = 100,
                                Height = 100
                            };
                            canvas.Children.Add(image);

                            // Measure and arrange the canvas
                            canvas.Measure(new Size(100, 100));
                            canvas.Arrange(new Rect(0, 0, 100, 100));

                            // Create a RenderTargetBitmap
                            var renderBitmap = new RenderTargetBitmap(100, 100, 96, 96, PixelFormats.Pbgra32);
                            renderBitmap.Render(canvas);

                            // Save as PNG
                            BitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                            using (var stream = new FileStream(filename, FileMode.Create))
                            {
                                encoder.Save(stream);
                            }

                            count++;
                        }
                    }
                }

                Status = $"Exported {count} tiles to {fullPath}";
            }
        }
        catch (Exception ex)
        {
            Status = $"Error exporting tiles: {ex.Message}";
        }
    }
}