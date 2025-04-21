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
using WFC.Models.NeuralNetwork;
using WFC.Factories.Model;
using Microsoft.ML;
using WFC.Services.ML;

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
        private readonly IModelFactory _modelFactory;

        // Neural network model
        private IQualityAssessmentModel _qualityModel;

        // Cancellation
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource _batchCancellationTokenSource;

        // Training data collector
        private TrainingDataCollector _trainingDataCollector;

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
            IFileSystem fileSystem,
            IModelFactory modelFactory = null)
        {
            _wfcService = wfcService;
            _exporterFactory = exporterFactory;
            _pluginManager = pluginManager;
            _tileConfigManager = tileConfigManager;
            _modelFactory = modelFactory ?? new DefaultModelFactory();

            // Create batch generation service
            _batchGenerationService = new BatchGenerationService(
                _wfcService,
                _exporterFactory,
                _tileConfigManager);

            // Initialize training data collector
            _trainingDataCollector = new TrainingDataCollector();

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

            // Initialize neural network commands
            RateMapCommand = new RelayCommand<string>(RateMap);
            TrainModelCommand = new AsyncRelayCommand(TrainModelAsync);

            // Initialize plugins and tile configuration
            InitializePlugins();

            // Initialize the quality model
            InitializeQualityModel();
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

        // Neural Network Commands
        public ICommand RateMapCommand { get; }
        public ICommand TrainModelCommand { get; }

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

        #region Properties - Neural Network

        // Quality assessment
        private QualityAssessment _qualityAssessment;

        public QualityAssessment QualityAssessment
        {
            get => _qualityAssessment;
            set
            {
                _qualityAssessment = value;
                OnPropertyChanged(nameof(QualityAssessment));
                OnPropertyChanged(nameof(HasQualityAssessment));
                OnPropertyChanged(nameof(QualityScoreDisplay));
                OnPropertyChanged(nameof(QualityScorePercent));
            }
        }

        public bool HasQualityAssessment => _qualityAssessment != null;

        public string QualityScoreDisplay =>
            _qualityAssessment != null ? $"{_qualityAssessment.OverallScore:F2}" : "N/A";

        public int QualityScorePercent => _qualityAssessment != null ? (int)(_qualityAssessment.OverallScore * 100) : 0;

        // Training
        private bool _saveForTraining = true;

        public bool SaveForTraining
        {
            get => _saveForTraining;
            set
            {
                _saveForTraining = value;
                OnPropertyChanged(nameof(SaveForTraining));
            }
        }

        private bool _isTraining = false;

        public bool IsTraining
        {
            get => _isTraining;
            set
            {
                _isTraining = value;
                OnPropertyChanged(nameof(IsTraining));
            }
        }

        private string _trainingStatus = "";

        public string TrainingStatus
        {
            get => _trainingStatus;
            set
            {
                _trainingStatus = value;
                OnPropertyChanged(nameof(TrainingStatus));
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
        /// Initialize the quality assessment model
        /// </summary>
        private void InitializeQualityModel()
        {
            try
            {
                _qualityModel = _modelFactory.CreateModel(ModelType.Advanced);

                // Check for saved model
                string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "quality_model.zip");
                if (File.Exists(modelPath))
                {
                    _qualityModel = _modelFactory.CreateModel(ModelType.Custom, modelPath);
                    Console.WriteLine("Loaded trained quality model");
                }
                else
                {
                    // Use default model
                    _qualityModel = _modelFactory.CreateModel(ModelType.Basic);
                    Console.WriteLine("Using default quality model");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize quality model: {ex.Message}");
                // Fall back to basic model
                _qualityModel = _modelFactory.CreateModel(ModelType.Basic);
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

                        // Assess quality
                        await AssessQuality(result.Grid);

                        // Save for training if enabled
                        if (SaveForTraining)
                        {
                            await _trainingDataCollector.SaveGeneratedMapForTraining(
                                result.Grid, settings, seed?.ToString() ?? "random");
                            Console.WriteLine("Map saved for training");
                        }
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
        /// Assess the quality of a generated map
        /// </summary>
        private async Task AssessQuality(Tile[,] grid)
        {
            try
            {
                // If model is not initialized, initialize it
                if (_qualityModel == null)
                {
                    InitializeQualityModel();
                }

                Status = "Assessing map quality...";

                // Evaluate the map
                QualityAssessment = await _qualityModel.EvaluateAsync(grid);

                // Update status with quality information
                Status = $"Quality assessment: {QualityScoreDisplay} ({QualityScorePercent}%)";

                // Show first feedback item if available
                if (QualityAssessment.Feedback != null && QualityAssessment.Feedback.Length > 0)
                {
                    Status = QualityAssessment.Feedback[0];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error assessing quality: {ex.Message}");
                QualityAssessment = null;
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
            QualityAssessment = null;
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

        #region Methods - Neural Network

        /// <summary>
        /// Rate the current map
        /// </summary>
        private void RateMap(string ratingStr)
        {
            if (!HasCompletedGeneration)
                return;

            if (!int.TryParse(ratingStr, out int rating))
                return;

            // Normalize rating to 0-1 range
            float normalizedRating = rating / 5.0f;

            try
            {
                // Generate map ID
                string mapId = $"map_{DateTime.Now:yyyyMMdd}_{SeedText}";

                // Show immediate feedback
                Status = $"Thank you for rating this map {rating}/5!";

                // Save rating in background thread
                Task.Run(async () =>
                {
                    try
                    {
                        await _trainingDataCollector.AddUserRating(mapId, normalizedRating);
                        Console.WriteLine($"Rating {rating}/5 saved successfully for map {mapId}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving rating: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Status = $"Error processing rating: {ex.Message}";
                Console.WriteLine($"Rating error: {ex}");
            }
        }

        /// <summary>
        /// Train the quality assessment model
        /// </summary>
        private async Task TrainModelAsync()
        {
            try
            {
                IsTraining = true;
                TrainingStatus = "Preparing training data...";

                // Create models directory if it doesn't exist
                string modelsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models");
                Directory.CreateDirectory(modelsDir);

                // Export training data
                string trainingDataPath = Path.Combine(modelsDir, "training_data.json");
                await _trainingDataCollector.ExportTrainingData(trainingDataPath);

                TrainingStatus = "Training model...";
                Status = "Training neural network model...";

                // Train model
                var trainer = new ModelTrainer(trainingDataPath);
                var model = await trainer.TrainModel();

                // Save model
                string modelPath = Path.Combine(modelsDir, "quality_model.zip");
                trainer.SaveModel(model, modelPath);

                // Reinitialize quality model
                _qualityModel = _modelFactory.CreateModel(ModelType.Custom, modelPath);

                TrainingStatus = "Model training completed successfully!";
                Status = "Neural network training completed";
            }
            catch (Exception ex)
            {
                TrainingStatus = $"Training failed: {ex.Message}";
                Status = $"Error training model: {ex.Message}";
                Console.WriteLine($"Error training model: {ex}");
            }
            finally
            {
                IsTraining = false;
            }
        }

        #endregion
    }

    /// <summary>
    /// Training data collector for neural network
    /// </summary>
    public class TrainingDataCollector
    {
        private readonly string _dataFolder;
        private List<TrainingExample> _examples = new List<TrainingExample>();

        public class TrainingExample
        {
            public string MapId { get; set; }
            public string ImagePath { get; set; }
            public string MetadataPath { get; set; }
            public float UserRating { get; set; }
            public Dictionary<string, float> FeatureValues { get; set; }
        }

        public TrainingDataCollector(string dataFolder = null)
        {
            _dataFolder = dataFolder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TrainingData");
            Directory.CreateDirectory(_dataFolder);
            LoadExistingExamples();
        }

        public async Task SaveGeneratedMapForTraining(Tile[,] grid, WFCSettings settings, string seedValue)
        {
            string mapId = $"map_{DateTime.Now:yyyyMMdd_HHmmss}_{seedValue}";
            string subfolder = Path.Combine(_dataFolder, mapId);
            Directory.CreateDirectory(subfolder);

            // Save metadata
            string metadataPath = Path.Combine(subfolder, "metadata.json");
            SaveMapMetadata(grid, settings, metadataPath);

            // Add to examples list for later labeling
            _examples.Add(new TrainingExample
            {
                MapId = mapId,
                ImagePath = "", // Image would be saved separately by export system
                MetadataPath = metadataPath,
                UserRating = 0, // Will be set later by user
                FeatureValues = ExtractFeatures(grid)
            });

            // Save examples list
            await SaveExamplesList();
        }

        private void SaveMapMetadata(Tile[,] grid, WFCSettings settings, string metadataPath)
        {
            // Save relevant metadata
            var metadata = new
            {
                Width = grid.GetLength(0),
                Height = grid.GetLength(1),
                Seed = settings.Seed,
                TileCount = CountTiles(grid),
                TimeGenerated = DateTime.Now.ToString("o")
            };

            string json = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(metadataPath, json);
        }

        private Dictionary<string, int> CountTiles(Tile[,] grid)
        {
            var counts = new Dictionary<string, int>();

            foreach (var tile in grid)
            {
                if (tile != null)
                {
                    string category = tile.Category ?? "unknown";
                    if (!counts.ContainsKey(category))
                        counts[category] = 0;
                    counts[category]++;
                }
            }

            return counts;
        }

        private Dictionary<string, float> ExtractFeatures(Tile[,] grid)
        {
            // Extract features for training
            var features = new Dictionary<string, float>();

            // Calculate basic features
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            int totalTiles = width * height;

            // Count tile types
            var categories = new Dictionary<string, int>();
            foreach (var tile in grid)
            {
                if (tile != null)
                {
                    string category = tile.Category ?? "unknown";
                    if (!categories.ContainsKey(category))
                        categories[category] = 0;
                    categories[category]++;
                }
            }

            // Convert counts to ratios
            foreach (var category in categories)
            {
                features[$"Ratio_{category.Key}"] = (float)category.Value / totalTiles;
            }

            // Variety score (unique tiles / total)
            var uniqueTileIds = new HashSet<string>();
            foreach (var tile in grid)
            {
                if (tile != null)
                    uniqueTileIds.Add(tile.TileId);
            }

            features["VarietyScore"] = (float)uniqueTileIds.Count / totalTiles;

            // Transition count
            int transitions = CountTransitions(grid);
            features["TransitionCount"] = transitions;
            features["TransitionDensity"] = (float)transitions / totalTiles;

            return features;
        }

        private int CountTransitions(Tile[,] grid)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            int transitions = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var current = grid[x, y]?.Category;

                    // Check right
                    if (x < width - 1)
                    {
                        var right = grid[x + 1, y]?.Category;
                        if (current != right && current != null && right != null)
                            transitions++;
                    }

                    // Check down
                    if (y < height - 1)
                    {
                        var down = grid[x, y + 1]?.Category;
                        if (current != down && current != null && down != null)
                            transitions++;
                    }
                }
            }

            return transitions;
        }

        public async Task<bool> AddUserRating(string mapId, float rating)
        {
            // Find matching example
            var example = _examples.FirstOrDefault(e => e.MapId.Contains(mapId));
            if (example == null)
            {
                // Create a new example if not found
                example = new TrainingExample
                {
                    MapId = mapId,
                    UserRating = rating,
                    FeatureValues = new Dictionary<string, float>()
                };
                _examples.Add(example);
            }
            else
            {
                example.UserRating = rating;
            }

            // Save rating to file
            string ratingPath = Path.Combine(_dataFolder, example.MapId, "rating.json");
            if (!Directory.Exists(Path.GetDirectoryName(ratingPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ratingPath));
            }

            await File.WriteAllTextAsync(ratingPath, System.Text.Json.JsonSerializer.Serialize(
                new { Rating = rating }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            // Save examples list
            await SaveExamplesList();

            return true;
        }

        private async Task SaveExamplesList()
        {
            try
            {
                string examplesPath = Path.Combine(_dataFolder, "examples.json");
                string json = System.Text.Json.JsonSerializer.Serialize(
                    _examples, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(examplesPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving examples list: {ex.Message}");
                // Продолжаем выполнение даже при ошибке
            }
        }

        private void LoadExistingExamples()
        {
            string examplesPath = Path.Combine(_dataFolder, "examples.json");
            if (File.Exists(examplesPath))
            {
                try
                {
                    string json = File.ReadAllText(examplesPath);
                    var examples = System.Text.Json.JsonSerializer.Deserialize<List<TrainingExample>>(json);
                    if (examples != null)
                    {
                        _examples = examples;
                        Console.WriteLine($"Loaded {_examples.Count} existing training examples");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading training examples: {ex.Message}");
                    _examples = new List<TrainingExample>();
                }
            }
        }

        public async Task ExportTrainingData(string outputPath)
        {
            // Export all examples with ratings
            var trainingData = _examples.Where(e => e.UserRating > 0).ToList();

            if (trainingData.Count == 0)
            {
                throw new InvalidOperationException(
                    "No rated examples available for training. Please rate some maps first.");
            }

            await File.WriteAllTextAsync(outputPath, System.Text.Json.JsonSerializer.Serialize(
                trainingData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            Console.WriteLine($"Exported {trainingData.Count} training examples to {outputPath}");
        }
    }


}