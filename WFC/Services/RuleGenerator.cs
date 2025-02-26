namespace WFC.Services;

using WFC.Models;

public class RuleGenerator
{
    // Method to generate tile connection rules
    public static void GenerateRules(WFCSettings settings)
    {
        settings.Rules.Clear();

        // Using more balanced weights to enable diverse patterns

        // Rules for EARTH (ID 0)
        AddRule(settings, TileTypes.EARTH, "left", new[]
        {
            (TileTypes.EARTH, 1.0f),                    // Land can connect to land (high weight)
            (TileTypes.SHORE_RIGHT_WATER_LEFT, 0.7f)    // Land can transition to shore (medium-high weight)
        });

        AddRule(settings, TileTypes.EARTH, "right", new[]
        {
            (TileTypes.EARTH, 1.0f),                   // Land can connect to land (high weight)
            (TileTypes.SHORE_LEFT_WATER_RIGHT, 0.7f)   // Land can transition to shore (medium-high weight)
        });

        AddRule(settings, TileTypes.EARTH, "up", new[]
        {
            (TileTypes.EARTH, 1.0f),                    // Land above land (high weight)
            (TileTypes.SHORE_LEFT_WATER_RIGHT, 0.5f),   // Shore tiles above land (medium weight)
            (TileTypes.SHORE_RIGHT_WATER_LEFT, 0.5f)    // Shore tiles above land (medium weight)
        });

        AddRule(settings, TileTypes.EARTH, "down", new[]
        {
            (TileTypes.EARTH, 1.0f),                    // Land below land (high weight)
            (TileTypes.SHORE_LEFT_WATER_RIGHT, 0.5f),   // Shore tiles below land (medium weight)
            (TileTypes.SHORE_RIGHT_WATER_LEFT, 0.5f)    // Shore tiles below land (medium weight)
        });

        // Rules for WATER (ID 1)
        AddRule(settings, TileTypes.WATER, "left", new[]
        {
            (TileTypes.WATER, 1.0f),                    // Water next to water (high weight)
            (TileTypes.SHORE_LEFT_WATER_RIGHT, 0.7f)    // Water can transition to shore (medium-high weight)
        });

        AddRule(settings, TileTypes.WATER, "right", new[]
        {
            (TileTypes.WATER, 1.0f),                    // Water next to water (high weight)
            (TileTypes.SHORE_RIGHT_WATER_LEFT, 0.7f)    // Water can transition to shore (medium-high weight)
        });

        AddRule(settings, TileTypes.WATER, "up", new[]
        {
            (TileTypes.WATER, 1.0f),                    // Water above water (high weight)
            (TileTypes.SHORE_LEFT_WATER_RIGHT, 0.5f),   // Shore tiles above water (medium weight)
            (TileTypes.SHORE_RIGHT_WATER_LEFT, 0.5f)    // Shore tiles above water (medium weight)
        });

        AddRule(settings, TileTypes.WATER, "down", new[]
        {
            (TileTypes.WATER, 1.0f),                    // Water below water (high weight)
            (TileTypes.SHORE_LEFT_WATER_RIGHT, 0.5f),   // Shore tiles below water (medium weight)
            (TileTypes.SHORE_RIGHT_WATER_LEFT, 0.5f)    // Shore tiles below water (medium weight)
        });

        // Rules for SHORE_LEFT_WATER_RIGHT (ID 2) - water on left, land on right
        AddRule(settings, TileTypes.SHORE_LEFT_WATER_RIGHT, "left", new[]
        {
            (TileTypes.WATER, 1.0f),                        // Water to the left of this shore (high weight)
            (TileTypes.SHORE_LEFT_WATER_RIGHT, 0.5f),       // Same shore type to left (medium weight)
        });

        AddRule(settings, TileTypes.SHORE_LEFT_WATER_RIGHT, "right", new[]
        {
            (TileTypes.EARTH, 1.0f),                        // Land to the right of this shore (high weight)
            (TileTypes.SHORE_RIGHT_WATER_LEFT, 0.5f),       // Other shore type to right (medium weight)
        });

        AddRule(settings, TileTypes.SHORE_LEFT_WATER_RIGHT, "up", new[]
        {
            (TileTypes.SHORE_LEFT_WATER_RIGHT, 0.8f),       // Same shore type above (high weight)
            (TileTypes.EARTH, 0.6f),                        // Land above (medium-high weight)
            (TileTypes.WATER, 0.6f),                        // Water above (medium-high weight)
            (TileTypes.SHORE_RIGHT_WATER_LEFT, 0.4f)        // Other shore type above (medium-low weight)
        });

        AddRule(settings, TileTypes.SHORE_LEFT_WATER_RIGHT, "down", new[]
        {
            (TileTypes.SHORE_LEFT_WATER_RIGHT, 0.8f),       // Same shore type below (high weight)
            (TileTypes.EARTH, 0.6f),                        // Land below (medium-high weight)
            (TileTypes.WATER, 0.6f),                        // Water below (medium-high weight)
            (TileTypes.SHORE_RIGHT_WATER_LEFT, 0.4f)        // Other shore type below (medium-low weight)
        });

        // Rules for SHORE_RIGHT_WATER_LEFT (ID 3) - land on left, water on right
        AddRule(settings, TileTypes.SHORE_RIGHT_WATER_LEFT, "left", new[]
        {
            (TileTypes.EARTH, 1.0f),                        // Land to the left of this shore (high weight)
            (TileTypes.SHORE_LEFT_WATER_RIGHT, 0.5f),       // Other shore type to left (medium weight)
        });

        AddRule(settings, TileTypes.SHORE_RIGHT_WATER_LEFT, "right", new[]
        {
            (TileTypes.WATER, 1.0f),                        // Water to the right of this shore (high weight)
            (TileTypes.SHORE_RIGHT_WATER_LEFT, 0.5f),       // Same shore type to right (medium weight)
        });

        AddRule(settings, TileTypes.SHORE_RIGHT_WATER_LEFT, "up", new[]
        {
            (TileTypes.SHORE_RIGHT_WATER_LEFT, 0.8f),       // Same shore type above (high weight)
            (TileTypes.EARTH, 0.6f),                        // Land above (medium-high weight)
            (TileTypes.WATER, 0.6f),                        // Water above (medium-high weight)
            (TileTypes.SHORE_LEFT_WATER_RIGHT, 0.4f)        // Other shore type above (medium-low weight)
        });

        AddRule(settings, TileTypes.SHORE_RIGHT_WATER_LEFT, "down", new[]
        {
            (TileTypes.SHORE_RIGHT_WATER_LEFT, 0.8f),       // Same shore type below (high weight)
            (TileTypes.EARTH, 0.6f),                        // Land below (medium-high weight)
            (TileTypes.WATER, 0.6f),                        // Water below (medium-high weight)
            (TileTypes.SHORE_LEFT_WATER_RIGHT, 0.4f)        // Other shore type below (medium-low weight)
        });

        // Validate rules
        ValidateRules(settings);
    }

