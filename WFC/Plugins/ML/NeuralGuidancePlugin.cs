using WFC.Factories.Model;
using WFC.Models;
using WFC.Models.NeuralNetwork;
using WFC.Services.ML;

namespace WFC.Plugins.ML
{
    /// <summary>
    /// Plugin that uses neural network feedback to guide WFC generation
    /// </summary>
    public class NeuralGuidancePlugin : IGenerationHookPlugin, IPostProcessorPlugin
    {
        public string Id => "wfc.neural.guidance";
        public string Name => "Neural Network Guidance";
        public string Version => "1.0";
        public string Description => "Uses neural network feedback to guide WFC generation";
        public bool Enabled { get; set; }
        public int Priority => 20; // Run after terrain and building plugins
        
        private IQualityAssessmentModel _model;
        private int _evaluationFrequency = 10; // How often to evaluate during generation
        private int _generationCounter = 0;
        
        public void Initialize(IServiceProvider serviceProvider)
        {
            // Get model factory from services if available
            var modelFactory = serviceProvider.GetService(typeof(IModelFactory)) as IModelFactory;
            if (modelFactory != null)
            {
                _model = modelFactory.CreateModel(ModelType.Advanced);
                Console.WriteLine("Neural guidance using advanced quality model");
            }
            else
            {
                // Create default model
                _model = new AccordNetQualityModel();
                Console.WriteLine("Neural guidance using default quality model");
            }
        }
        
        public void OnBeforeGeneration(WFCSettings settings)
        {
            _generationCounter = 0;
            
            // Initialize neural guidance data
            var context = settings.PluginSettings.TryGetValue("context", out var contextObj) ? 
                contextObj as GenerationContext : null;
                
            if (context != null)
            {
                context.SharedData["neural.guidance"] = new Dictionary<string, float>();
                context.SharedData["neural.lastEvaluation"] = null;
                Console.WriteLine("Neural guidance initialized");
            }
            else
            {
                Console.WriteLine("Warning: Context not available for neural guidance");
            }
        }
        
        public IEnumerable<int> OnBeforeCollapse(int x, int y, IEnumerable<int> possibleStates, GenerationContext context)
        {
            return possibleStates; // Default behavior, no neural guidance yet

            // Every N collapses, evaluate current state and adjust weights
            if (++_generationCounter % _evaluationFrequency == 0 && context.Grid != null)
            {
                // Create partial map from current state
                var partialMap = CreatePartialMap(context);
                
                try
                {
                    // Async evaluations in a sync context need to be handled carefully
                    var task = _model.EvaluateAsync(partialMap);
                    task.Wait();
                    var assessment = task.Result;
                    
                    // Store evaluation results
                    context.SharedData["neural.lastEvaluation"] = assessment;
                    
                    // Use assessment to influence state selection
                    return AdjustStatePriorities(possibleStates, x, y, assessment, context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in neural guidance: {ex.Message}");
                }
            }
            
            return possibleStates;
        }
        
        private Tile[,] CreatePartialMap(GenerationContext context)
        {
            // Create a tile map from the current partially collapsed grid
            int width = context.Width;
            int height = context.Height;
            var map = new Tile[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var cell = context.Grid[x, y];
                    if (cell.Collapsed && cell.CollapsedState.HasValue)
                    {
                        // Use the collapsed state
                        map[x, y] = context.Settings.Tiles[cell.CollapsedState.Value];
                    }
                    else
                    {
                        // For uncollapsed cells, use null or a placeholder
                        map[x, y] = null;
                    }
                }
            }
            
            return map;
        }
        
