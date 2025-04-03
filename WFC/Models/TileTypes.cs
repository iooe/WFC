namespace WFC.Models;

public static class TileTypes
{
    // Basic tile types
    public const int GRASS = 0;
    public const int FLOWERS = 1;
    public const int PAVEMENT = 2;
    
    // Transition tiles for pavement and grass
    public const int PAVEMENT_GRASS_LEFT = 3;     // Pavement with grass on left
    public const int PAVEMENT_GRASS_RIGHT = 4;    // Pavement with grass on right
    public const int PAVEMENT_GRASS_TOP = 5;      // Pavement with grass on top
    public const int PAVEMENT_GRASS_BOTTOM = 6;   // Pavement with grass on bottom
    
    // Corner transition tiles
    public const int PAVEMENT_GRASS_TOP_LEFT = 7;     // Pavement with grass on top-left corner
    public const int PAVEMENT_GRASS_TOP_RIGHT = 8;    // Pavement with grass on top-right corner
    public const int PAVEMENT_GRASS_BOTTOM_LEFT = 9;  // Pavement with grass on bottom-left corner
    public const int PAVEMENT_GRASS_BOTTOM_RIGHT = 10; // Pavement with grass on bottom-right corner
    
    // Surface types for different tiles and directions
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
                
            // Transition tiles have different surface types based on direction
            case PAVEMENT_GRASS_LEFT:
                return direction == "left" ? SurfaceType.Grass : SurfaceType.Pavement;
                
            case PAVEMENT_GRASS_RIGHT:
                return direction == "right" ? SurfaceType.Grass : SurfaceType.Pavement;
                
            case PAVEMENT_GRASS_TOP:
                return direction == "up" ? SurfaceType.Grass : SurfaceType.Pavement;
                
            case PAVEMENT_GRASS_BOTTOM:
                return direction == "down" ? SurfaceType.Grass : SurfaceType.Pavement;
                
            // Corner transition tiles are more complex
            case PAVEMENT_GRASS_TOP_LEFT:
                return (direction == "up" || direction == "left") ? SurfaceType.Grass : SurfaceType.Pavement;
                
            case PAVEMENT_GRASS_TOP_RIGHT:
                return (direction == "up" || direction == "right") ? SurfaceType.Grass : SurfaceType.Pavement;
                
            case PAVEMENT_GRASS_BOTTOM_LEFT:
                return (direction == "down" || direction == "left") ? SurfaceType.Grass : SurfaceType.Pavement;
                
            case PAVEMENT_GRASS_BOTTOM_RIGHT:
                return (direction == "down" || direction == "right") ? SurfaceType.Grass : SurfaceType.Pavement;
                
            default:
                return SurfaceType.Grass; // Default to grass
        }
    }
    
    // Get the appropriate transition tile based on neighboring tiles
    public static int GetTransitionTile(bool isGrassTop, bool isGrassRight, bool isGrassBottom, bool isGrassLeft)
    {
        // Check for corner cases
        if (isGrassTop && isGrassLeft && !isGrassRight && !isGrassBottom)
            return PAVEMENT_GRASS_TOP_LEFT;
            
        if (isGrassTop && isGrassRight && !isGrassLeft && !isGrassBottom)
            return PAVEMENT_GRASS_TOP_RIGHT;
            
        if (isGrassBottom && isGrassLeft && !isGrassRight && !isGrassTop)
            return PAVEMENT_GRASS_BOTTOM_LEFT;
            
        if (isGrassBottom && isGrassRight && !isGrassLeft && !isGrassTop)
            return PAVEMENT_GRASS_BOTTOM_RIGHT;
        
        // Check for edge cases
        if (isGrassTop && !isGrassRight && !isGrassBottom && !isGrassLeft)
            return PAVEMENT_GRASS_TOP;
            
        if (!isGrassTop && isGrassRight && !isGrassBottom && !isGrassLeft)
            return PAVEMENT_GRASS_RIGHT;
            
        if (!isGrassTop && !isGrassRight && isGrassBottom && !isGrassLeft)
            return PAVEMENT_GRASS_BOTTOM;
            
        if (!isGrassTop && !isGrassRight && !isGrassBottom && isGrassLeft)
            return PAVEMENT_GRASS_LEFT;
            
        // For more complex cases, we default to basic tiles
        if (isGrassTop && isGrassRight && isGrassBottom && isGrassLeft)
            return GRASS; // Surrounded by grass, use grass
            
        return PAVEMENT; // Default to pavement for other cases
    }
    
    // Helper method to check if a tile is grass or flowers (which should connect seamlessly)
    public static bool IsGrassLike(int tileId)
    {
        return tileId == GRASS || tileId == FLOWERS;
    }
}