namespace WFC.Models;

public static class TileTypes
{
    // Константы для ID тайлов
    public const int EARTH = 0;
    public const int WATER = 1;
    public const int SHORE_LEFT_WATER_RIGHT = 2; // Вода слева, земля справа
    public const int SHORE_RIGHT_WATER_LEFT = 3; // Вода справа, земля слева

    // Получить тип поверхности для определенного тайла и направления
    public static SurfaceType GetSurfaceType(int tileId, string direction)
    {
        switch (tileId)
        {
            case EARTH:
                return SurfaceType.Land;

            case WATER:
                return SurfaceType.Water;

            case SHORE_LEFT_WATER_RIGHT:
                // Тайл "Shore Left Water Right" (ID 2) имеет воду слева и землю справа
                if (direction == "left")
                    return SurfaceType.Water;
                else
                    return SurfaceType.Land;

            case SHORE_RIGHT_WATER_LEFT:
                // Тайл "Shore Right Water Left" (ID 3) имеет землю слева и воду справа
                if (direction == "right")
                    return SurfaceType.Water;
                else
                    return SurfaceType.Land;

            default:
                return SurfaceType.Land;
        }
    }

    // Проверка, имеет ли тайл воду на указанной стороне
    public static bool HasWaterOnSide(int tileId, string direction)
    {
        return GetSurfaceType(tileId, direction) == SurfaceType.Water;
    }

    // Проверка, имеет ли тайл землю на указанной стороне
    public static bool HasLandOnSide(int tileId, string direction)
    {
        return GetSurfaceType(tileId, direction) == SurfaceType.Land;
    }
}