        private IEnumerable<int> AdjustStatePriorities(IEnumerable<int> states, int x, int y, 
                                                QualityAssessment assessment, GenerationContext context)
        {
            // If the list is empty or only has one item, nothing to prioritize
            if (!states.Any() || states.Count() == 1)
                return states;
            
            // Get the list of states
            var statesList = states.ToList();
            
            // If quality is already good, no need to intervene
            if (assessment.OverallScore > 0.7f)
                return statesList;
            
            // Get the lowest dimensional score to focus improvement
            var lowestDimension = assessment.DimensionalScores
                .OrderBy(kv => kv.Value)
                .First();
            
            // Prioritize states based on the lowest dimension
            switch (lowestDimension.Key)
            {
                case "Coherence":
                    return PrioritizeForCoherence(statesList, context);
                    
                case "Aesthetics":
                    return PrioritizeForAesthetics(statesList, context);
                    
                case "Playability":
                    return PrioritizeForPlayability(statesList, context);
                    
                default:
                    return statesList;
            }
        }
        
        private IEnumerable<int> PrioritizeForCoherence(List<int> states, GenerationContext context)
        {
            // For coherence, prioritize tiles that create good transitions with neighbors
            var tiles = context.Settings.Tiles;
            
            // Get the current surrounding tile categories
            var surroundingCategories = GetSurroundingCategories(context);
            
            // Find the most common category
            var mostCommonCategory = surroundingCategories
                .GroupBy(c => c)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;
                
            if (string.IsNullOrEmpty(mostCommonCategory))
                return states;
                
            // Prioritize states with matching category (70% chance) or different category (30% chance)
            // to create a balance between uniformity and variety
            if (context.Random.NextDouble() < 0.7)
            {
                // Prioritize matching categories
                return states.OrderByDescending(s => 
                    tiles[s].Category == mostCommonCategory ? 1 : 0);
            }
            else
            {
                // Prioritize different categories for some variety
                return states.OrderByDescending(s => 
                    tiles[s].Category != mostCommonCategory ? 1 : 0);
            }
        }
        
        private IEnumerable<int> PrioritizeForAesthetics(List<int> states, GenerationContext context)
        {
            // For aesthetics, prioritize natural elements and variety
            var tiles = context.Settings.Tiles;
            
            // Count natural vs constructed elements in the current map
            int naturalCount = 0;
            int constructedCount = 0;
            
            for (int x = 0; x < context.Width; x++)
            {
                for (int y = 0; y < context.Height; y++)
                {
                    var cell = context.Grid[x, y];
                    if (cell.Collapsed && cell.CollapsedState.HasValue)
                    {
                        var tile = tiles[cell.CollapsedState.Value];
                        var category = tile.Category?.ToLowerInvariant();
                        
                        if (category == "grass" || category == "flowers" || category == "water")
                            naturalCount++;
                        else if (category == "pavement" || category == "building")
                            constructedCount++;
                    }
                }
            }
            
            // Determine if we need more natural or constructed elements
            bool needMoreNatural = naturalCount < constructedCount;
            
            // Prioritize based on the need
            return states.OrderByDescending(s => {
                var category = tiles[s].Category?.ToLowerInvariant();
                
                if (needMoreNatural && (category == "grass" || category == "flowers" || category == "water"))
                    return 2;
                else if (!needMoreNatural && (category == "pavement" || category == "building"))
                    return 2;
                else
                    return 1;
            });
        }
        
        private IEnumerable<int> PrioritizeForPlayability(List<int> states, GenerationContext context)
        {
            // For playability, aim for a good balance of open areas and obstacles
            var tiles = context.Settings.Tiles;
            
            // Count open vs obstacle areas
            int openCount = 0;
            int obstacleCount = 0;
            
            for (int x = 0; x < context.Width; x++)
            {
                for (int y = 0; y < context.Height; y++)
                {
                    var cell = context.Grid[x, y];
                    if (cell.Collapsed && cell.CollapsedState.HasValue)
                    {
                        var tile = tiles[cell.CollapsedState.Value];
                        bool isWalkable = true;
                        
                        // Check if tile has walkable property
                        if (tile.Properties != null && 
                            tile.Properties.TryGetValue("walkable", out string walkableStr))
                        {
                            isWalkable = walkableStr.ToLowerInvariant() == "true";
                        }
                        else
                        {
                            // Infer from category
                            var category = tile.Category?.ToLowerInvariant();
                            isWalkable = category != "building" && category != "water";
                        }
                        
                        if (isWalkable)
                            openCount++;
                        else
                            obstacleCount++;
                    }
                }
            }
            
            // Target ratio: 70% open, 30% obstacle
            float currentRatio = openCount + obstacleCount > 0 ? 
                (float)openCount / (openCount + obstacleCount) : 0.5f;
            bool needMoreOpen = currentRatio < 0.7f;
            
            // Prioritize based on the need
            return states.OrderByDescending(s => {
                var tile = tiles[s];
                bool isWalkable = true;
                
                // Check walkable property
                if (tile.Properties != null && 
                    tile.Properties.TryGetValue("walkable", out string walkableStr))
                {
                    isWalkable = walkableStr.ToLowerInvariant() == "true";
                }
                else
                {
                    // Infer from category
                    var category = tile.Category?.ToLowerInvariant();
                    isWalkable = category != "building" && category != "water";
                }
                
                if (needMoreOpen && isWalkable)
                    return 2;
                else if (!needMoreOpen && !isWalkable)
                    return 2;
                else
                    return 1;
            });
        }
        
