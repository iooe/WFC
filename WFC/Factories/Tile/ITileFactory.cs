using WFC.Models;

public interface ITileFactory
{
    /// <summary>
    /// Create a new tile
    /// </summary>
    Tile CreateTile(int id, string tileId, string name, string folderPath, 
        string category = null, Dictionary<string, string> properties = null);
}