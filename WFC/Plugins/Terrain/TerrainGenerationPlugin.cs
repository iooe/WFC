using WFC.Models;

namespace WFC.Plugins.Terrain;

/// <summary>
/// Plugin for terrain generation and post-processing
/// </summary>
public class TerrainGenerationPlugin : IGenerationHookPlugin, IPostProcessorPlugin
{
    public string Id => "wfc.terrain";
    public string Name => "Terrain Generator";
    public string Version => "1.0";
    public string Description => "Provides advanced terrain generation";
    public bool Enabled { get; set; }

    // Post-processor priority - run before building plugin
    public int Priority => 5;

    private Random _random;

    public void Initialize(IServiceProvider serviceProvider)
    {
        _random = new Random();
    }

    public void OnBeforeGeneration(WFCSettings settings)
    {
        // Initialize terrain maps in context
        var context = settings.PluginSettings["context"] as GenerationContext;
        if (context != null)
        {
            // Create terrain noise map
            var noiseMap = GenerateNoiseMap(settings.Width, settings.Height, context.Random);
            context.SharedData["terrain.noise"] = noiseMap;

            // Create flower areas map
            var flowerMap = GenerateFlowerMap(noiseMap, settings.Width, settings.Height, context.Random);
            context.SharedData["terrain.flowers"] = flowerMap;

            // Create pavement pattern map
            var pavementMap = GeneratePavementMap(settings.Width, settings.Height, context.Random);
            context.SharedData["terrain.pavement"] = pavementMap;
        }
    }


    public IEnumerable<int> OnBeforeCollapse(int x, int y, IEnumerable<int> possibleStates, GenerationContext context)
    {
        // Получаем карту индексов тайлов
        var tileIndexMap = context.Settings.TileIndexMap;

        // Безопасное получение карты шума
        float[,] noiseMap;
        if (context.SharedData.TryGetValue("terrain.noise", out var noiseData))
        {
            noiseMap = (float[,])noiseData;
        }
        else
        {
            // Если нет, создаем новую
            noiseMap = new float[context.Width, context.Height];
            context.SharedData["terrain.noise"] = noiseMap;
        }

        // Безопасное получение карты цветов
        bool[,] flowerMap;
        if (context.SharedData.TryGetValue("terrain.flowers", out var flowerData))
        {
            flowerMap = (bool[,])flowerData;
        }
        else
        {
            // Если нет, создаем новую
            flowerMap = new bool[context.Width, context.Height];
            context.SharedData["terrain.flowers"] = flowerMap;
        }

        // Безопасное получение карты мощения
        bool[,] pavementMap;
        if (context.SharedData.TryGetValue("terrain.pavement", out var pavementData))
        {
            pavementMap = (bool[,])pavementData;
        }
        else
        {
            // Если нет, создаем новую
            pavementMap = new bool[context.Width, context.Height];
            context.SharedData["terrain.pavement"] = pavementMap;
        }

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

        // Пропускаем, если эта ячейка уже является частью здания
        if (x >= 0 && x < context.Width && y >= 0 && y < context.Height && buildingMap[x, y])
        {
            return possibleStates;
        }

        // Проверяем тип местности в этой позиции
        if (x >= 0 && x < context.Width && y >= 0 && y < context.Height)
        {
            if (pavementMap[x, y])
            {
                // Предпочитаем мощение в областях мощения
                if (tileIndexMap.TryGetValue("pavement.basic", out int pavementIndex))
                {
                    if (possibleStates.Contains(pavementIndex))
                    {
                        return new[] { pavementIndex };
                    }
                }
            }
            else if (flowerMap[x, y])
            {
                // Предпочитаем цветы в областях цветов
                if (tileIndexMap.TryGetValue("flowers.basic", out int flowerIndex))
                {
                    if (possibleStates.Contains(flowerIndex))
                    {
                        return new[] { flowerIndex };
                    }
                }
            }
            else
            {
                // Предпочитаем траву в остальных местах
                if (tileIndexMap.TryGetValue("grass.basic", out int grassIndex))
                {
                    if (possibleStates.Contains(grassIndex))
                    {
                        return new[] { grassIndex };
                    }
                }
            }
        }

        // Если нет конкретных предпочтений или предпочитаемая плитка недоступна, возвращаем исходные состояния
        return possibleStates;
    }

    public void OnAfterCollapse(int x, int y, int state, GenerationContext context)
    {
        // Nothing to do here
    }

