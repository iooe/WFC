namespace WFC.Models;

/// <summary>
/// Context for the generation process, containing additional data for plugins
/// </summary>
public class GenerationContext
{
    /// <summary>
    /// WFC settings for the current generation
    /// </summary>
    public WFCSettings Settings { get; }
    
    /// <summary>
    /// Width of the grid
    /// </summary>
    public int Width => Settings.Width;
    
    /// <summary>
    /// Height of the grid
    /// </summary>
    public int Height => Settings.Height;
    
    /// <summary>
    /// Random number generator for the generation process
    /// </summary>
    public Random Random { get; }
    
    /// <summary>
    /// Current state of the generation process
    /// </summary>
    public Cell[,] Grid { get; set; }
    
    /// <summary>
    /// Shared data between plugins
    /// </summary>
    public Dictionary<string, object> SharedData { get; } = new();
    
    public GenerationContext(WFCSettings settings, Random random, Cell[,] grid)
    {
        Settings = settings;
        Random = random;
        Grid = grid;
    }
    
    /// <summary>
    /// Get the current state of a cell
    /// </summary>
    public Cell GetCell(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;
            
        return Grid[x, y];
    }
    
    /// <summary>
    /// Get or create a shared data object
    /// </summary>
    public T GetOrCreateSharedData<T>(string key) where T : new()
    {
        if (!SharedData.TryGetValue(key, out var data))
        {
            data = new T();
            SharedData[key] = data;
        }
        
        return (T)data;
    }
}