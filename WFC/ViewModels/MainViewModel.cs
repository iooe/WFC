using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using WFC.Models;
using WFC.Services;

namespace WFC.ViewModels;

// ViewModels/MainViewModel.cs
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

    private int _gridWidth = 5;

    public int GridWidth
    {
        get => _gridWidth;
        set
        {
            _gridWidth = value;
            OnPropertyChanged(nameof(GridWidth));
        }
    }

    private int _gridHeight = 5;

    public int GridHeight
    {
        get => _gridHeight;
        set
        {
            _gridHeight = value;
            OnPropertyChanged(nameof(GridHeight));
        }
    }

// Изменяем тип коллекции для отображения
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

    // Оставляем коллекцию для доступных тайлов
    public ObservableCollection<Tile> Tiles { get; }

    public MainViewModel(IWFCService wfcService)
    {
        _wfcService = wfcService;
        random = new Random();
        _wfcService.ProgressChanged += OnProgressChanged;

        Tiles = new ObservableCollection<Tile>();
        GridTiles = new ObservableCollection<TileDisplay>();  // Инициализируем новую коллекцию
        GenerateCommand = new AsyncRelayCommand(GenerateAsync);
        CancelCommand = new RelayCommand(Cancel);
        ResetCommand = new RelayCommand(Reset);

        InitializeDefaultTiles();
    }

    private void InitializeDefaultTiles()
    {
        // Здания основные
        Tiles.Add(new Tile(0, "Water Full", "water_center_e.png"));
        Tiles.Add(new Tile(1, "Earth", "grass_center_e.png"));
        Tiles.Add(new Tile(2, "Water To Shore", "grass_waterConcave_W.png")); // тайл с водой слева и берегом справа

    }

    private void AddDefaultRules(WFCSettings settings)
    {
        settings.Rules.Clear();

        // Земля (0) может соединяться с землей или водой
        AddRule(settings, 0, "right", new[] { (0, 0.7f), (1, 0.3f) });
        AddRule(settings, 0, "right", new[] { (0, 0.5f), (1, 0.2f), (2, 0.3f) });
        AddRule(settings, 0, "up", new[] { (0, 0.7f), (1, 0.3f) });
        AddRule(settings, 0, "down", new[] { (0, 0.7f), (1, 0.3f) });

        // Вода (1) может соединяться с водой или землей
        AddRule(settings, 1, "right", new[] { (1, 0.7f), (0, 0.3f) });
        AddRule(settings, 1, "left", new[] { (1, 0.5f), (0, 0.2f), (2, 0.3f) });
        AddRule(settings, 1, "up", new[] { (1, 0.7f), (0, 0.3f) });
        AddRule(settings, 1, "down", new[] { (1, 0.7f), (0, 0.3f) });
        
        // Тайл 2 (земля слева, вода справа)
        AddRule(settings, 2, "left", new[] { (0, 1.0f) }); // Слева разрешена только земля
        AddRule(settings, 2, "right", new[] { (1, 1.0f) }); // Справа разрешена только вода
        AddRule(settings, 2, "up", new[] { (0, 0.5f), (1, 0.5f) }); // Вертикальные связи (пример)
        AddRule(settings, 2, "down", new[] { (0, 0.5f), (1, 0.5f) });
    }
    private void AddRule(WFCSettings settings, int fromState, string direction,
        (int state, float weight)[] possibleStates)
    {
        var key = (fromState, direction);
        settings.Rules[key] = possibleStates.ToList();
    }

    private async Task GenerateAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        var settings = new WFCSettings
        {
            Width = GridWidth,
            Height = GridHeight,
            Tiles = Tiles.ToList()
        };

        AddDefaultRules(settings);

        try
        {
            Status = "Generating...";
            bool generationSuccessful = false;
            int attempts = 0;

            while (!generationSuccessful && attempts < 300)
            {
                attempts++;
                Status = $"Generating... (Attempt {attempts}/20)";

                var result = await _wfcService.GenerateAsync(settings, _cancellationTokenSource.Token);

                if (result.Success)
                {
                    UpdateGridDisplay(result.Grid);
                    Status = "Generation completed successfully";
                    generationSuccessful = true;
                }
                else if (attempts >= 20)
                {
                    Status = $"Failed after 20 attempts. Last error: {result.ErrorMessage}";
                }
            }

            if (!generationSuccessful)
            {
                Status = "Generation failed after 10 attempts";
            }
        }
        catch (Exception ex)
        {
            Status = $"Unexpected error: {ex.Message}";
        }
    }

    private void UpdateGridDisplay(Tile[,] grid)
    {
        GridTiles.Clear();
    
        // Константы для гексагональной сетки
        double hexWidth = 100;      
        double hexHeight = 100;    
        double horizontalDistance = hexWidth * 0.53;  // Было 0.75, уменьшили
        double verticalDistance = hexHeight * 0.4;   // Было 0.87, уменьшили

        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                var tile = grid[x, y] ?? Tiles[0];
            
                // Вычисляем позицию для гексагональной сетки
                double xPos = x * horizontalDistance;
                double yPos = y * verticalDistance;
            
                // Смещаем четные ряды
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
        GridTiles.Clear();  // Очищаем коллекцию тайлов для отображения
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