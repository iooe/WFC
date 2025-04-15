using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WFC.Models;
using WFC.Services;
using WFC.Factories;
using WFC.Services.Export;
using WFC.Plugins;

namespace WFC.ViewModels;

/// <summary>
/// Main view model for the application
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly IWFCService _wfcService;
    private readonly IExporterFactory _exporterFactory;
    private readonly PluginManager _pluginManager;
    private readonly TileConfigManager _tileConfigManager;
    private CancellationTokenSource _cancellationTokenSource;

    public ICommand ApplyPluginChangesCommand { get; }
    /// <summary>
    /// Apply all plugin changes and refresh configuration
    /// </summary>
    private void ApplyPluginChanges()
    {
        // Перезагружаем все правила и конфигурации плиток
        _pluginManager.RefreshTileDefinitions();
        _tileConfigManager.Initialize();
    
        // Обновляем список доступных плиток
        UpdateAvailableTiles();
    
        // Сохраняем настройки плагинов
        _pluginManager.SavePluginPreferences();
    
        Status = "Plugin changes applied successfully";
        Console.WriteLine("Plugin changes applied");
    }
    
    public event PropertyChangedEventHandler PropertyChanged;

    // Commands
    public ICommand GenerateCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand ExportAsPngCommand { get; }
    public ICommand ExportAsTilesCommand { get; }
    public ICommand TogglePluginCommand { get; }

    // Progress tracking
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

    // Status message
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

    // Grid dimensions
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

    // Debug rendering
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

    // Random seed
    private string _seedText = "";
    public string SeedText
    {
        get => _seedText;
        set
        {
            _seedText = value;
            OnPropertyChanged(nameof(SeedText));
        }
    }

    // Generated tiles
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

    // Available tiles
    public ObservableCollection<Tile> Tiles { get; }

    // Available plugins
    public ObservableCollection<PluginViewModel> AvailablePlugins { get; }

    // Constructor
    public MainViewModel(
        IWFCService wfcService, 
        IExporterFactory exporterFactory,
        PluginManager pluginManager,
        TileConfigManager tileConfigManager)
    {
        _wfcService = wfcService;
        _exporterFactory = exporterFactory;
        _pluginManager = pluginManager;
        _tileConfigManager = tileConfigManager;
        
        _wfcService.ProgressChanged += OnProgressChanged;

        Tiles = new ObservableCollection<Tile>();
        GridTiles = new ObservableCollection<TileDisplay>();
        AvailablePlugins = new ObservableCollection<PluginViewModel>();
        
        // Initialize commands
        GenerateCommand = new AsyncRelayCommand(GenerateAsync);
        CancelCommand = new RelayCommand(Cancel);
        ResetCommand = new RelayCommand(Reset);
        ExportAsPngCommand = new AsyncRelayCommand(ExportAsPngAsync);
        ExportAsTilesCommand = new AsyncRelayCommand(ExportAsTilesAsync);
        TogglePluginCommand = new RelayCommand<PluginViewModel>(TogglePlugin);
        ApplyPluginChangesCommand = new RelayCommand(ApplyPluginChanges);

        // Initialize plugins and tile configuration
        InitializePlugins();
    }

    /// <summary>
    /// Initialize plugins and tile configuration
    /// </summary>
    private void InitializePlugins()
    {
        try
        {
            Status = "Loading plugins...";
            
            // Load plugins
            _pluginManager.LoadPlugins();
            
            // Initialize tile configuration
            _tileConfigManager.Initialize();
            
            // Update available tiles
            UpdateAvailableTiles();
            
            // Update available plugins list
            UpdatePluginsList();
            
            Status = $"Loaded {_pluginManager.Plugins.Count} plugins with {Tiles.Count} tiles";
        }
        catch (Exception ex)
        {
            Status = $"Error loading plugins: {ex.Message}";
            Console.WriteLine($"Error loading plugins: {ex}");
        }
    }

    /// <summary>
    /// Update the list of available tiles from configuration
    /// </summary>
    private void UpdateAvailableTiles()
    {
        Tiles.Clear();
        
        // Create settings to get tile instances
        var settings = _tileConfigManager.CreateSettings(1, 1);
        
        // Add all tiles to the list
        foreach (var tile in settings.Tiles)
        {
            Tiles.Add(tile);
        }
    }

    /// <summary>
    /// Update the list of available plugins
    /// </summary>
    private void UpdatePluginsList()
    {
        AvailablePlugins.Clear();
        
        foreach (var plugin in _pluginManager.Plugins)
        {
            AvailablePlugins.Add(new PluginViewModel(plugin));
        }
    }

    /// <summary>
    /// Toggle a plugin's enabled state
    /// </summary>
    private void TogglePlugin(PluginViewModel pluginViewModel)
    {
        if (pluginViewModel != null)
        {
            // Запоминаем старое состояние для протоколирования
            bool wasEnabled = pluginViewModel.Enabled;
        
            // Изменяем состояние плагина через PluginManager
            _pluginManager.TogglePlugin(pluginViewModel.Id, pluginViewModel.Enabled);
        
            // Важно: явно переинициализируем конфигурацию плиток
            _tileConfigManager.Initialize();
        
            // Обновляем список доступных плиток
            UpdateAvailableTiles();
        
            // Протоколируем изменения
            Status = $"Plugin '{pluginViewModel.Name}' {(pluginViewModel.Enabled ? "enabled" : "disabled")}";
            Console.WriteLine($"Plugin {pluginViewModel.Name} toggled from {wasEnabled} to {pluginViewModel.Enabled}");
        }
    }

    /// <summary>
    /// Generate a new WFC grid
    /// </summary>
    private async Task GenerateAsync()
    {
        try
        {
            // Clear any previous generation
            Reset();

            // Create a new cancellation token source
            _cancellationTokenSource = new CancellationTokenSource();

            // Limit grid size to reasonable values
            int safeGridWidth = Math.Min(_gridWidth, 256);
            int safeGridHeight = Math.Min(_gridHeight, 256);

            if (safeGridWidth != _gridWidth || safeGridHeight != _gridHeight)
            {
                GridWidth = safeGridWidth;
                GridHeight = safeGridHeight;
                Status = $"Grid size limited to {safeGridWidth}x{safeGridHeight} for stability";
                await Task.Delay(10); // Allow UI to update
            }

            // Parse seed if provided
            int? seed = null;
            if (!string.IsNullOrWhiteSpace(SeedText))
            {
                if (int.TryParse(SeedText, out int parsedSeed))
                {
                    seed = parsedSeed;
                }
                else
                {
                    // Use string hash as seed
                    seed = SeedText.GetHashCode();
                }
            }

            // Create settings for generation
            var settings = _tileConfigManager.CreateSettings(
                safeGridWidth, 
                safeGridHeight, 
                IsAnimationEnabled,
                seed);

            Status = "Starting WFC Generation...";
            await Task.Delay(10); // Allow UI to update

            try
            {
                // Use a generous timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
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
                    Status = "Generation timed out";
                }
            }
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Update the grid display with generated tiles
    /// </summary>
    private async Task UpdateGridDisplay(Tile[,] grid, WFCSettings settings)
    {
        GridTiles.Clear();

        // If animation is disabled, add all tiles at once
        if (!IsAnimationEnabled)
        {
            for (int y = 0; y < settings.Height; y++)
            {
                for (int x = 0; x < settings.Width; x++)
                {
                    var tile = grid[x, y] ?? settings.Tiles[0]; // Default to first tile if null

                    // Create a new TileDisplay with position
                    double xPos = x * 100;
                    double yPos = y * 100;
                    GridTiles.Add(new TileDisplay(tile, (float)xPos, (float)yPos));
                }
            }
        }
        else
        {
            // Animated rendering
            await AnimateTilesDisplay(grid, settings);
        }
    }

    /// <summary>
    /// Animate the rendering of tiles
    /// </summary>
    private async Task AnimateTilesDisplay(Tile[,] grid, WFCSettings settings)
    {
        // Calculate delay based on grid size
        int totalTiles = settings.Width * settings.Height;
        int baseDelay = 1; // Base delay in milliseconds
        int delay = Math.Max(1, baseDelay * 100 / totalTiles * 10);

        Status = "Animating WFC algorithm steps...";

        // First phase: initial grid generation
        Status = "Phase 1: Initial WFC grid generation...";
        await Task.Delay(500); // Short delay for reading status

        // Generate tiles sequentially from left to right, top to bottom
        for (int y = 0; y < settings.Height; y++)
        {
            for (int x = 0; x < settings.Width; x++)
            {
                var tile = grid[x, y] ?? settings.Tiles[0]; // Default to first tile if null

                // Calculate position using regular grid layout
                double xPos = x * 100;
                double yPos = y * 100;

                // Create a new TileDisplay
                GridTiles.Add(new TileDisplay(tile, (float)xPos, (float)yPos));

                // Add delay between tiles for visualization
                await Task.Delay(delay);
            }

            // Update status every few rows
            if (y % 5 == 0 || y == settings.Height - 1)
            {
                Status = $"Phase 1: Generating row {y + 1}/{settings.Height}...";
            }
        }

        Status = "Generation visualization completed";
    }

    /// <summary>
    /// Cancel the current generation
    /// </summary>
    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        Status = "Operation cancelled";
    }

    /// <summary>
    /// Reset the grid
    /// </summary>
    private void Reset()
    {
        _wfcService.Reset();
        GridTiles.Clear();
        Progress = 0;
        Status = "Ready";
    }

    /// <summary>
    /// Handle progress updates from the WFC service
    /// </summary>
    private void OnProgressChanged(object sender, WFCProgressEventArgs e)
    {
        Progress = e.Progress;
        Status = e.Status;
    }

    /// <summary>
    /// Notify property changed
    /// </summary>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Export the grid as a PNG image
    /// </summary>
    private async Task ExportAsPngAsync()
    {
        try
        {
            Status = "Exporting map as PNG...";
            await Task.Delay(10); // Allow UI to update

            // Create PNG exporter
            var exporter = _exporterFactory.CreateExporter(ExportType.Png);

            // Export the grid
            var result = await exporter.ExportAsync(GridTiles, GridWidth, GridHeight);

            // Update status
            Status = result;
        }
        catch (Exception ex)
        {
            Status = $"Error exporting map: {ex.Message}";
        }
    }

    /// <summary>
    /// Export the grid as individual tiles
    /// </summary>
    private async Task ExportAsTilesAsync()
    {
        try
        {
            Status = "Exporting tiles...";
            await Task.Delay(10); // Allow UI to update

            // Create tiles exporter
            var exporter = _exporterFactory.CreateExporter(ExportType.Tiles);

            // Export the grid
            var result = await exporter.ExportAsync(GridTiles, GridWidth, GridHeight);

            // Update status
            Status = result;
        }
        catch (Exception ex)
        {
            Status = $"Error exporting tiles: {ex.Message}";
        }
    }
}