        private List<string> GetSurroundingCategories(GenerationContext context)
        {
            var surroundingCategories = new List<string>();
            
            // Check a 5x5 area around the center of the map
            int centerX = context.Width / 2;
            int centerY = context.Height / 2;
            
            for (int x = Math.Max(0, centerX - 2); x <= Math.Min(context.Width - 1, centerX + 2); x++)
            {
                for (int y = Math.Max(0, centerY - 2); y <= Math.Min(context.Height - 1, centerY + 2); y++)
                {
                    var cell = context.Grid[x, y];
                    if (cell.Collapsed && cell.CollapsedState.HasValue)
                    {
                        var tile = context.Settings.Tiles[cell.CollapsedState.Value];
                        if (!string.IsNullOrEmpty(tile.Category))
                        {
                            surroundingCategories.Add(tile.Category);
                        }
                    }
                }
            }
            
            return surroundingCategories;
        }
        
        public void OnAfterCollapse(int x, int y, int state, GenerationContext context)
        {
            // Nothing to do here
        }
        
        public void OnAfterGeneration(Tile[,] grid, GenerationContext context)
        {
            // Final quality evaluation
            try
            {
                var task = _model.EvaluateAsync(grid);
                task.Wait();
                var finalAssessment = task.Result;
                
                // Store final assessment
                context.SharedData["neural.finalAssessment"] = finalAssessment;
                
                Console.WriteLine($"Final quality assessment: {finalAssessment.OverallScore:F2}");
                foreach (var score in finalAssessment.DimensionalScores)
                {
                    Console.WriteLine($"- {score.Key}: {score.Value:F2}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in final quality assessment: {ex.Message}");
            }
        }
        
        public Tile[,] OnPostProcess(Tile[,] grid, GenerationContext context)
        {
            // Implementation of post-processing based on neural feedback
            return ProcessGrid(grid, context);
        }
        
        public Tile[,] ProcessGrid(Tile[,] grid, GenerationContext context)
        {
            // Get final assessment
            if (!context.SharedData.TryGetValue("neural.finalAssessment", out var assessmentObj) ||
                !(assessmentObj is QualityAssessment assessment))
            {
                return grid; // No assessment available
            }
            
            // If the quality is already good, no need to adjust
            if (assessment.OverallScore > 0.7f)
                return grid;
                
            // Find the lowest dimensional score to determine the improvement needed
            var lowestDimension = assessment.DimensionalScores
                .OrderBy(kv => kv.Value)
                .First();
                
            // Apply improvements based on the lowest dimension
            switch (lowestDimension.Key)
            {
                case "Coherence":
                    return ImproveCoherence(grid, context);
                    
                case "Aesthetics":
                    return ImproveAesthetics(grid, context);
                    
                case "Playability":
                    return ImprovePlayability(grid, context);
                    
                default:
                    return grid;
            }
        }
        
        private Tile[,] ImproveCoherence(Tile[,] grid, GenerationContext context)
        {
            // Improve coherence by reducing jarring transitions
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            var result = new Tile[width, height];
            
            // Copy original grid
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[x, y] = grid[x, y];
                }
            }
            
