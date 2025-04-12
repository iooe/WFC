namespace WFC.Factories;

public interface ITileFactory
{
    Tile CreateTile(int id, string name, string folderPath);
    Tile CreateBasicTile(int id, string name);
    Tile CreateTransitionTile(int id, string name, string direction);
    Tile CreateBuildingTile(int id, string name, string buildingPart);
}