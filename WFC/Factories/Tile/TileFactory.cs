using WFC.Models;

namespace WFC.Factories;

public class TileFactory: ITileFactory
{
    /// <summary>
    /// Create a new tile
    /// </summary>
    public Tile CreateTile(int id, string tileId, string name, string folderPath, 
        string category = null, Dictionary<string, string> properties = null)
    {
        return new Tile(id, tileId, name, folderPath, category, properties);
    }
}