    // Add rule to settings
    private static void AddRule(WFCSettings settings, int fromTileId, string direction,
        (int state, float weight)[] possibleStates)
    {
        var key = (fromTileId, direction);
        settings.Rules[key] = new List<(int state, float weight)>(possibleStates);
    }

    // Validate rule consistency (water connects to water, land to land)
    private static void ValidateRules(WFCSettings settings)
    {
        bool hasErrors = false;

        foreach (var rule in settings.Rules)
        {
            var (tileId, direction) = rule.Key;
            var allowedStates = rule.Value;

            var fromSurface = TileTypes.GetSurfaceType(tileId, direction);

            foreach (var tuple in allowedStates)
            {
                var toTileId = tuple.Item1;
                var weight = tuple.Item2;
                
                var toSurface = TileTypes.GetSurfaceType(toTileId, GetOppositeDirection(direction));

                if (fromSurface != toSurface)
                {
                    hasErrors = true;
                    Console.WriteLine($"VALIDATION ERROR: Rule {tileId} -> {toTileId} in direction {direction} " +
                                      $"has surface mismatch! {fromSurface} != {toSurface}");
                }
            }
        }

        if (hasErrors)
        {
            Console.WriteLine("WARNING: Rules have validation errors. Water/land continuity might be broken!");
        }
        else
        {
            Console.WriteLine("Rules validation passed. Water/land continuity should be preserved.");
        }
    }

    // Get opposite direction
    private static string GetOppositeDirection(string direction)
    {
        return direction switch
        {
            "left" => "right",
            "right" => "left",
            "up" => "down",
            "down" => "up",
            _ => throw new ArgumentException($"Unknown direction: {direction}")
        };
    }
}