namespace WFC.Services;

using WFC.Models;

public class RuleGenerator
{
    // Method to generate tile connection rules
    public static void GenerateRules(WFCSettings settings)
    {
        settings.Rules.Clear();

        // Rules for GRASS (ID 0)
        AddRule(settings, TileTypes.GRASS, "left", new[]
        {
            (TileTypes.GRASS, 1.0f),     // Grass can connect to grass (high weight)
            (TileTypes.FLOWERS, 0.7f),   // Grass can connect to flowers (medium-high weight)
            (TileTypes.PAVEMENT, 0.5f)   // Grass can connect to pavement (medium weight)
        });

        AddRule(settings, TileTypes.GRASS, "right", new[]
        {
            (TileTypes.GRASS, 1.0f),     // Grass can connect to grass (high weight)
            (TileTypes.FLOWERS, 0.7f),   // Grass can connect to flowers (medium-high weight)
            (TileTypes.PAVEMENT, 0.5f)   // Grass can connect to pavement (medium weight)
        });

        AddRule(settings, TileTypes.GRASS, "up", new[]
        {
            (TileTypes.GRASS, 1.0f),     // Grass can connect to grass (high weight)
            (TileTypes.FLOWERS, 0.7f),   // Grass can connect to flowers (medium-high weight)
            (TileTypes.PAVEMENT, 0.5f)   // Grass can connect to pavement (medium weight)
        });

        AddRule(settings, TileTypes.GRASS, "down", new[]
        {
            (TileTypes.GRASS, 1.0f),     // Grass can connect to grass (high weight)
            (TileTypes.FLOWERS, 0.7f),   // Grass can connect to flowers (medium-high weight)
            (TileTypes.PAVEMENT, 0.5f)   // Grass can connect to pavement (medium weight)
        });

        // Rules for FLOWERS (ID 1)
        AddRule(settings, TileTypes.FLOWERS, "left", new[]
        {
            (TileTypes.GRASS, 0.8f),     // Flowers can connect to grass (high weight)
            (TileTypes.FLOWERS, 1.0f),   // Flowers can connect to flowers (highest weight)
            (TileTypes.PAVEMENT, 0.3f)   // Flowers can connect to pavement (lower weight)
        });

        AddRule(settings, TileTypes.FLOWERS, "right", new[]
        {
            (TileTypes.GRASS, 0.8f),     // Flowers can connect to grass (high weight)
            (TileTypes.FLOWERS, 1.0f),   // Flowers can connect to flowers (highest weight)
            (TileTypes.PAVEMENT, 0.3f)   // Flowers can connect to pavement (lower weight)
        });

        AddRule(settings, TileTypes.FLOWERS, "up", new[]
        {
            (TileTypes.GRASS, 0.8f),     // Flowers can connect to grass (high weight)
            (TileTypes.FLOWERS, 1.0f),   // Flowers can connect to flowers (highest weight)
            (TileTypes.PAVEMENT, 0.3f)   // Flowers can connect to pavement (lower weight)
        });

        AddRule(settings, TileTypes.FLOWERS, "down", new[]
        {
            (TileTypes.GRASS, 0.8f),     // Flowers can connect to grass (high weight)
            (TileTypes.FLOWERS, 1.0f),   // Flowers can connect to flowers (highest weight)
            (TileTypes.PAVEMENT, 0.3f)   // Flowers can connect to pavement (lower weight)
        });

        // Rules for PAVEMENT (ID 2)
        AddRule(settings, TileTypes.PAVEMENT, "left", new[]
        {
            (TileTypes.GRASS, 0.5f),     // Pavement can connect to grass (medium weight)
            (TileTypes.FLOWERS, 0.3f),   // Pavement can connect to flowers (lower weight)
            (TileTypes.PAVEMENT, 1.0f)   // Pavement can connect to pavement (highest weight)
        });

        AddRule(settings, TileTypes.PAVEMENT, "right", new[]
        {
            (TileTypes.GRASS, 0.5f),     // Pavement can connect to grass (medium weight)
            (TileTypes.FLOWERS, 0.3f),   // Pavement can connect to flowers (lower weight)
            (TileTypes.PAVEMENT, 1.0f)   // Pavement can connect to pavement (highest weight)
        });

        AddRule(settings, TileTypes.PAVEMENT, "up", new[]
        {
            (TileTypes.GRASS, 0.5f),     // Pavement can connect to grass (medium weight)
            (TileTypes.FLOWERS, 0.3f),   // Pavement can connect to flowers (lower weight)
            (TileTypes.PAVEMENT, 1.0f)   // Pavement can connect to pavement (highest weight)
        });

        AddRule(settings, TileTypes.PAVEMENT, "down", new[]
        {
            (TileTypes.GRASS, 0.5f),     // Pavement can connect to grass (medium weight)
            (TileTypes.FLOWERS, 0.3f),   // Pavement can connect to flowers (lower weight)
            (TileTypes.PAVEMENT, 1.0f)   // Pavement can connect to pavement (highest weight)
        });
    }

    // Add rule to settings
    private static void AddRule(WFCSettings settings, int fromTileId, string direction,
        (int state, float weight)[] possibleStates)
    {
        var key = (fromTileId, direction);
        settings.Rules[key] = new List<(int state, float weight)>(possibleStates);
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