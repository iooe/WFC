namespace WFC.Models;

public class WFCSettings
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<Tile> Tiles { get; set; }
    // Меняем структуру правил, чтобы сделать их более понятными
    public Dictionary<(int fromState, string direction), List<(int toState, float weight)>> Rules { get; set; }

    public WFCSettings()
    {
        Tiles = new List<Tile>();
        Rules = new Dictionary<(int fromState, string direction), List<(int toState, float weight)>>();
    }
}