            // Find and fix incoherent areas
            var tileIndexMap = context.Settings.TileIndexMap;
            
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    // Check if this tile is different from all its neighbors
                    var currentCategory = grid[x, y]?.Category;
                    if (string.IsNullOrEmpty(currentCategory))
                        continue;
                        
                    // Check neighbors
                    int matchingNeighbors = 0;
                    
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            
                            var neighborCategory = grid[x + dx, y + dy]?.Category;
                            if (currentCategory == neighborCategory)
                                matchingNeighbors++;
                        }
                    }
                    
                    // If isolated (0-1 matching neighbors), replace with a more appropriate tile
                    if (matchingNeighbors <= 1)
                    {
                        // Find the most common neighbor category
                        var neighborCategories = new Dictionary<string, int>();
                        
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                
                                var neighborCategory = grid[x + dx, y + dy]?.Category;
                                if (!string.IsNullOrEmpty(neighborCategory))
                                {
                                    if (!neighborCategories.ContainsKey(neighborCategory))
                                        neighborCategories[neighborCategory] = 0;
                                    neighborCategories[neighborCategory]++;
                                }
                            }
                        }
                        
                        // Get most common category
                        var mostCommonCategory = neighborCategories
                            .OrderByDescending(kv => kv.Value)
                            .FirstOrDefault();
                            
                        if (mostCommonCategory.Key != null)
                        {
                            // Find a tile of the most common category or a transition
                            string tileId = "";
                            
                            // Look for a transition tile if available
                            foreach (var entry in tileIndexMap)
                            {
                                // If transition tile available, use it
                                if (entry.Key.Contains("transition") &&
                                    (entry.Key.Contains(currentCategory.ToLowerInvariant()) ||
                                     entry.Key.Contains(mostCommonCategory.Key.ToLowerInvariant())))
                                {
                                    tileId = entry.Key;
                                    break;
                                }
                            }
                            
                            // If no transition tile, use most common category
                            if (string.IsNullOrEmpty(tileId))
                            {
                                foreach (var entry in tileIndexMap)
                                {
                                    if (context.Settings.Tiles[entry.Value].Category == mostCommonCategory.Key)
                                    {
                                        tileId = entry.Key;
                                        break;
                                    }
                                }
                            }
                            
                            // Replace the tile if we found a suitable replacement
                            if (!string.IsNullOrEmpty(tileId) && tileIndexMap.TryGetValue(tileId, out int index))
                            {
                                result[x, y] = context.Settings.Tiles[index];
                            }
                        }
                    }
                }
            }
            
            return result;
        }
        
        private Tile[,] ImproveAesthetics(Tile[,] grid, GenerationContext context)
        {
            // Improve aesthetics by enhancing variety and balance
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            var result = new Tile[width, height];
            
            // Copy original grid
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[x, y] = grid[x, y];
                }
            }
            
            // Count tiles by category
            var categoryCounts = new Dictionary<string, int>();
            int totalTiles = width * height;
            
            foreach (var tile in grid)
            {
                if (tile != null)
                {
                    string category = tile.Category ?? "unknown";
                    if (!categoryCounts.ContainsKey(category))
                        categoryCounts[category] = 0;
                    categoryCounts[category]++;
                }
            }
            
            // Calculate natural vs constructed ratio
            int naturalCount = 
                categoryCounts.GetValueOrDefault("grass", 0) + 
                categoryCounts.GetValueOrDefault("flowers", 0) + 
                categoryCounts.GetValueOrDefault("water", 0);
                
            int constructedCount = 
                categoryCounts.GetValueOrDefault("pavement", 0) + 
                categoryCounts.GetValueOrDefault("building", 0);
                
            // Determine what needs improvement
            bool needMoreNatural = naturalCount < constructedCount * 0.5;
            bool needMoreFlowers = false;
            
            if (!needMoreNatural)
            {
                // Check if we have enough flowers relative to grass
                int grassCount = categoryCounts.GetValueOrDefault("grass", 0);
                int flowerCount = categoryCounts.GetValueOrDefault("flowers", 0);
                needMoreFlowers = flowerCount < grassCount * 0.2; // Aim for ~20% flowers to grass ratio
            }
            
            var tileIndexMap = context.Settings.TileIndexMap;
            var random = context.Random;
            
            // Enhance aesthetics based on needs
            if (needMoreNatural || needMoreFlowers)
            {
                // Find flower and grass tile indices
                int flowerTileIdx = -1;
                int grassTileIdx = -1;
                
                if (tileIndexMap.TryGetValue("flowers.basic", out flowerTileIdx) &&
                    tileIndexMap.TryGetValue("grass.basic", out grassTileIdx))
                {
                    // Replace some non-natural or grass tiles with flowers/natural elements
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var tile = grid[x, y];
                            if (tile == null) continue;
                            
                            if (needMoreNatural)
                            {
                                // Replace some pavement with grass
                                if (tile.Category == "pavement" && random.NextDouble() < 0.15)
                                {
                                    result[x, y] = context.Settings.Tiles[grassTileIdx];
                                }
                            }
                            else if (needMoreFlowers)
                            {
                                // Replace some grass with flowers
                                if (tile.Category == "grass" && random.NextDouble() < 0.2)
                                {
                                    result[x, y] = context.Settings.Tiles[flowerTileIdx];
                                }
                            }
                        }
                    }
                }
            }
            
            return result;
        }
        
        private Tile[,] ImprovePlayability(Tile[,] grid, GenerationContext context)
        {
            // Improve playability by ensuring good open/obstacle balance
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            var result = new Tile[width, height];
            
            // Copy original grid
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[x, y] = grid[x, y];
                }
            }
            
            // Count walkable vs. non-walkable tiles
            int walkableTiles = 0;
            int unwalkableTiles = 0;
            
            foreach (var tile in grid)
            {
                if (tile == null) continue;
                
                bool isWalkable = true;
                
                // Check if tile has walkable property
                if (tile.Properties != null && 
                    tile.Properties.TryGetValue("walkable", out string walkableStr))
                {
                    isWalkable = walkableStr.ToLowerInvariant() == "true";
                }
                else
                {
                    // Infer from category
                    var category = tile.Category?.ToLowerInvariant();
                    isWalkable = category != "building" && category != "water";
                }
                
                if (isWalkable)
                    walkableTiles++;
                else
                    unwalkableTiles++;
            }
            
            // Check if adjustment is needed
            float walkableRatio = (float)walkableTiles / (walkableTiles + unwalkableTiles);
            bool needMoreWalkable = walkableRatio < 0.7f; // Target: 70% walkable
            
            if (needMoreWalkable)
            {
                // Find grass and building tile indices
                int grassTileIdx = -1;
                var tileIndexMap = context.Settings.TileIndexMap;
                
                if (tileIndexMap.TryGetValue("grass.basic", out grassTileIdx))
                {
                    // Replace some obstacles with grass to create more walkable areas
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var tile = grid[x, y];
                            if (tile == null) continue;
                            
                            // Check if this is an obstacle
                            bool isWalkable = true;
                            
                            if (tile.Properties != null && 
                                tile.Properties.TryGetValue("walkable", out string walkableStr))
                            {
                                isWalkable = walkableStr.ToLowerInvariant() == "true";
                            }
                            else
                            {
                                var category = tile.Category?.ToLowerInvariant();
                                isWalkable = category != "building" && category != "water";
                            }
                            
                            // Consider replacing some obstacles
                            if (!isWalkable && context.Random.NextDouble() < 0.2)
                            {
                                result[x, y] = context.Settings.Tiles[grassTileIdx];
                            }
                        }
                    }
                }
            }
            else if (walkableRatio > 0.85f) // If too many walkable areas, add some obstacles
            {
                // For now, we'll just leave this as-is since adding proper obstacles 
                // would be more complex and might disrupt the map's coherence
            }
            
            return result;
        }
    }
}