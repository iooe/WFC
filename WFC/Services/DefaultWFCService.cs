using WFC.Models;

namespace WFC.Services
{
    public class DefaultWFCService : IWFCService
    {
        private Random random;
        private Cell[,] grid;
        private WFCSettings settings;
        private float totalCells;
        private int collapsedCells;
        private bool[,] buildingMap; // Поле для хранения информации о зданиях

        public event EventHandler<WFCProgressEventArgs> ProgressChanged;

        public async Task<WFCResult> GenerateAsync(WFCSettings settings, CancellationToken token = default)
        {
            // Создаем новый генератор случайных чисел с уникальным seed
            random = new Random(Guid.NewGuid().GetHashCode());

            this.settings = settings;
            InitializeGrid();
            totalCells = settings.Width * settings.Height;
            collapsedCells = 0;

            try
            {
                // Запускаем алгоритм в фоновом потоке
                return await Task.Run(() =>
                {
                    // Используем упрощенный WFC алгоритм
                    UpdateProgress("Starting simplified WFC algorithm");

                    // Этап 1: Создаем базовую карту с зашумлением
                    bool[,] isLandMap = CreateNoiseBasedMap(settings.Width, settings.Height);

                    // Этап 2: Преобразуем базовую карту в тайлы с учетом правил соединения
                    for (int y = 0; y < settings.Height; y++)
                    {
                        for (int x = 0; x < settings.Width; x++)
                        {
                            token.ThrowIfCancellationRequested();

                            // Выбираем тип тайла на основе карты и соседей
                            int tileType = DetermineTileType(x, y, isLandMap);

                            // Схлопываем клетку
                            grid[x, y].Collapse(tileType);
                            collapsedCells++;

                            // Обновляем прогресс каждые N ячеек
                            if ((x + y * settings.Width) % 10 == 0)
                            {
                                UpdateProgress($"Processing: {collapsedCells}/{totalCells} cells");
                            }
                        }
                    }

                    // Этап 3: Проверяем и исправляем несоответствия
                    FixInconsistencies();

                    UpdateProgress("WFC generation completed successfully");

                    return new WFCResult
                    {
                        Success = true,
                        Grid = GetResultGrid(),
                        ErrorMessage = null
                    };
                }, token);
            }
            catch (OperationCanceledException)
            {
                return new WFCResult { Success = false, ErrorMessage = "Operation canceled" };
            }
            catch (Exception ex)
            {
                return new WFCResult { Success = false, ErrorMessage = $"Error: {ex.Message}" };
            }
        }

        // Обновленный метод CreateNoiseBasedMap с адаптивными параметрами в зависимости от размера сетки
        private bool[,] CreateNoiseBasedMap(int width, int height)
        {
            // For our system we'll use this to determine where flowers appear and buildings
            bool[,] featureMap = new bool[width, height];
            bool[,] buildingMap = new bool[width, height];

            // Calculate scale-adaptive frequency parameters based on grid size
            // For larger grids, we need lower frequencies to get similar sized features
            double baseScaleFactor = Math.Min(15.0 / Math.Max(width, height), 1.0);
            double xFreq = 0.15 * baseScaleFactor; 
            double yFreq = 0.15 * baseScaleFactor;
            
            // Calculate building density factor - more buildings on larger maps
            double densityFactor = 0.7;
            if (width > 16 || height > 16)
            {
                densityFactor = 0.65;
            }
            if (width > 25 || height > 25)
            {
                densityFactor = 0.6;
            }

            // Generate the basic "noise" map
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate pseudo-noise for this position
                    double noise = Math.Sin(x * xFreq * 3.1) * Math.Cos(y * yFreq * 2.7) +
                                Math.Sin((x + y) * xFreq * 1.5) * 0.5 +
                                Math.Cos(x * xFreq * 2.0 - y * yFreq * 3.0) * 0.25;

                    // Add randomness
                    noise += (random.NextDouble() * 0.8 - 0.4);

                    // Determine terrain type: true = flowers potential, false = no flowers potential
                    featureMap[x, y] = noise > 0.3;
                    
                    // Generate separate noise for buildings - with longer wavelength for larger structures
                    double buildingNoise = Math.Sin(x * xFreq * 0.7) * Math.Cos(y * yFreq * 0.8) +
                                          Math.Sin((x + y) * xFreq * 0.4) * 0.3 +
                                          Math.Cos(x * xFreq * 0.5 - y * yFreq * 0.6) * 0.2;
                    
                    // Add some controlled randomness with bias toward positive values
                    buildingNoise += (random.NextDouble() * 0.4);
                    
                    // For larger maps, lower the threshold to make buildings more common
                    double buildingThreshold = 0.8 * densityFactor;
                    buildingMap[x, y] = buildingNoise > buildingThreshold; 
                    
                    // Ensure some minimum spacing between potential buildings
                    if (buildingMap[x, y])
                    {
                        // Force a minimum gap between building clusters
                        int clearRadius = 3;
                        for (int dy = -clearRadius; dy <= clearRadius; dy++)
                        {
                            for (int dx = -clearRadius; dx <= clearRadius; dx++)
                            {
                                if (dx == 0 && dy == 0) continue; // Skip self
                                
                                int nx = x + dx;
                                int ny = y + dy;
                                
                                // If this is outside the map, skip
                                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                                
                                // Neighboring cells have a lower chance to be buildings
                                double distance = Math.Sqrt(dx*dx + dy*dy);
                                if (distance < 2) 
                                {
                                    buildingMap[nx, ny] = buildingMap[nx, ny] && random.NextDouble() < 0.3;
                                }
                            }
                        }
                    }
                }
            }

