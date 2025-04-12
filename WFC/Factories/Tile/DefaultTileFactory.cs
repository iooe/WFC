namespace WFC.Factories;

public class DefaultTileFactory : ITileFactory
{
    public Tile CreateTile(int id, string name, string folderPath)
    {
        return new Tile(id, name, folderPath);
    }

    public Tile CreateBasicTile(int id, string name)
    {
        string folderPath = name.ToLowerInvariant();
        return new Tile(id, name, folderPath);
    }

    public Tile CreateTransitionTile(int id, string name, string direction)
    {
        string folderPath = $"pavement-transitions/{direction}";
        return new Tile(id, name, folderPath);
    }

    public Tile CreateBuildingTile(int id, string name, string buildingPart)
    {
        string folderPath = $"buildings/{buildingPart}";
        return new Tile(id, name, folderPath);
    }
}