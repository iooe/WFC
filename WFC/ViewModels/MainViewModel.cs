using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WFC.Models;
using WFC.Services;
using WFC.Services.Export;
using WFC.Plugins;
using WFC.Services.BatchGeneration;

namespace WFC.ViewModels
{
    /// <summary>
    /// Main view model for the application
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Fields

        // Services
        private readonly IWFCService _wfcService;
        private readonly IExporterFactory _exporterFactory;
        private readonly PluginManager _pluginManager;
        private readonly TileConfigManager _tileConfigManager;
        private readonly BatchGenerationService _batchGenerationService;

        // Cancellation
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource _batchCancellationTokenSource;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize the view model with required services
        /// </summary>
        public MainViewModel(
            IWFCService wfcService,
            IExporterFactory exporterFactory,
            PluginManager pluginManager,
            TileConfigManager tileConfigManager,
            IDialogService dialogService,
            IFileSystem fileSystem)
        {
            _wfcService = wfcService;
            _exporterFactory = exporterFactory;
            _pluginManager = pluginManager;
            _tileConfigManager = tileConfigManager;

            // Create batch generation service
            _batchGenerationService = new BatchGenerationService(
                _wfcService,
                _exporterFactory,
                _tileConfigManager);

            // Subscribe to events
            _wfcService.ProgressChanged += OnProgressChanged;
            _batchGenerationService.BatchProgressChanged += OnBatchProgressChanged;

            // Initialize collections
            Tiles = new ObservableCollection<Tile>();
            GridTiles = new ObservableCollection<TileDisplay>();
            AvailablePlugins = new ObservableCollection<PluginViewModel>();

            // Initialize commands for main functionality
            GenerateCommand = new AsyncRelayCommand(GenerateAsync);
            CancelCommand = new RelayCommand(Cancel);
            ResetCommand = new RelayCommand(Reset);
            ExportAsPngCommand = new AsyncRelayCommand(ExportAsPngAsync);
            ExportAsTilesCommand = new AsyncRelayCommand(ExportAsTilesAsync);
            TogglePluginCommand = new RelayCommand<PluginViewModel>(TogglePlugin);
            ApplyPluginChangesCommand = new RelayCommand(ApplyPluginChanges);

            // Initialize zoom commands
            ZoomInCommand = new RelayCommand(ZoomIn);
            ZoomOutCommand = new RelayCommand(ZoomOut);
            ResetZoomCommand = new RelayCommand(ResetZoom);

            // Initialize batch generation commands
            GenerateBatchCommand = new AsyncRelayCommand(GenerateBatchAsync);
            CancelBatchCommand = new RelayCommand(CancelBatch);
            BrowseBatchExportPathCommand = new RelayCommand(() => BrowseBatchExportPath(dialogService));
            OpenBatchExportFolderCommand = new RelayCommand(OpenBatchExportFolder);
            IncreaseBatchMapCountCommand = new RelayCommand(() => BatchMapCount++);
            DecreaseBatchMapCountCommand = new RelayCommand(() => BatchMapCount--);

            // Initialize plugins and tile configuration
            InitializePlugins();
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notify property changed
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Commands

        // Main Commands
        public ICommand GenerateCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand ExportAsPngCommand { get; }
        public ICommand ExportAsTilesCommand { get; }
        public ICommand TogglePluginCommand { get; }
        public ICommand ApplyPluginChangesCommand { get; }

        // Zoom Commands
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ResetZoomCommand { get; }

        // Batch Generation Commands
        public ICommand GenerateBatchCommand { get; }
        public ICommand CancelBatchCommand { get; }
        public ICommand BrowseBatchExportPathCommand { get; }
        public ICommand OpenBatchExportFolderCommand { get; }
        public ICommand IncreaseBatchMapCountCommand { get; }
        public ICommand DecreaseBatchMapCountCommand { get; }

        #endregion

        #region Properties - Main

        // Collections
        public ObservableCollection<Tile> Tiles { get; }
        public ObservableCollection<TileDisplay> GridTiles { get; set; }
        public ObservableCollection<PluginViewModel> AvailablePlugins { get; }

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
        private bool _isAnimationEnabled = false;

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

        // Generation completion
        private bool _hasCompletedGeneration = false;

        public bool HasCompletedGeneration
        {
            get => _hasCompletedGeneration;
            set
            {
                _hasCompletedGeneration = value;
                OnPropertyChanged(nameof(HasCompletedGeneration));
            }
        }

        #endregion

        #region Properties - Zoom

        private double _zoomLevel = 1.0;

        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                if (Math.Abs(_zoomLevel - value) > 0.01)
                {
                    _zoomLevel = Math.Clamp(value, 0.1, 5.0); // Limit zoom between 10% and 500%
                    OnPropertyChanged(nameof(ZoomLevel));
                    OnPropertyChanged(nameof(ZoomPercentage));
                }
            }
        }

        public string ZoomPercentage => $"{ZoomLevel * 100:0}%";

        #endregion

        #region Properties - Batch Generation

        private int _batchMapCount = 10;

        public int BatchMapCount
        {
            get => _batchMapCount;
            set
            {
                _batchMapCount = Math.Max(1, Math.Min(100, value)); // Limit between 1 and 100
                OnPropertyChanged(nameof(BatchMapCount));
            }
        }

        private int _batchGridWidth = 32;

        public int BatchGridWidth
        {
            get => _batchGridWidth;
            set
            {
                _batchGridWidth = Math.Max(8, Math.Min(128, value)); // Limit between 8 and 128
                OnPropertyChanged(nameof(BatchGridWidth));
            }
        }

        private int _batchGridHeight = 32;

        public int BatchGridHeight
        {
            get => _batchGridHeight;
            set
            {
                _batchGridHeight = Math.Max(8, Math.Min(128, value)); // Limit between 8 and 128
                OnPropertyChanged(nameof(BatchGridHeight));
            }
        }

        private bool _batchUseSeed = false;

        public bool BatchUseSeed
        {
            get => _batchUseSeed;
            set
            {
                _batchUseSeed = value;
                OnPropertyChanged(nameof(BatchUseSeed));
            }
        }

        private string _batchSeedValue = "";

        public string BatchSeedValue
        {
            get => _batchSeedValue;
            set
            {
                _batchSeedValue = value;
                OnPropertyChanged(nameof(BatchSeedValue));
            }
        }

        private int _batchExportFormat = 0; // 0 = PNG, 1 = Tiles, 2 = Both

        public int BatchExportFormat
        {
            get => _batchExportFormat;
            set
            {
                _batchExportFormat = value;
                OnPropertyChanged(nameof(BatchExportFormat));
            }
        }

        private string _batchExportPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WFC Maps");

        public string BatchExportPath
        {
            get => _batchExportPath;
            set
            {
                _batchExportPath = value;
                OnPropertyChanged(nameof(BatchExportPath));
            }
        }

        private float _batchProgress = 0;

        public float BatchProgress
        {
            get => _batchProgress;
            set
            {
                _batchProgress = value;
                OnPropertyChanged(nameof(BatchProgress));
            }
        }

        private string _batchStatus = "Ready for batch generation";

        public string BatchStatus
        {
            get => _batchStatus;
            set
            {
                _batchStatus = value;
                OnPropertyChanged(nameof(BatchStatus));
            }
        }

        private bool _hasBatchResults = false;

        public bool HasBatchResults
        {
            get => _hasBatchResults;
            set
            {
                _hasBatchResults = value;
                OnPropertyChanged(nameof(HasBatchResults));
            }
        }

        private int _batchTotalMapsGenerated = 0;

        public int BatchTotalMapsGenerated
        {
            get => _batchTotalMapsGenerated;
            set
            {
                _batchTotalMapsGenerated = value;
                OnPropertyChanged(nameof(BatchTotalMapsGenerated));
            }
        }

        private string _batchElapsedTime = "";

        public string BatchElapsedTime
        {
            get => _batchElapsedTime;
            set
            {
                _batchElapsedTime = value;
                OnPropertyChanged(nameof(BatchElapsedTime));
            }
        }

        private string _batchStatusMessage = "";

        public string BatchStatusMessage
        {
            get => _batchStatusMessage;
            set
            {
                _batchStatusMessage = value;
                OnPropertyChanged(nameof(BatchStatusMessage));
            }
        }

        #endregion

        #region Methods - Main

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
                // Remember old state for logging
                bool wasEnabled = pluginViewModel.Enabled;

                // Change the plugin state through PluginManager
                _pluginManager.TogglePlugin(pluginViewModel.Id, pluginViewModel.Enabled);

                // Important: explicitly reinitialize tile configuration
                _tileConfigManager.Initialize();

                // Update the list of available tiles
                UpdateAvailableTiles();

                // Log changes
                Status = $"Plugin '{pluginViewModel.Name}' {(pluginViewModel.Enabled ? "enabled" : "disabled")}";
                Console.WriteLine(
                    $"Plugin {pluginViewModel.Name} toggled from {wasEnabled} to {pluginViewModel.Enabled}");
            }
        }

        /// <summary>
        /// Apply all plugin changes and refresh configuration
        /// </summary>
        private void ApplyPluginChanges()
        {
            // Reload all tile rules and configurations
            _pluginManager.RefreshTileDefinitions();
            _tileConfigManager.Initialize();

            // Update the list of available tiles
            UpdateAvailableTiles();

            // Save plugin settings
            _pluginManager.SavePluginPreferences();

            Status = "Plugin changes applied successfully";
            Console.WriteLine("Plugin changes applied");
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
                        HasCompletedGeneration = true;
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

        #endregion

        #region Methods - Zoom

        // Zoom methods
        private void ZoomIn()
        {
            ZoomLevel += 0.1;
        }

        private void ZoomOut()
        {
            ZoomLevel -= 0.1;
        }

        private void ResetZoom()
        {
            ZoomLevel = 1.0;
        }

        #endregion

        #region Methods - Batch Generation

        /// <summary>
        /// Generate a batch of maps
        /// </summary>
        private async Task GenerateBatchAsync()
        {
            try
            {
                // Create parameters
                var parameters = new BatchGenerationParameters
                {
                    MapCount = BatchMapCount,
                    Width = BatchGridWidth,
                    Height = BatchGridHeight,
                    UseSeed = BatchUseSeed,
                    ExportFormat = (ExportFormat)BatchExportFormat
                };

                // Parse seed if provided
                if (BatchUseSeed && !string.IsNullOrWhiteSpace(BatchSeedValue))
                {
                    if (int.TryParse(BatchSeedValue, out int seed))
                    {
                        parameters.Seed = seed;
                    }
                    else
                    {
                        // Use string hash as seed
                        parameters.Seed = BatchSeedValue.GetHashCode();
                    }
                }

                // Reset progress and status
                HasBatchResults = false;
                BatchProgress = 0;
                BatchStatus = "Starting batch generation...";

                // Create cancellation token source
                _batchCancellationTokenSource = new CancellationTokenSource();

                // Generate maps
                var result = await _batchGenerationService.GenerateBatchAsync(
                    parameters,
                    BatchExportPath,
                    _batchCancellationTokenSource.Token);

                // Update results
                BatchTotalMapsGenerated = result.GeneratedMaps.Count;
                BatchElapsedTime = $"{result.ElapsedTime.TotalMinutes:F1} minutes";
                BatchStatusMessage = result.Message;
                HasBatchResults = true;

                // Update status
                BatchStatus = result.Success ? "Batch generation completed successfully" : result.Message;
            }
            catch (Exception ex)
            {
                BatchStatus = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Cancel batch generation
        /// </summary>
        private void CancelBatch()
        {
            _batchCancellationTokenSource?.Cancel();
            BatchStatus = "Batch generation cancelled";
        }

        /// <summary>
        /// Browse for export path
        /// </summary>
        private void BrowseBatchExportPath(IDialogService dialogService)
        {
            var path = dialogService.ShowFolderBrowserDialog("Select folder to export maps");
            if (!string.IsNullOrEmpty(path))
            {
                BatchExportPath = path;
            }
        }

        /// <summary>
        /// Open export folder in explorer
        /// </summary>
        private void OpenBatchExportFolder()
        {
            if (Directory.Exists(BatchExportPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = BatchExportPath,
                    UseShellExecute = true
                });
            }
            else
            {
                BatchStatus = "Export folder does not exist";
            }
        }

        /// <summary>
        /// Handle batch progress updates
        /// </summary>
        private void OnBatchProgressChanged(object sender, BatchProgressEventArgs e)
        {
            BatchProgress = e.Progress;
            BatchStatus = e.Status;
        }

        #endregion
    }
}