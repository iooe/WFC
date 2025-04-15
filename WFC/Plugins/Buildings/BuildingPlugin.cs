using WFC.Models;

namespace WFC.Plugins.Buildings;

/// <summary>
/// Plugin providing building tiles
/// </summary>
public class BuildingPlugin : ITileSetPlugin, IPostProcessorPlugin, IGenerationHookPlugin
{
    public string Id => "wfc.buildings";
    public string Name => "Buildings";
    public string Version => "1.0";
    public string Description => "Provides building tiles and generation logic";

    public bool Enabled { get; set; }
    
    // Post-processor priority
    public int Priority => 10;

    private List<TileDefinition> _tileDefinitions;
    private List<TileRuleDefinition> _ruleDefinitions;
    private Random _random;

    public void Initialize(IServiceProvider serviceProvider)
    {
        _random = new Random();

        // Create tile definitions
        _tileDefinitions = new List<TileDefinition>
        {
            // Wall tiles
            new()
            {
                Id = "building.wall.middle",
                Name = "Wall Middle",
                Category = "building",
                ResourcePath = "buildings/wall-front-middle",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "false" },
                    { "surface", "wall" }
                }
            },
            new()
            {
                Id = "building.wall.top_left",
                Name = "Wall Top-Left Corner",
                Category = "building",
                ResourcePath = "buildings/wall-front-corner-top-left",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "false" },
                    { "surface", "wall" }
                }
            },
            new()
            {
                Id = "building.wall.top_right",
                Name = "Wall Top-Right Corner",
                Category = "building",
                ResourcePath = "buildings/wall-front-corner-top-right",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "false" },
                    { "surface", "wall" }
                }
            },
            new()
            {
                Id = "building.wall.bottom_left",
                Name = "Wall Bottom-Left Corner",
                Category = "building",
                ResourcePath = "buildings/wall-front-corner-bottom-left",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "false" },
                    { "surface", "wall" }
                }
            },
            new()
            {
                Id = "building.wall.bottom_right",
                Name = "Wall Bottom-Right Corner",
                Category = "building",
                ResourcePath = "buildings/wall-front-corner-bottom-right",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "false" },
                    { "surface", "wall" }
                }
            },
            new()
            {
                Id = "building.wall.top",
                Name = "Wall Top",
                Category = "building",
                ResourcePath = "buildings/wall-front-top-end",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "false" },
                    { "surface", "wall" }
                }
            },
            new()
            {
                Id = "building.wall.bottom",
                Name = "Wall Bottom",
                Category = "building",
                ResourcePath = "buildings/wall-front-bottom-end",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "false" },
                    { "surface", "wall" }
                }
            },
            new()
            {
                Id = "building.wall.left",
                Name = "Wall Left",
                Category = "building",
                ResourcePath = "buildings/wall-front-left-end",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "false" },
                    { "surface", "wall" }
                }
            },
            new()
            {
                Id = "building.wall.right",
                Name = "Wall Right",
                Category = "building",
                ResourcePath = "buildings/wall-front-right-end",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "false" },
                    { "surface", "wall" }
                }
            },

            // Window tiles
            new()
            {
                Id = "building.window.top",
                Name = "Window Top",
                Category = "building",
                ResourcePath = "buildings/window-top",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "false" },
                    { "surface", "window" }
                }
            },
            new()
            {
                Id = "building.window.bottom",
                Name = "Window Bottom",
                Category = "building",
                ResourcePath = "buildings/window-bottom",
                Properties = new Dictionary<string, string>
                {
                    { "walkable", "false" },
                    { "surface", "window" }
                }
            }
        };

        // Create rule definitions
        _ruleDefinitions = CreateRuleDefinitions();
    }

    private List<TileRuleDefinition> CreateRuleDefinitions()
    {
        var rules = new List<TileRuleDefinition>();

        // Wall middle rules
        AddWallMiddleRules(rules);

        // Corner rules
        AddWallCornerRules(rules);

        // Edge rules
        AddWallEdgeRules(rules);

        // Window rules
        AddWindowRules(rules);

        // Connections to terrain
        AddTerrainConnectionRules(rules);

        return rules;
    }

    private void AddWallMiddleRules(List<TileRuleDefinition> rules)
    {
        // Wall middle can connect to other walls and windows
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.middle",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.middle", Weight = 1.0f },
                new() { ToTileId = "building.wall.right", Weight = 0.8f },
                new() { ToTileId = "building.window.top", Weight = 0.6f },
                new() { ToTileId = "building.window.bottom", Weight = 0.6f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.middle",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.middle", Weight = 1.0f },
                new() { ToTileId = "building.wall.left", Weight = 0.8f },
                new() { ToTileId = "building.window.top", Weight = 0.6f },
                new() { ToTileId = "building.window.bottom", Weight = 0.6f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.middle",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.middle", Weight = 1.0f },
                new() { ToTileId = "building.wall.top", Weight = 0.8f },
                new() { ToTileId = "building.window.top", Weight = 0.7f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.middle",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.middle", Weight = 1.0f },
                new() { ToTileId = "building.wall.bottom", Weight = 0.8f },
                new() { ToTileId = "building.window.bottom", Weight = 0.7f }
            }
        });
    }

    private void AddWallCornerRules(List<TileRuleDefinition> rules)
    {
        // Top-left corner rules
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.top_left",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.top", Weight = 1.0f },
                new() { ToTileId = "building.window.top", Weight = 0.7f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.top_left",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.left", Weight = 1.0f },
                new() { ToTileId = "building.wall.middle", Weight = 0.8f }
            }
        });

        // Top-right corner rules
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.top_right",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.top", Weight = 1.0f },
                new() { ToTileId = "building.window.top", Weight = 0.7f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.top_right",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.right", Weight = 1.0f },
                new() { ToTileId = "building.wall.middle", Weight = 0.8f }
            }
        });

        // Bottom-left corner rules
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.bottom_left",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.bottom", Weight = 1.0f },
                new() { ToTileId = "building.window.bottom", Weight = 0.7f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.bottom_left",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.left", Weight = 1.0f },
                new() { ToTileId = "building.wall.middle", Weight = 0.8f }
            }
        });

        // Bottom-right corner rules
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.bottom_right",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.bottom", Weight = 1.0f },
                new() { ToTileId = "building.window.bottom", Weight = 0.7f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.bottom_right",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.right", Weight = 1.0f },
                new() { ToTileId = "building.wall.middle", Weight = 0.8f }
            }
        });
    }

    private void AddWallEdgeRules(List<TileRuleDefinition> rules)
    {
        // Top wall rules
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.top",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.top", Weight = 1.0f },
                new() { ToTileId = "building.wall.top_right", Weight = 0.8f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.top",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.top", Weight = 1.0f },
                new() { ToTileId = "building.wall.top_left", Weight = 0.8f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.top",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.middle", Weight = 1.0f },
                new() { ToTileId = "building.window.top", Weight = 0.8f }
            }
        });

        // Bottom wall rules
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.bottom",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.bottom", Weight = 1.0f },
                new() { ToTileId = "building.wall.bottom_right", Weight = 0.8f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.bottom",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.bottom", Weight = 1.0f },
                new() { ToTileId = "building.wall.bottom_left", Weight = 0.8f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.bottom",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.middle", Weight = 1.0f },
                new() { ToTileId = "building.window.bottom", Weight = 0.8f }
            }
        });

        // Left wall rules
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.left",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.left", Weight = 1.0f },
                new() { ToTileId = "building.wall.bottom_left", Weight = 0.8f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.left",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.left", Weight = 1.0f },
                new() { ToTileId = "building.wall.top_left", Weight = 0.8f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.left",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.middle", Weight = 1.0f },
                new() { ToTileId = "building.window.top", Weight = 0.7f },
                new() { ToTileId = "building.window.bottom", Weight = 0.7f }
            }
        });

        // Right wall rules
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.right",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.right", Weight = 1.0f },
                new() { ToTileId = "building.wall.bottom_right", Weight = 0.8f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.right",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.right", Weight = 1.0f },
                new() { ToTileId = "building.wall.top_right", Weight = 0.8f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.right",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.middle", Weight = 1.0f },
                new() { ToTileId = "building.window.top", Weight = 0.7f },
                new() { ToTileId = "building.window.bottom", Weight = 0.7f }
            }
        });
    }

    private void AddWindowRules(List<TileRuleDefinition> rules)
    {
        // Window top rules
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.window.top",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.middle", Weight = 1.0f },
                new() { ToTileId = "building.wall.right", Weight = 0.8f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.window.top",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.middle", Weight = 1.0f },
                new() { ToTileId = "building.wall.left", Weight = 0.8f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.window.top",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.middle", Weight = 0.9f },
                new() { ToTileId = "building.wall.top", Weight = 0.8f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.window.top",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.window.bottom", Weight = 1.0f }
            }
        });

        // Window bottom rules
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.window.bottom",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.middle", Weight = 1.0f },
                new() { ToTileId = "building.wall.right", Weight = 0.8f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.window.bottom",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.middle", Weight = 1.0f },
                new() { ToTileId = "building.wall.left", Weight = 0.8f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.window.bottom",
            Direction = "up",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.window.top", Weight = 1.0f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.window.bottom",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "building.wall.middle", Weight = 0.9f },
                new() { ToTileId = "building.wall.bottom", Weight = 0.8f }
            }
        });
    }

    private void AddTerrainConnectionRules(List<TileRuleDefinition> rules)
    {
        // Connections from buildings to terrain
        // Wall bottom to grass/pavement
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.bottom",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "grass.basic", Weight = 0.6f },
                new() { ToTileId = "flowers.basic", Weight = 0.5f }
            }
        });

        // Corner bottom-left to terrain
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.bottom_left",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "grass.basic", Weight = 0.6f },
                new() { ToTileId = "flowers.basic", Weight = 0.5f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.bottom_left",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "grass.basic", Weight = 0.6f },
                new() { ToTileId = "flowers.basic", Weight = 0.5f }
            }
        });

        // Corner bottom-right to terrain
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.bottom_right",
            Direction = "down",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "grass.basic", Weight = 0.6f },
                new() { ToTileId = "flowers.basic", Weight = 0.5f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.bottom_right",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "grass.basic", Weight = 0.6f },
                new() { ToTileId = "flowers.basic", Weight = 0.5f }
            }
        });

        // Left/right walls to terrain
        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.left",
            Direction = "left",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "grass.basic", Weight = 0.6f },
                new() { ToTileId = "flowers.basic", Weight = 0.5f }
            }
        });

        rules.Add(new TileRuleDefinition
        {
            FromTileId = "building.wall.right",
            Direction = "right",
            PossibleConnections = new List<TileConnectionWeight>
            {
                new() { ToTileId = "pavement.basic", Weight = 1.0f },
                new() { ToTileId = "grass.basic", Weight = 0.6f },
                new() { ToTileId = "flowers.basic", Weight = 0.5f }
            }
        });
    }

    public IEnumerable<TileDefinition> GetTileDefinitions()
    {
        return _tileDefinitions;
    }

    public IEnumerable<TileRuleDefinition> GetRuleDefinitions()
    {
        return _ruleDefinitions;
    }

    // IGenerationHookPlugin implementation

    public void OnBeforeGeneration(WFCSettings settings)
    {
        // Initialize building map in shared data
        var context = settings.PluginSettings["context"] as GenerationContext;
        if (context != null)
        {
            var buildingMap = new bool[settings.Width, settings.Height];
            context.SharedData["building.map"] = buildingMap;
        }
    }

    public IEnumerable<int> OnBeforeCollapse(int x, int y, IEnumerable<int> possibleStates, GenerationContext context)
    {
        // Безопасное получение карты зданий
        bool[,] buildingMap;
        if (context.SharedData.TryGetValue("building.map", out var buildingData))
        {
            buildingMap = (bool[,])buildingData;
        }
        else
        {
            // Если нет, создаем новую
            buildingMap = new bool[context.Width, context.Height];
            context.SharedData["building.map"] = buildingMap;
        }

        // Проверяем, является ли эта ячейка частью здания
        if (x >= 0 && x < context.Width && y >= 0 && y < context.Height && buildingMap[x, y])
        {
            // Разрешаем только плитки зданий
            var tileIndexMap = context.Settings.TileIndexMap;
            var allowedStates = new List<int>();

            foreach (var state in possibleStates)
            {
                var tile = context.Settings.Tiles[state];
                if (tile.Category == "building")
                {
                    allowedStates.Add(state);
                }
            }

            return allowedStates.Count > 0 ? allowedStates : possibleStates;
        }

        return possibleStates;
    }

    public void OnAfterCollapse(int x, int y, int state, GenerationContext context)
    {
        // Nothing to do here
    }

    public void OnAfterGeneration(Tile[,] grid, GenerationContext context)
    {
        // Generate building areas
        GenerateBuildingAreas(context);
    }

    public Tile[,] OnPostProcess(Tile[,] grid, GenerationContext context)
    {
        // Nothing to do in post-process as a generation hook
        return grid;
    }

    // IPostProcessorPlugin implementation

    public Tile[,] ProcessGrid(Tile[,] grid, GenerationContext context)
    {
        // Apply building processing
        return PostProcessBuildings(grid, context);
    }

    // Helper methods

    private void GenerateBuildingAreas(GenerationContext context)
    {
        int width = context.Width;
        int height = context.Height;

        // Получаем или создаем карту зданий
        bool[,] buildingMap;
        if (context.SharedData.TryGetValue("building.map", out var buildingData))
        {
            buildingMap = (bool[,])buildingData;
        }
        else
        {
            buildingMap = new bool[width, height];
            context.SharedData["building.map"] = buildingMap;
        }

        // Создаем шумовую карту для зданий на основе случайных значений
        double xFreq = 0.15 * Math.Min(15.0 / Math.Max(width, height), 1.0);
        double yFreq = 0.15 * Math.Min(15.0 / Math.Max(width, height), 1.0);

        // Рассчитываем плотность зданий
        double densityFactor = width > 25 || height > 25 ? 0.6 : 0.7;

        // Генерируем "семена" зданий
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Генерируем шум
                double noise = Math.Sin(x * xFreq * 0.7) * Math.Cos(y * yFreq * 0.8) +
                               Math.Sin((x + y) * xFreq * 0.4) * 0.3 +
                               Math.Cos(x * xFreq * 0.5 - y * yFreq * 0.6) * 0.2;

                // Добавляем случайность
                noise += (context.Random.NextDouble() * 0.4);

                // Проверяем пороговое значение
                double threshold = 0.8 * densityFactor;
                buildingMap[x, y] = noise > threshold;

                // Обеспечиваем минимальное расстояние
                if (buildingMap[x, y])
                {
                    EnsureMinimumSpacing(buildingMap, x, y, width, height, context.Random);
                }
            }
        }

        // Выращиваем прямоугольники зданий
        GrowBuildingRectangles(buildingMap, width, height, context.Random);

        // Применяем клеточный автомат для сглаживания
        SmoothBuildingMap(buildingMap, width, height);

        // Заполняем маленькие дыры
        FillSmallHoles(buildingMap, width, height);

        // Сохраняем обновленную карту зданий
        context.SharedData["building.map"] = buildingMap;
    }

    private void EnsureMinimumSpacing(bool[,] buildingMap, int x, int y, int width, int height, Random random)
    {
        int clearRadius = 3;

        for (int dy = -clearRadius; dy <= clearRadius; dy++)
        {
            for (int dx = -clearRadius; dx <= clearRadius; dx++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                // Пропускаем выход за границы
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                // Уменьшаем вероятность смежных зданий
                double distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance < 2)
                {
                    buildingMap[nx, ny] = buildingMap[nx, ny] && random.NextDouble() < 0.3;
                }
            }
        }
    }

    private void GrowBuildingRectangles(bool[,] buildingMap, int width, int height, Random random)
    {
        // Create a copy of the original seeds
        bool[,] buildingSeeds = new bool[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                buildingSeeds[x, y] = buildingMap[x, y];
            }
        }

        // Find and process building clusters
        bool[,] visited = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (buildingSeeds[x, y] && !visited[x, y])
                {
                    // Find connected building seeds
                    var cluster = new List<(int x, int y)>();
                    FloodFill(x, y, buildingSeeds, visited, cluster, width, height);

                    // Process clusters with enough seeds
                    if (cluster.Count >= 2)
                    {
                        ProcessBuildingCluster(cluster, buildingMap, width, height, random);
                    }
                }
            }
        }
    }

    private void ProcessBuildingCluster(List<(int x, int y)> cluster, bool[,] buildingMap,
        int width, int height, Random random)
    {
        // Find bounding box
        int minX = width, maxX = 0, minY = height, maxY = 0;

        foreach (var (cx, cy) in cluster)
        {
            minX = Math.Min(minX, cx);
            maxX = Math.Max(maxX, cx);
            minY = Math.Min(minY, cy);
            maxY = Math.Max(maxY, cy);
        }

        // Expand slightly
        minX = Math.Max(0, minX - 1);
        minY = Math.Max(0, minY - 1);
        maxX = Math.Min(width - 1, maxX + 1);
        maxY = Math.Min(height - 1, maxY + 1);

        // Limit rectangle size
        int rectWidth = maxX - minX + 1;
        int rectHeight = maxY - minY + 1;

        if (rectWidth <= 10 && rectHeight <= 10)
        {
            // Fill the rectangle
            for (int ry = minY; ry <= maxY; ry++)
            {
                for (int rx = minX; rx <= maxX; rx++)
                {
                    buildingMap[rx, ry] = true;
                }
            }
        }
    }

    private void FloodFill(int x, int y, bool[,] map, bool[,] visited,
        List<(int x, int y)> cluster, int width, int height)
    {
        if (x < 0 || y < 0 || x >= width || y >= height || visited[x, y] || !map[x, y])
            return;

        visited[x, y] = true;
        cluster.Add((x, y));

        // Check 4-connected neighbors
        FloodFill(x + 1, y, map, visited, cluster, width, height);
        FloodFill(x - 1, y, map, visited, cluster, width, height);
        FloodFill(x, y + 1, map, visited, cluster, width, height);
        FloodFill(x, y - 1, map, visited, cluster, width, height);
    }

    private void SmoothBuildingMap(bool[,] buildingMap, int width, int height)
    {
        bool[,] temp = new bool[width, height];

        for (int pass = 0; pass < 3; pass++)
        {
            // Copy current state
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    temp[x, y] = buildingMap[x, y];
                }
            }

            // Apply cellular automata rules
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    // Count neighbors
                    int adjNeighbors = 0; // Adjacent (NSEW)
                    int diagNeighbors = 0; // Diagonal

                    // Adjacent
                    if (buildingMap[x, y - 1]) adjNeighbors++;
                    if (buildingMap[x, y + 1]) adjNeighbors++;
                    if (buildingMap[x - 1, y]) adjNeighbors++;
                    if (buildingMap[x + 1, y]) adjNeighbors++;

                    // Diagonal
                    if (buildingMap[x - 1, y - 1]) diagNeighbors++;
                    if (buildingMap[x + 1, y - 1]) diagNeighbors++;
                    if (buildingMap[x - 1, y + 1]) diagNeighbors++;
                    if (buildingMap[x + 1, y + 1]) diagNeighbors++;

                    // Apply rules
                    if (buildingMap[x, y])
                    {
                        // Keep building if well supported
                        temp[x, y] = adjNeighbors >= 2 || (adjNeighbors >= 1 && diagNeighbors >= 2);
                    }
                    else
                    {
                        // Add new building if surrounded
                        temp[x, y] = adjNeighbors >= 3 || (adjNeighbors >= 2 && diagNeighbors >= 2);
                    }

                    // Ensure buildings aren't too close to edges
                    if (x < 2 || y < 2 || x >= width - 2 || y >= height - 2)
                    {
                        temp[x, y] = false;
                    }
                }
            }

            // Copy back
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    buildingMap[x, y] = temp[x, y];
                }
            }
        }
    }

    private void FillSmallHoles(bool[,] buildingMap, int width, int height)
    {
        bool[,] temp = new bool[width, height];

        // Copy original
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                temp[x, y] = buildingMap[x, y];
            }
        }

        // Find and fill small holes
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                if (!buildingMap[x, y])
                {
                    // Count surrounding building tiles
                    int surroundCount = 0;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            if (buildingMap[x + dx, y + dy])
                                surroundCount++;
                        }
                    }

                    // If surrounded on at least 5 sides, fill the hole
                    if (surroundCount >= 5)
                    {
                        temp[x, y] = true;
                    }
                }
            }
        }

        // Copy back
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                buildingMap[x, y] = temp[x, y];
            }
        }
    }

    private Tile[,] PostProcessBuildings(Tile[,] grid, GenerationContext context)
    {
        int width = context.Width;
        int height = context.Height;

        // Получаем карту зданий
        bool[,] buildingMap;
        if (context.SharedData.TryGetValue("building.map", out var buildingData))
        {
            buildingMap = (bool[,])buildingData;
        }
        else
        {
            // Если карта не существует, ничего не делаем
            return grid;
        }

        // Создаем копию сетки
        var result = new Tile[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = grid[x, y];
            }
        }

        // Обрабатываем области зданий
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (buildingMap[x, y])
                {
                    // Определяем подходящую плитку здания
                    result[x, y] = DetermineBuildingTile(x, y, buildingMap, result, context);
                }
            }
        }

        // Обеспечиваем согласованность зданий
        EnsureBuildingCoherence(result, width, height, context);

        return result;
    }

    private Tile DetermineBuildingTile(int x, int y, bool[,] buildingMap, Tile[,] grid, GenerationContext context)
    {
        int width = context.Width;
        int height = context.Height;
        var tileIndexMap = context.Settings.TileIndexMap;

        // Check neighbors
        bool hasTopNeighbor = y > 0 && buildingMap[x, y - 1];
        bool hasRightNeighbor = x < width - 1 && buildingMap[x + 1, y];
        bool hasBottomNeighbor = y < height - 1 && buildingMap[x, y + 1];
        bool hasLeftNeighbor = x > 0 && buildingMap[x - 1, y];

        // Determine the tile type based on neighbors
        string tileId;

        if (!hasTopNeighbor && hasBottomNeighbor) // Top edge
        {
            if (!hasLeftNeighbor && hasRightNeighbor) // Top-left corner
                tileId = "building.wall.top_left";
            else if (hasLeftNeighbor && !hasRightNeighbor) // Top-right corner
                tileId = "building.wall.top_right";
            else // Middle top
                tileId = "building.wall.top";
        }
        else if (hasTopNeighbor && !hasBottomNeighbor) // Bottom edge
        {
            if (!hasLeftNeighbor && hasRightNeighbor) // Bottom-left corner
                tileId = "building.wall.bottom_left";
            else if (hasLeftNeighbor && !hasRightNeighbor) // Bottom-right corner
                tileId = "building.wall.bottom_right";
            else // Middle bottom
                tileId = "building.wall.bottom";
        }
        else if (hasTopNeighbor && hasBottomNeighbor) // Middle row
        {
            if (!hasLeftNeighbor && hasRightNeighbor) // Left edge
                tileId = "building.wall.left";
            else if (hasLeftNeighbor && !hasRightNeighbor) // Right edge
                tileId = "building.wall.right";
            else // Interior
            {
                // Choose between wall and window
                if (context.Random.NextDouble() < 0.2)
                {
                    tileId = "building.window.top";

                    // Ensure we have space for bottom window part
                    if (y < height - 1 && buildingMap[x, y + 1])
                    {
                        // Mark bottom part for window
                        grid[x, y + 1] = context.Settings.Tiles[tileIndexMap["building.window.bottom"]];
                    }
                    else
                    {
                        tileId = "building.wall.middle";
                    }
                }
                else
                {
                    tileId = "building.wall.middle";
                }
            }
        }
        else // Isolated or highly irregular case
        {
            tileId = "building.wall.middle";
        }

        return context.Settings.Tiles[tileIndexMap[tileId]];
    }

    private void EnsureBuildingCoherence(Tile[,] grid, int width, int height, GenerationContext context)
    {
        var tileIndexMap = context.Settings.TileIndexMap;

        // Fix window pairs
        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Check for window top
                if (grid[x, y].TileId == "building.window.top")
                {
                    // Ensure window bottom is below
                    if (y < height - 1)
                    {
                        grid[x, y + 1] = context.Settings.Tiles[tileIndexMap["building.window.bottom"]];
                    }
                }
                // Check for window bottom without top
                else if (grid[x, y].TileId == "building.window.bottom" && y > 0 &&
                         grid[x, y - 1].TileId != "building.window.top")
                {
                    // Replace with wall
                    grid[x, y] = context.Settings.Tiles[tileIndexMap["building.wall.middle"]];
                }
            }
        }

        // Fix wall corners and edges
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Process specific problematic cases
                if (grid[x, y].TileId == "building.wall.top_left")
                {
                    // Ensure right has appropriate tiles
                    if (x < width - 1 && IsBuildingTile(grid[x + 1, y].TileId) &&
                        grid[x + 1, y].TileId != "building.wall.top")
                    {
                        grid[x + 1, y] = context.Settings.Tiles[tileIndexMap["building.wall.top"]];
                    }

                    // Ensure below has appropriate tiles
                    if (y < height - 1 && IsBuildingTile(grid[x, y + 1].TileId) &&
                        grid[x, y + 1].TileId != "building.wall.left")
                    {
                        grid[x, y + 1] = context.Settings.Tiles[tileIndexMap["building.wall.left"]];
                    }
                }

                // Similar logic for other corner/edge types
                // ...
            }
        }
    }

    private bool IsBuildingTile(string tileId)
    {
        return tileId.StartsWith("building.");
    }
}