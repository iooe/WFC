namespace WFC.Models;

public static class TileTypes
{
    // New tile type constants
    public const int GRASS = 0;
    public const int FLOWERS = 1;
    public const int PAVEMENT = 2;
    
    // Surface types for different tiles
    public static SurfaceType GetSurfaceType(int tileId, string direction)
    {
        switch (tileId)
        {
            case GRASS:
                return SurfaceType.Grass;
                
            case FLOWERS:
                return SurfaceType.Flowers;
                
            case PAVEMENT:
                return SurfaceType.Pavement;
                
            default:
                return SurfaceType.Grass; // Default to grass
        }
    }
    
    // Helper methods to check tile compatibility
    public static bool CanConnect(int tileId1, int tileId2, string direction)
    {
        var surface1 = GetSurfaceType(tileId1, direction);
        var surface2 = GetSurfaceType(tileId2, GetOppositeDirection(direction));
        
        // Define which surfaces can connect to each other
        return true; // By default, allow all connections
    }
    
    // Get opposite direction
    private static string GetOppositeDirection(string direction)
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
}