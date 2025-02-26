using WFC.Models;

namespace WFC.Services;

public class WFCResult
{
    public bool Success { get; set; }
    public Tile[,] Grid { get; set; }
    public string ErrorMessage { get; set; }
}