            // Apply cellular automata to smooth the feature map (flowers/grass)
            bool[,] smoothedFeatures = new bool[width, height];
            for (int pass = 0; pass < 2; pass++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int featureNeighbors = 0;
                        int totalNeighbors = 0;

                        // Check neighbors in a 3x3 area
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;

                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    totalNeighbors++;
                                    if (featureMap[nx, ny]) featureNeighbors++;
                                }
                            }
                        }

                        // Smoothing rule: majority determines type
                        double featureRatio = (double)featureNeighbors / totalNeighbors;

                        // Add some randomness for variety
                        if (random.NextDouble() < 0.1) // 10% chance for random decision
                        {
                            smoothedFeatures[x, y] = random.NextDouble() < 0.3; // Less likely to be flowers
                        }
                        else
                        {
                            // Mostly follow majority rule
                            smoothedFeatures[x, y] = featureRatio > 0.5;
                        }
                    }
                }

                // Copy the smoothed map for the next pass
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        featureMap[x, y] = smoothedFeatures[x, y];
                    }
                }
            }
            
            // Now apply our improved building generation algorithm
            
            // First pass: grow building seeds into rectangles
            GrowBuildingRectangles(buildingMap, width, height);
            
            // Second pass: apply cellular automata to smooth buildings and ensure good shapes
            bool[,] smoothedBuildings = new bool[width, height];
            for (int pass = 0; pass < 3; pass++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Count building neighbors 
                        int adjNeighbors = 0; // Adjacent neighbors (NSEW)
                        int diagNeighbors = 0; // Diagonal neighbors
                        
                        // Check adjacent
                        if (y > 0 && buildingMap[x, y - 1]) adjNeighbors++;
                        if (y < height - 1 && buildingMap[x, y + 1]) adjNeighbors++;
                        if (x > 0 && buildingMap[x - 1, y]) adjNeighbors++;
                        if (x < width - 1 && buildingMap[x + 1, y]) adjNeighbors++;
                        
                        // Check diagonal
                        if (x > 0 && y > 0 && buildingMap[x - 1, y - 1]) diagNeighbors++;
                        if (x < width - 1 && y > 0 && buildingMap[x + 1, y - 1]) diagNeighbors++;
                        if (x > 0 && y < height - 1 && buildingMap[x - 1, y + 1]) diagNeighbors++;
                        if (x < width - 1 && y < height - 1 && buildingMap[x + 1, y + 1]) diagNeighbors++;
                        
                        // Rules for building cells
                        if (buildingMap[x, y])
                        {
                            // Keep existing buildings if they have good support
                            smoothedBuildings[x, y] = adjNeighbors >= 2 || (adjNeighbors >= 1 && diagNeighbors >= 2);
                        }
                        else
                        {
                            // Add new building cells only if well surrounded
                            smoothedBuildings[x, y] = adjNeighbors >= 3 || (adjNeighbors >= 2 && diagNeighbors >= 2);
                        }
                        
                        // Ensure buildings aren't too close to edges
                        if (x < 2 || y < 2 || x >= width - 2 || y >= height - 2)
                        {
                            smoothedBuildings[x, y] = false;
                        }
                    }
                }
                
                // Copy for next pass
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        buildingMap[x, y] = smoothedBuildings[x, y];
                    }
                }
            }
            
            // Third pass: fill in any small holes in buildings
            FillBuildingHoles(buildingMap, width, height);
            
            // Fourth pass: identify building clusters and ensure proper wall formation
            EnsureBuildingWalls(buildingMap, width, height);
            
            // Make sure buildings and flowers don't overlap
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (buildingMap[x, y])
                    {
                        featureMap[x, y] = false; // No flowers where buildings are
                    }
                }
            }
            
            // Store the building map in a class field for later use
            this.buildingMap = buildingMap;
            
            return featureMap;
        }

        // New method to grow building rectangles from seed points
        private void GrowBuildingRectangles(bool[,] buildingMap, int width, int height)
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
            
            // Find seed clusters and grow them into rectangles
            bool[,] visited = new bool[width, height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (buildingSeeds[x, y] && !visited[x, y])
                    {
                        // Find all connected seed tiles
                        List<(int x, int y)> seedCluster = new List<(int x, int y)>();
                        FloodFillBuildingCluster(x, y, buildingSeeds, visited, seedCluster, width, height);
                        
                        // If we have enough seeds, grow a rectangle
                        if (seedCluster.Count >= 2)
                        {
                            // Find bounding box
                            int minX = width, maxX = 0, minY = height, maxY = 0;
                            
                            foreach (var (cx, cy) in seedCluster)
                            {
                                minX = Math.Min(minX, cx);
                                maxX = Math.Max(maxX, cx);
                                minY = Math.Min(minY, cy);
                                maxY = Math.Max(maxY, cy);
                            }
                            
                            // Expand slightly to make a nice rectangle
                            minX = Math.Max(0, minX - 1);
                            minY = Math.Max(0, minY - 1);
                            maxX = Math.Min(width - 1, maxX + 1);
                            maxY = Math.Min(height - 1, maxY + 1);
                            
                            // Make sure rectangle isn't too big
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
                    }
                }
            }
        }

        // New method to fill small holes in buildings
        private void FillBuildingHoles(bool[,] buildingMap, int width, int height)
        {
            bool[,] temp = new bool[width, height];
            
            // Copy original map
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
                        
                        // If surrounded on at least 5 sides, fill in the hole
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

        // New method to ensure buildings have complete exterior walls
        private void EnsureBuildingWalls(bool[,] buildingMap, int width, int height)
        {
            bool[,] visited = new bool[width, height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (buildingMap[x, y] && !visited[x, y])
                    {
                        // Find connected building cluster
                        List<(int x, int y)> cluster = new List<(int x, int y)>();
                        FloodFillBuildingCluster(x, y, buildingMap, visited, cluster, width, height);
                        
                        // Analyze the cluster
                        if (cluster.Count >= 4 && cluster.Count <= 100)
                        {
                            // Identify interior cells
                            HashSet<(int x, int y)> interiorCells = new HashSet<(int x, int y)>();
                            HashSet<(int x, int y)> edgeCells = new HashSet<(int x, int y)>();
                            
                            foreach (var (cx, cy) in cluster)
                            {
                                bool isEdge = false;
                                
                                // Check each direction to see if it's an edge
                                if (cx == 0 || !buildingMap[cx - 1, cy]) isEdge = true;
                                else if (cx == width - 1 || !buildingMap[cx + 1, cy]) isEdge = true;
                                else if (cy == 0 || !buildingMap[cx, cy - 1]) isEdge = true;
                                else if (cy == height - 1 || !buildingMap[cx, cy + 1]) isEdge = true;
                                
                                if (isEdge)
                                    edgeCells.Add((cx, cy));
                                else
                                    interiorCells.Add((cx, cy));
                            }
                            
                            // Verify all interior cells are surrounded
                            bool valid = true;
                            foreach (var (cx, cy) in interiorCells)
                            {
                                // Ensure it has neighbors in all 4 cardinal directions
                                if (!buildingMap[cx - 1, cy] || !buildingMap[cx + 1, cy] ||
                                    !buildingMap[cx, cy - 1] || !buildingMap[cx, cy + 1])
                                {
                                    valid = false;
                                    break;
                                }
                            }
                            
                            // If invalid or too few edge cells relative to total, reject this building
                            double edgeRatio = (double)edgeCells.Count / cluster.Count;
                            if (!valid || edgeRatio < 0.3 || edgeRatio > 0.9)
                            {
                                // Delete this entire cluster
                                foreach (var (cx, cy) in cluster)
                                {
                                    buildingMap[cx, cy] = false;
                                }
                            }
                        }
                        else
                        {
                            // Cluster is too small or too large, remove it
                            foreach (var (cx, cy) in cluster)
                            {
                                buildingMap[cx, cy] = false;
                            }
                        }
                    }
                }
            }
        }

        // Helper method to identify connected building clusters
        private void FloodFillBuildingCluster(int x, int y, bool[,] buildingMap, bool[,] visited, 
                                        List<(int x, int y)> cluster, int width, int height)
        {
            if (x < 0 || y < 0 || x >= width || y >= height || visited[x, y] || !buildingMap[x, y])
                return;
                
            visited[x, y] = true;
            cluster.Add((x, y));
            
            // Only check 4-connected neighbors for better building shapes
            FloodFillBuildingCluster(x + 1, y, buildingMap, visited, cluster, width, height);
            FloodFillBuildingCluster(x - 1, y, buildingMap, visited, cluster, width, height);
            FloodFillBuildingCluster(x, y + 1, buildingMap, visited, cluster, width, height);
            FloodFillBuildingCluster(x, y - 1, buildingMap, visited, cluster, width, height);
        }

        // Determine tile type based on neighbors and noise map
        private int DetermineTileType(int x, int y, bool[,] featureMap)
        {
            // First check if this position is designated for a building
            if (buildingMap != null && buildingMap[x, y])
            {
                // Determine building tile type based on neighbors
                // We'll determine the exact type in EnhanceBuildingEdges later
                // Just use a default wall middle for all building tiles initially
                return TileTypes.WALL_FRONT_MIDDLE;
            }
            
            // Check if this position can have flowers based on our noise map
            bool canHaveFlowers = featureMap[x, y];

            // Check for pavement in a more clustered pattern
            // Create a simple deterministic pattern for pavement
            bool isPavementArea = (x % 7 == 0 && y % 7 == 0) || // Create occasional pavement clusters
                                ((x % 7 == 1 || x % 7 == 2) && y % 7 == 0) || // Extend horizontally
                                (x % 7 == 0 && (y % 7 == 1 || y % 7 == 2)); // Extend vertically

            // Add some randomness to pavement placement
            double pavementRandom = Math.Sin(x * 0.3) * Math.Cos(y * 0.4) + random.NextDouble() * 0.4;
            bool isPavement = isPavementArea && pavementRandom > 0.2;

            // Determine the tile type based on our logic
            if (isPavement)
            {
                return TileTypes.PAVEMENT;
            }
            else if (canHaveFlowers && random.NextDouble() < 0.6) // 60% chance for flowers in flower potential areas
            {
                return TileTypes.FLOWERS;
            }
            else
            {
                return TileTypes.GRASS;
            }
        }

        // Updated fix inconsistencies method (without roof checks)
        private void FixInconsistencies()
        {
            // For our new tile system, we have simpler rules for consistency
            // We'll still check each tile to ensure transitions look good
            for (int y = 0; y < settings.Height; y++)
            {
                for (int x = 0; x < settings.Width; x++)
                {
                    int tileId = grid[x, y].CollapsedState.Value;

                    // Check neighbors and fix inconsistencies
                    foreach (var (nx, ny, direction) in GetNeighbors(x, y))
                    {
                        if (!IsValidPosition(nx, ny)) continue;

                        int neighborId = grid[nx, ny].CollapsedState.Value;
                        string oppositeDir = GetOppositeDirection(direction);

                        // Get the surface types
                        var mySurface = TileTypes.GetSurfaceType(tileId, direction);
                        var neighborSurface = TileTypes.GetSurfaceType(neighborId, oppositeDir);

                        // In our new system, we don't have strict rules for transitions
                        // But we want to avoid too many abrupt changes

                        // Apply a small chance to fix transitions based on weights
                        // This creates smoother transitions

                        // For high contrast transitions (like pavement to flowers)
                        // we apply a higher chance of smoothing
                        bool isHighContrastTransition =
                            (mySurface == SurfaceType.Pavement && neighborSurface == SurfaceType.Flowers) ||
                            (mySurface == SurfaceType.Flowers && neighborSurface == SurfaceType.Pavement);

                        // The chance to smooth depends on the transition type
                        double smoothingChance = isHighContrastTransition ? 0.4 : 0.2;

                        if (mySurface != neighborSurface && random.NextDouble() < smoothingChance)
                        {
                            // Choose whether to change current tile or neighbor
                            if (random.NextDouble() < 0.5)
                            {
                                // 50% chance to fix current tile
                                // Use grass more often as a transition tile
                                int newTileId = random.NextDouble() < 0.7
                                    ? TileTypes.GRASS
                                    : GetCompatibleTile(direction, neighborSurface);

                                grid[x, y] = new Cell(settings.Tiles.Count);
                                grid[x, y].Collapse(newTileId);
                            }
                            else
                            {
                                // 50% chance to fix neighbor
                                // Use grass more often as a transition tile
                                int newNeighborId = random.NextDouble() < 0.7
                                    ? TileTypes.GRASS
                                    : GetCompatibleTile(oppositeDir, mySurface);

                                grid[nx, ny] = new Cell(settings.Tiles.Count);
                                grid[nx, ny].Collapse(newNeighborId);
                            }
                        }
                    }
                }
            }
            
            // Now enhance building edges with improved algorithm
            EnhanceBuildingEdges();
            
            // Clean up isolated building blocks
            CleanupIsolatedBuildingTiles();
        }

        // Updated method to ensure building edge consistency
        private void EnhanceBuildingEdges()
        {
            // First pass: identify all building tiles
            bool[,] isBuildingTile = new bool[settings.Width, settings.Height];
            
            for (int y = 0; y < settings.Height; y++)
            {
                for (int x = 0; x < settings.Width; x++)
                {
                    int tileId = grid[x, y].CollapsedState.Value;
                    isBuildingTile[x, y] = IsBuildingTile(tileId);
                }
            }
            
            // Mark interior vs exterior tiles
            bool[,] isInterior = new bool[settings.Width, settings.Height];
            for (int y = 1; y < settings.Height - 1; y++)
            {
                for (int x = 1; x < settings.Width - 1; x++)
                {
                    if (isBuildingTile[x, y])
                    {
                        // Check if surrounded by building tiles in all cardinal directions
                        isInterior[x, y] = isBuildingTile[x - 1, y] && isBuildingTile[x + 1, y] &&
                                           isBuildingTile[x, y - 1] && isBuildingTile[x, y + 1];
                    }
                }
            }
            
            // Second pass: enhance edges
            for (int y = 0; y < settings.Height; y++)
            {
                for (int x = 0; x < settings.Width; x++)
                {
                    if (!isBuildingTile[x, y]) continue;
                    
                    // Skip interior tiles unless they're marked for windows
                    if (y > 0 && y < settings.Height - 1 && x > 0 && x < settings.Width - 1)
                    {
                        if (isInterior[x, y])
                        {
                            // Interior tiles should remain as standard wall middles, with occasional windows
                            // Only place windows on every 3rd interior tile for a pleasing pattern
                            if ((x + y) % 3 == 0 && random.NextDouble() < 0.4)
                            {
                                grid[x, y] = new Cell(settings.Tiles.Count);
                                grid[x, y].Collapse(TileTypes.WALL_WINDOW_TOP);
                                
                                if (y < settings.Height - 1 && isBuildingTile[x, y + 1] && isInterior[x, y + 1])
                                {
                                    grid[x, y + 1] = new Cell(settings.Tiles.Count);
                                    grid[x, y + 1].Collapse(TileTypes.WALL_WINDOW_BOTTOM);
                                }
                            }
                            else
                            {
                                // Skip other interior tiles - leave as middle wall
                                grid[x, y] = new Cell(settings.Tiles.Count);
                                grid[x, y].Collapse(TileTypes.WALL_FRONT_MIDDLE);
                            }
                            continue;
                        }
                    }
                    
                    // Handle exterior walls and corners properly
                    bool hasTopNeighbor = y > 0 && isBuildingTile[x, y - 1];
                    bool hasRightNeighbor = x < settings.Width - 1 && isBuildingTile[x + 1, y];
                    bool hasBottomNeighbor = y < settings.Height - 1 && isBuildingTile[x, y + 1];
                    bool hasLeftNeighbor = x > 0 && isBuildingTile[x - 1, y];
                    
                    // Determine the exterior edges where walls should be
                    if (!hasTopNeighbor && hasBottomNeighbor) // Top edge
                    {
                        if (!hasLeftNeighbor && hasRightNeighbor) // Top-left corner
                        {
                            grid[x, y] = new Cell(settings.Tiles.Count);
                            grid[x, y].Collapse(TileTypes.WALL_FRONT_CORNER_TOP_LEFT);
                        }
                        else if (hasLeftNeighbor && !hasRightNeighbor) // Top-right corner
                        {
                            grid[x, y] = new Cell(settings.Tiles.Count);
                            grid[x, y].Collapse(TileTypes.WALL_FRONT_CORNER_TOP_RIGHT);
                        }
                        else if (hasLeftNeighbor && hasRightNeighbor) // Middle top edge
                        {
                            grid[x, y] = new Cell(settings.Tiles.Count);
                            grid[x, y].Collapse(TileTypes.WALL_FRONT_TOP_END);
                        }
                    }
                    else if (hasTopNeighbor && !hasBottomNeighbor) // Bottom edge
                    {
                        if (!hasLeftNeighbor && hasRightNeighbor) // Bottom-left corner
                        {
                            grid[x, y] = new Cell(settings.Tiles.Count);
                            grid[x, y].Collapse(TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT);
                        }
                        else if (hasLeftNeighbor && !hasRightNeighbor) // Bottom-right corner
                        {
                            grid[x, y] = new Cell(settings.Tiles.Count);
                            grid[x, y].Collapse(TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT);
                        }
                        else if (hasLeftNeighbor && hasRightNeighbor) // Middle bottom edge
                        {
                            grid[x, y] = new Cell(settings.Tiles.Count);
                            grid[x, y].Collapse(TileTypes.WALL_FRONT_BOTTOM_END);
                        }
                    }
                    else if (hasTopNeighbor && hasBottomNeighbor) // Middle row
                    {
                        if (!hasLeftNeighbor && hasRightNeighbor) // Left edge
                        {
                            grid[x, y] = new Cell(settings.Tiles.Count);
                            grid[x, y].Collapse(TileTypes.WALL_FRONT_LEFT_END);
                        }
                        else if (hasLeftNeighbor && !hasRightNeighbor) // Right edge
                        {
                            grid[x, y] = new Cell(settings.Tiles.Count);
                            grid[x, y].Collapse(TileTypes.WALL_FRONT_RIGHT_END);
                        }
                    }
                }
            }
            
            // Third pass: fix any remaining window issues
            bool[,] hasWindowTop = new bool[settings.Width, settings.Height];
            bool[,] hasWindowBottom = new bool[settings.Width, settings.Height];
            
            for (int y = 0; y < settings.Height; y++)
            {
                for (int x = 0; x < settings.Width; x++)
                {
                    if (isBuildingTile[x, y])
                    {
                        int tileId = grid[x, y].CollapsedState.Value;
                        
                        if (tileId == TileTypes.WALL_WINDOW_TOP)
                            hasWindowTop[x, y] = true;
                        else if (tileId == TileTypes.WALL_WINDOW_BOTTOM)
                            hasWindowBottom[x, y] = true;
                    }
                }
            }
            
            // Fix any window pairs
            for (int y = 0; y < settings.Height - 1; y++)
            {
                for (int x = 0; x < settings.Width; x++)
                {
                    if (hasWindowTop[x, y] && !hasWindowBottom[x, y + 1])
                    {
                        // Replace bottom or add it
                        if (isBuildingTile[x, y + 1])
                        {
                            grid[x, y + 1] = new Cell(settings.Tiles.Count);
                            grid[x, y + 1].Collapse(TileTypes.WALL_WINDOW_BOTTOM);
                        }
                        else
                        {
                            // Can't place bottom - replace top with wall
                            grid[x, y] = new Cell(settings.Tiles.Count);
                            grid[x, y].Collapse(TileTypes.WALL_FRONT_MIDDLE);
                        }
                    }
                    
                    if (!hasWindowTop[x, y] && hasWindowBottom[x, y + 1])
                    {
                        // Replace top or add it
                        if (isBuildingTile[x, y])
                        {
                            grid[x, y] = new Cell(settings.Tiles.Count);
                            grid[x, y].Collapse(TileTypes.WALL_WINDOW_TOP);
                        }
                        else
                        {
                            // Can't place top - replace bottom with wall
                            grid[x, y + 1] = new Cell(settings.Tiles.Count);
                            grid[x, y + 1].Collapse(TileTypes.WALL_FRONT_MIDDLE);
                        }
                    }
                }
            }
        }

        // Helper method to fix building tile connections
        private void FixBuildingTileConnections(int x, int y)
        {
            int currentTile = grid[x, y].CollapsedState.Value;
            
            // Check neighbors
            bool hasTopNeighbor = y > 0;
            bool hasBottomNeighbor = y < settings.Height - 1;
            bool hasLeftNeighbor = x > 0;
            bool hasRightNeighbor = x < settings.Width - 1;
            
            int topTile = hasTopNeighbor ? grid[x, y - 1].CollapsedState.Value : -1;
            int bottomTile = hasBottomNeighbor ? grid[x, y + 1].CollapsedState.Value : -1;
            int leftTile = hasLeftNeighbor ? grid[x - 1, y].CollapsedState.Value : -1;
            int rightTile = hasRightNeighbor ? grid[x + 1, y].CollapsedState.Value : -1;
            
            // Fix window connections
            if (currentTile == TileTypes.WALL_WINDOW_TOP && hasBottomNeighbor && 
                bottomTile != TileTypes.WALL_WINDOW_BOTTOM)
            {
                // Window top should have window bottom below it
                grid[x, y + 1] = new Cell(settings.Tiles.Count);
                grid[x, y + 1].Collapse(TileTypes.WALL_WINDOW_BOTTOM);
            }
            else if (currentTile == TileTypes.WALL_WINDOW_BOTTOM && hasTopNeighbor && 
                     topTile != TileTypes.WALL_WINDOW_TOP)
            {
                // Window bottom should have window top above it
                grid[x, y - 1] = new Cell(settings.Tiles.Count);
                grid[x, y - 1].Collapse(TileTypes.WALL_WINDOW_TOP);
            }
        }

        // Helper method to clean up isolated building tiles
        private void CleanupIsolatedBuildingTiles()
        {
            bool[,] visited = new bool[settings.Width, settings.Height];
            
            for (int y = 0; y < settings.Height; y++)
            {
                for (int x = 0; x < settings.Width; x++)
                {
                    if (visited[x, y]) continue;
                    
                    int tileId = grid[x, y].CollapsedState.Value;
                    if (!IsBuildingTile(tileId)) continue;
                    
                    // Find all connected building tiles
                    List<(int x, int y)> cluster = new List<(int x, int y)>();
                    FindConnectedBuildingTiles(x, y, visited, cluster);
                    
                    // If cluster is too small, replace with grass/pavement
                    if (cluster.Count < 4)
                    {
                        foreach (var (cx, cy) in cluster)
                        {
                            int replacementTile = TileTypes.GRASS;
                            if (random.NextDouble() < 0.3) // 30% chance for pavement
                                replacementTile = TileTypes.PAVEMENT;
                            
                            grid[cx, cy] = new Cell(settings.Tiles.Count);
                            grid[cx, cy].Collapse(replacementTile);
                        }
                    }
                }
            }
        }

        // Helper method to check if a tile is a building tile
        private bool IsBuildingTile(int tileId)
        {
            return TileTypes.IsWallTile(tileId) || 
                   tileId == TileTypes.WALL_WINDOW_TOP || 
                   tileId == TileTypes.WALL_WINDOW_BOTTOM;
        }

        // Helper method to find connected building tiles
        private void FindConnectedBuildingTiles(int x, int y, bool[,] visited, List<(int x, int y)> cluster)
        {
            if (x < 0 || y < 0 || x >= settings.Width || y >= settings.Height || visited[x, y])
                return;
            
            int tileId = grid[x, y].CollapsedState.Value;
            if (!IsBuildingTile(tileId))
                return;
            
            visited[x, y] = true;
            cluster.Add((x, y));
            
            // Check 4-connected neighbors
            FindConnectedBuildingTiles(x + 1, y, visited, cluster);
            FindConnectedBuildingTiles(x - 1, y, visited, cluster);
            FindConnectedBuildingTiles(x, y + 1, visited, cluster);
            FindConnectedBuildingTiles(x, y - 1, visited, cluster);
        }

        // Updated GetCompatibleTile to remove roof types
        private int GetCompatibleTile(string direction, SurfaceType targetSurface)
        {
            // Return the tile type that matches the target surface
            switch (targetSurface)
            {
                case SurfaceType.Grass:
                    return TileTypes.GRASS;

                case SurfaceType.Flowers:
                    return TileTypes.FLOWERS;

                case SurfaceType.Pavement:
                    return TileTypes.PAVEMENT;

                case SurfaceType.Wall:
                    return TileTypes.WALL_FRONT_MIDDLE;

                case SurfaceType.Window:
                    return direction == "up" ? TileTypes.WALL_WINDOW_TOP : TileTypes.WALL_WINDOW_BOTTOM;

                default:
                    // Default to grass as fallback
                    return TileTypes.GRASS;
            }
        }

        // Инициализация сетки пустыми клетками
        private void InitializeGrid()
        {
            grid = new Cell[settings.Width, settings.Height];
            for (int x = 0; x < settings.Width; x++)
            {
                for (int y = 0; y < settings.Height; y++)
                {
                    grid[x, y] = new Cell(settings.Tiles.Count);
                }
            }
        }

        // Получение противоположного направления
        private string GetOppositeDirection(string direction)
        {
            return direction switch
            {
                "right" => "left",
                "left" => "right",
                "up" => "down",
                "down" => "up",
                _ => throw new ArgumentException($"Invalid direction: {direction}")
            };
        }

        // Проверка валидности позиции
        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < settings.Width && y >= 0 && y < settings.Height;
        }

        // Получение соседей во всех 4 направлениях
        private List<(int x, int y, string direction)> GetNeighbors(int x, int y)
        {
            return new List<(int x, int y, string direction)>
            {
                (x - 1, y, "left"),
                (x + 1, y, "right"),
                (x, y - 1, "up"),
                (x, y + 1, "down")
            };
        }

        // Получение итоговой сетки тайлов
        private Tile[,] GetResultGrid()
        {
            var result = new Tile[settings.Width, settings.Height];

            for (int x = 0; x < settings.Width; x++)
            {
                for (int y = 0; y < settings.Height; y++)
                {
                    var cell = grid[x, y];
                    if (cell.Collapsed && cell.PossibleStates.Count > 0)
                    {
                        result[x, y] = settings.Tiles[cell.CollapsedState.Value];
                    }
                    else
                    {
                        // Для несхлопнутых клеток используем тайл земли по умолчанию
                        result[x, y] = settings.Tiles[0];
                    }
                }
            }

            return result;
        }

        // Обновление прогресса
        private void UpdateProgress(string status)
        {
            var progress = Math.Min(100f, collapsedCells / totalCells * 100);
            ProgressChanged?.Invoke(this, new WFCProgressEventArgs(progress, status));
        }

        // Сброс сервиса
        public void Reset()
        {
            grid = null;
            settings = null;
            collapsedCells = 0;
            buildingMap = null;
            UpdateProgress("Ready");
        }
    }
}