    public void OnAfterGeneration(Tile[,] grid, GenerationContext context)
    {
        // Nothing to do here
    }

    public Tile[,] OnPostProcess(Tile[,] grid, GenerationContext context)
    {
        // Perform terrain transitions as part of post-processing
        return ProcessTerrainTransitions(grid, context);
    }

    public Tile[,] ProcessGrid(Tile[,] grid, GenerationContext context)
    {
        // Same implementation as OnPostProcess to ensure transitions are processed
        return ProcessTerrainTransitions(grid, context);
    }

    private float[,] GenerateNoiseMap(int width, int height, Random random)
    {
        var noiseMap = new float[width, height];

        // Generate simplex-like noise
        double xFreq = 0.1;
        double yFreq = 0.1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Generate pseudo-noise
                double noise = Math.Sin(x * xFreq * 3.1) * Math.Cos(y * yFreq * 2.7) +
                               Math.Sin((x + y) * xFreq * 1.5) * 0.5 +
                               Math.Cos(x * xFreq * 2.0 - y * yFreq * 3.0) * 0.25;

                // Add randomness
                noise += (random.NextDouble() * 0.8 - 0.4);

                // Normalize to 0.0-1.0 range
                noiseMap[x, y] = (float)((noise + 2.0) / 4.0);
            }
        }

        return noiseMap;
    }

    private bool[,] GenerateFlowerMap(float[,] noiseMap, int width, int height, Random random)
    {
        var flowerMap = new bool[width, height];

        // Use noise map to determine flower areas
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Use noise value with threshold
                flowerMap[x, y] = noiseMap[x, y] > 0.6 && random.NextDouble() < 0.7;
            }
        }

        // Apply cellular automata to create coherent regions
        bool[,] temp = new bool[width, height];

        for (int pass = 0; pass < 2; pass++)
        {
            // Copy current state
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    temp[x, y] = flowerMap[x, y];
                }
            }

            // Apply rules
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int flowerNeighbors = 0;

                    // Count flower neighbors
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (flowerMap[x + dx, y + dy])
                                flowerNeighbors++;
                        }
                    }

                    // Apply rules
                    if (flowerMap[x, y])
                    {
                        // Keep flowers if well supported
                        temp[x, y] = flowerNeighbors >= 4;
                    }
                    else
                    {
                        // Add flowers if well surrounded
                        temp[x, y] = flowerNeighbors >= 5;
                    }
                }
            }

            // Copy back
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    flowerMap[x, y] = temp[x, y];
                }
            }
        }

        return flowerMap;
    }

    private bool[,] GeneratePavementMap(int width, int height, Random random)
    {
        var pavementMap = new bool[width, height];

        // Create pavement paths
        int pathCount = Math.Max(1, width / 10);

        for (int i = 0; i < pathCount; i++)
        {
            // Choose path direction - horizontal or vertical
            bool isHorizontal = random.NextDouble() < 0.5;

            if (isHorizontal)
            {
                // Create horizontal path
                int y = random.Next(2, height - 2);
                int pathWidth = random.Next(1, 3);

                for (int py = y; py < Math.Min(height, y + pathWidth); py++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        pavementMap[x, py] = true;
                    }
                }

                // Add some noise to path
                AddPathNoise(pavementMap, 0, width, y, y + pathWidth, random);
            }
            else
            {
                // Create vertical path
                int x = random.Next(2, width - 2);
                int pathWidth = random.Next(1, 3);

                for (int px = x; px < Math.Min(width, x + pathWidth); px++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        pavementMap[px, y] = true;
                    }
                }

                // Add some noise to path
                AddPathNoise(pavementMap, x, x + pathWidth, 0, height, random);
            }
        }

        // Add occasional pavement areas
        int areaCount = Math.Max(1, (width * height) / 200);

        for (int i = 0; i < areaCount; i++)
        {
            int x = random.Next(2, width - 5);
            int y = random.Next(2, height - 5);
            int areaWidth = random.Next(3, 6);
            int areaHeight = random.Next(3, 6);

            for (int py = y; py < Math.Min(height, y + areaHeight); py++)
            {
                for (int px = x; px < Math.Min(width, x + areaWidth); px++)
                {
                    pavementMap[px, py] = true;
                }
            }
        }

        return pavementMap;
    }

    private void AddPathNoise(bool[,] pavementMap, int minX, int maxX, int minY, int maxY, Random random)
    {
        int width = pavementMap.GetLength(0);
        int height = pavementMap.GetLength(1);

        // Add noise to path edges
        for (int y = Math.Max(0, minY - 1); y < Math.Min(height, maxY + 1); y++)
        {
            for (int x = Math.Max(0, minX - 1); x < Math.Min(width, maxX + 1); x++)
            {
                // Skip core path
                if (x >= minX && x < maxX && y >= minY && y < maxY)
                    continue;

                // Add noise to edges
                if (random.NextDouble() < 0.3)
                {
                    pavementMap[x, y] = true;
                }
            }
        }
    }

    private Tile[,] ProcessTerrainTransitions(Tile[,] grid, GenerationContext context)
    {
        int width = context.Width;
        int height = context.Height;
        var tileIndexMap = context.Settings.TileIndexMap;

        // Create a copy of the grid
        var result = new Tile[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = grid[x, y];
            }
        }

        // Process transitions between grass and pavement
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var currentTile = grid[x, y];

                // Skip if not pavement or already a transition
                if (currentTile.TileId != "pavement.basic" || IsBuildingTile(currentTile.TileId) ||
                    IsTransitionTile(currentTile.TileId))
                    continue;

                // Check neighbors
                bool topIsGrass = IsGrassLike(GetTileAt(grid, x, y - 1, width, height));
                bool rightIsGrass = IsGrassLike(GetTileAt(grid, x + 1, y, width, height));
                bool bottomIsGrass = IsGrassLike(GetTileAt(grid, x, y + 1, width, height));
                bool leftIsGrass = IsGrassLike(GetTileAt(grid, x - 1, y, width, height));

                // Skip if no grass neighbors
                if (!topIsGrass && !rightIsGrass && !bottomIsGrass && !leftIsGrass)
                    continue;

                // Determine transition tile
                string transitionId = GetTransitionTileId(topIsGrass, rightIsGrass, bottomIsGrass, leftIsGrass);

                // Apply transition if needed
                if (transitionId != null && tileIndexMap.TryGetValue(transitionId, out int transitionIndex))
                {
                    result[x, y] = context.Settings.Tiles[transitionIndex];
                }
            }
        }

        return result;
    }

    private Tile GetTileAt(Tile[,] grid, int x, int y, int width, int height)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return null;

        return grid[x, y];
    }

    private bool IsGrassLike(Tile tile)
    {
        if (tile == null)
            return false;

        return tile.TileId == "grass.basic" || tile.TileId == "flowers.basic";
    }

    private bool IsBuildingTile(string tileId)
    {
        return tileId.StartsWith("building.");
    }

    private bool IsTransitionTile(string tileId)
    {
        return tileId.Contains("pavement.grass.");
    }

    private string GetTransitionTileId(bool topIsGrass, bool rightIsGrass, bool bottomIsGrass, bool leftIsGrass)
    {
        // Check for corner cases
        if (topIsGrass && leftIsGrass && !rightIsGrass && !bottomIsGrass)
            return "pavement.grass.topleft";

        if (topIsGrass && rightIsGrass && !leftIsGrass && !bottomIsGrass)
            return "pavement.grass.topright";

        if (bottomIsGrass && leftIsGrass && !rightIsGrass && !topIsGrass)
            return "pavement.grass.bottomleft";

        if (bottomIsGrass && rightIsGrass && !leftIsGrass && !topIsGrass)
            return "pavement.grass.bottomright";

        // Check for edge cases
        if (topIsGrass && !rightIsGrass && !bottomIsGrass && !leftIsGrass)
            return "pavement.grass.top";

        if (!topIsGrass && rightIsGrass && !bottomIsGrass && !leftIsGrass)
            return "pavement.grass.right";

        if (!topIsGrass && !rightIsGrass && bottomIsGrass && !leftIsGrass)
            return "pavement.grass.bottom";

        if (!topIsGrass && !rightIsGrass && !bottomIsGrass && leftIsGrass)
            return "pavement.grass.left";

        // Multiple connections - choose based on priority
        if (topIsGrass && rightIsGrass)
            return "pavement.grass.topright";

        if (topIsGrass && leftIsGrass)
            return "pavement.grass.topleft";

        if (bottomIsGrass && rightIsGrass)
            return "pavement.grass.bottomright";

        if (bottomIsGrass && leftIsGrass)
            return "pavement.grass.bottomleft";

        // No clear transition needed
        return null;
    }
}