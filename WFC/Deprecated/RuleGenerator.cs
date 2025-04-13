namespace WFC.Services;

using WFC.Models;

public class RuleGenerator
{
    // Method to generate tile connection rules
    public static void GenerateRules(WFCSettings settings)
    {
        settings.Rules.Clear();

        // Generate rules for grass and pavement tiles
        GenerateBasicRules(settings);
        
        // Generate rules for building tiles
        GenerateBuildingRules(settings);
    }
    
    // Generate rules for grass, flowers, and pavement
    private static void GenerateBasicRules(WFCSettings settings)
    {
        // ================ GRASS RULES ================
        AddRule(settings, TileTypes.GRASS, "left", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Grass can connect to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Grass can connect to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_RIGHT, 0.7f),        // Grass can connect to pavement transition (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.6f),    // Grass can connect to corner transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.6f)  // Grass can connect to corner transition (medium weight)
        });

        AddRule(settings, TileTypes.GRASS, "right", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Grass can connect to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Grass can connect to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_LEFT, 0.7f),         // Grass can connect to pavement transition (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.6f),     // Grass can connect to corner transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.6f)   // Grass can connect to corner transition (medium weight)
        });

        AddRule(settings, TileTypes.GRASS, "up", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Grass can connect to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Grass can connect to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM, 0.7f),       // Grass can connect to pavement transition (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.6f),  // Grass can connect to corner transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.6f)  // Grass can connect to corner transition (medium weight)
        });

        AddRule(settings, TileTypes.GRASS, "down", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Grass can connect to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Grass can connect to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP, 0.7f),          // Grass can connect to pavement transition (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.6f),     // Grass can connect to corner transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.6f)     // Grass can connect to corner transition (medium weight)
        });

        // ================ FLOWERS RULES ================
        // Flowers have similar rules to grass
        AddRule(settings, TileTypes.FLOWERS, "left", new[]
        {
            (TileTypes.FLOWERS, 1.0f),                     // Flowers can connect to flowers (highest weight)
            (TileTypes.GRASS, 0.9f),                       // Flowers can connect to grass (high weight)
            (TileTypes.PAVEMENT_GRASS_RIGHT, 0.6f),        // Flowers can connect to pavement transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.5f),    // Flowers can connect to corner transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.5f)  // Flowers can connect to corner transition (medium weight)
        });

        AddRule(settings, TileTypes.FLOWERS, "right", new[]
        {
            (TileTypes.FLOWERS, 1.0f),                     // Flowers can connect to flowers (highest weight)
            (TileTypes.GRASS, 0.9f),                       // Flowers can connect to grass (high weight)
            (TileTypes.PAVEMENT_GRASS_LEFT, 0.6f),         // Flowers can connect to pavement transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.5f),     // Flowers can connect to corner transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.5f)   // Flowers can connect to corner transition (medium weight)
        });

        AddRule(settings, TileTypes.FLOWERS, "up", new[]
        {
            (TileTypes.FLOWERS, 1.0f),                     // Flowers can connect to flowers (highest weight)
            (TileTypes.GRASS, 0.9f),                       // Flowers can connect to grass (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM, 0.6f),       // Flowers can connect to pavement transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.5f),  // Flowers can connect to corner transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.5f)  // Flowers can connect to corner transition (medium weight)
        });

        AddRule(settings, TileTypes.FLOWERS, "down", new[]
        {
            (TileTypes.FLOWERS, 1.0f),                     // Flowers can connect to flowers (highest weight)
            (TileTypes.GRASS, 0.9f),                       // Flowers can connect to grass (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP, 0.6f),          // Flowers can connect to pavement transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.5f),     // Flowers can connect to corner transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.5f)     // Flowers can connect to corner transition (medium weight)
        });

        // ================ PAVEMENT RULES ================
        AddRule(settings, TileTypes.PAVEMENT, "left", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Pavement can connect to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_LEFT, 0.7f),         // Pavement can connect to pavement transition (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.6f),     // Pavement can connect to corner transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.6f),  // Pavement can connect to corner transition (medium weight)
            (TileTypes.WALL_FRONT_RIGHT_END, 0.5f),        // Pavement can connect to wall right end (medium weight)
            (TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT, 0.4f) // Pavement can connect to wall corner (low-medium weight)
        });

        AddRule(settings, TileTypes.PAVEMENT, "right", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Pavement can connect to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_RIGHT, 0.7f),        // Pavement can connect to pavement transition (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.6f),    // Pavement can connect to corner transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.6f), // Pavement can connect to corner transition (medium weight)
            (TileTypes.WALL_FRONT_LEFT_END, 0.5f),         // Pavement can connect to wall left end (medium weight)
            (TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT, 0.4f) // Pavement can connect to wall corner (low-medium weight)
        });

        AddRule(settings, TileTypes.PAVEMENT, "up", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Pavement can connect to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP, 0.7f),          // Pavement can connect to pavement transition (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.6f),     // Pavement can connect to corner transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.6f)     // Pavement can connect to corner transition (medium weight)
        });

        AddRule(settings, TileTypes.PAVEMENT, "down", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Pavement can connect to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM, 0.7f),       // Pavement can connect to pavement transition (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.6f),  // Pavement can connect to corner transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.6f), // Pavement can connect to corner transition (medium weight)
            (TileTypes.WALL_FRONT_BOTTOM_END, 0.5f),       // Pavement can connect to wall bottom (medium weight)
            (TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT, 0.4f), // Pavement can connect to wall corner (low-medium weight)
            (TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT, 0.4f) // Pavement can connect to wall corner (low-medium weight)
        });

        // ================ TRANSITION TILES: PAVEMENT WITH GRASS EDGE ================
        
        // ----- PAVEMENT_GRASS_LEFT (Pavement with grass on left) -----
        AddRule(settings, TileTypes.PAVEMENT_GRASS_LEFT, "left", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Left side connects to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Left side connects to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.5f),     // Can connect to corner with matching sides (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.5f)   // Can connect to corner with matching sides (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_LEFT, "right", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Right side connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_RIGHT, 0.5f),        // Can connect to opposite transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.4f),    // Can connect to corner (medium-low weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.4f),  // Can connect to corner (medium-low weight)
            (TileTypes.WALL_FRONT_LEFT_END, 0.3f)          // Can connect to wall left end (low weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_LEFT, "up", new[]
        {
            (TileTypes.PAVEMENT, 0.8f),                    // Usually connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_LEFT, 1.0f),         // Best connects to same type (highest weight)
            (TileTypes.PAVEMENT_GRASS_TOP, 0.6f),          // Can connect to top transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.7f)      // Can connect to corner with matching sides (medium-high weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_LEFT, "down", new[]
        {
            (TileTypes.PAVEMENT, 0.8f),                    // Usually connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_LEFT, 1.0f),         // Best connects to same type (highest weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM, 0.6f),       // Can connect to bottom transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.7f)   // Can connect to corner with matching sides (medium-high weight)
        });

        // ----- PAVEMENT_GRASS_RIGHT (Pavement with grass on right) -----
        AddRule(settings, TileTypes.PAVEMENT_GRASS_RIGHT, "right", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Right side connects to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Right side connects to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.5f),    // Can connect to corner with matching sides (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.5f)  // Can connect to corner with matching sides (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_RIGHT, "left", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Left side connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_LEFT, 0.5f),         // Can connect to opposite transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.4f),     // Can connect to corner (medium-low weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.4f)   // Can connect to corner (medium-low weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_RIGHT, "up", new[]
        {
            (TileTypes.PAVEMENT, 0.8f),                    // Usually connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_RIGHT, 1.0f),        // Best connects to same type (highest weight)
            (TileTypes.PAVEMENT_GRASS_TOP, 0.6f),          // Can connect to top transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.7f)     // Can connect to corner with matching sides (medium-high weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_RIGHT, "down", new[]
        {
            (TileTypes.PAVEMENT, 0.8f),                    // Usually connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_RIGHT, 1.0f),        // Best connects to same type (highest weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM, 0.6f),       // Can connect to bottom transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.7f)  // Can connect to corner with matching sides (medium-high weight)
        });

        // ----- PAVEMENT_GRASS_TOP (Pavement with grass on top) -----
        AddRule(settings, TileTypes.PAVEMENT_GRASS_TOP, "up", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Top side connects to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Top side connects to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.5f),     // Can connect to corner with matching sides (medium weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.5f)     // Can connect to corner with matching sides (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_TOP, "down", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Bottom side connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM, 0.5f),       // Can connect to opposite transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.4f),  // Can connect to corner (medium-low weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.4f)  // Can connect to corner (medium-low weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_TOP, "left", new[]
        {
            (TileTypes.PAVEMENT, 0.8f),                    // Usually connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP, 1.0f),          // Best connects to same type (highest weight)
            (TileTypes.PAVEMENT_GRASS_LEFT, 0.6f),         // Can connect to left transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.7f)      // Can connect to corner with matching sides (medium-high weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_TOP, "right", new[]
        {
            (TileTypes.PAVEMENT, 0.8f),                    // Usually connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP, 1.0f),          // Best connects to same type (highest weight)
            (TileTypes.PAVEMENT_GRASS_RIGHT, 0.6f),        // Can connect to right transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.7f)     // Can connect to corner with matching sides (medium-high weight)
        });

        // ----- PAVEMENT_GRASS_BOTTOM (Pavement with grass on bottom) -----
        AddRule(settings, TileTypes.PAVEMENT_GRASS_BOTTOM, "down", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Bottom side connects to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Bottom side connects to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.5f),  // Can connect to corner with matching sides (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.5f)  // Can connect to corner with matching sides (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_BOTTOM, "up", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Top side connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP, 0.5f),          // Can connect to opposite transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.4f),     // Can connect to corner (medium-low weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.4f)     // Can connect to corner (medium-low weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_BOTTOM, "left", new[]
        {
            (TileTypes.PAVEMENT, 0.8f),                    // Usually connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM, 1.0f),       // Best connects to same type (highest weight)
            (TileTypes.PAVEMENT_GRASS_LEFT, 0.6f),         // Can connect to left transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.7f)   // Can connect to corner with matching sides (medium-high weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_BOTTOM, "right", new[]
        {
            (TileTypes.PAVEMENT, 0.8f),                    // Usually connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM, 1.0f),       // Best connects to same type (highest weight)
            (TileTypes.PAVEMENT_GRASS_RIGHT, 0.6f),        // Can connect to right transition (medium weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.7f)  // Can connect to corner with matching sides (medium-high weight)
        });

        // ----- PAVEMENT_GRASS_TOP_LEFT (Pavement with grass on top-left corner) -----
        AddRule(settings, TileTypes.PAVEMENT_GRASS_TOP_LEFT, "left", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Left side connects to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Left side connects to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.6f)   // Can connect to left transition with matching left side (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_TOP_LEFT, "up", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Top side connects to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Top side connects to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.6f)     // Can connect to top transition with matching top side (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_TOP_LEFT, "right", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Right side connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.7f),    // Can connect to corner with matching top side (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_RIGHT, 0.5f)         // Can connect to right transition (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_TOP_LEFT, "down", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Bottom side connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.7f),  // Can connect to corner with matching left side (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM, 0.5f)        // Can connect to bottom transition (medium weight)
        });

        // ----- PAVEMENT_GRASS_TOP_RIGHT (Pavement with grass on top-right corner) -----
        AddRule(settings, TileTypes.PAVEMENT_GRASS_TOP_RIGHT, "right", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Right side connects to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Right side connects to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.6f)  // Can connect to right transition with matching right side (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_TOP_RIGHT, "up", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Top side connects to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Top side connects to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.6f)      // Can connect to top transition with matching top side (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_TOP_RIGHT, "left", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Left side connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.7f),     // Can connect to corner with matching top side (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_LEFT, 0.5f)          // Can connect to left transition (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_TOP_RIGHT, "down", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Bottom side connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.7f), // Can connect to corner with matching right side (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM, 0.5f)        // Can connect to bottom transition (medium weight)
        });

        // ----- PAVEMENT_GRASS_BOTTOM_LEFT (Pavement with grass on bottom-left corner) -----
        AddRule(settings, TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, "left", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Left side connects to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Left side connects to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.6f)      // Can connect to left transition with matching left side (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, "down", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Bottom side connects to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Bottom side connects to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.6f)  // Can connect to bottom transition with matching bottom side (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, "right", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Right side connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, 0.7f), // Can connect to corner with matching bottom side (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_RIGHT, 0.5f)         // Can connect to right transition (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, "up", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Top side connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_LEFT, 0.7f),     // Can connect to corner with matching left side (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_TOP, 0.5f)           // Can connect to top transition (medium weight)
        });

        // ----- PAVEMENT_GRASS_BOTTOM_RIGHT (Pavement with grass on bottom-right corner) -----
        AddRule(settings, TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, "right", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Right side connects to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Right side connects to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.6f)     // Can connect to right transition with matching right side (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, "down", new[]
        {
            (TileTypes.GRASS, 1.0f),                       // Bottom side connects to grass (high weight)
            (TileTypes.FLOWERS, 0.8f),                     // Bottom side connects to flowers (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.6f)   // Can connect to bottom transition with matching bottom side (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, "left", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Left side connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_BOTTOM_LEFT, 0.7f),  // Can connect to corner with matching bottom side (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_LEFT, 0.5f)          // Can connect to left transition (medium weight)
        });
        
        AddRule(settings, TileTypes.PAVEMENT_GRASS_BOTTOM_RIGHT, "up", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Top side connects to pavement (high weight)
            (TileTypes.PAVEMENT_GRASS_TOP_RIGHT, 0.7f),    // Can connect to corner with matching right side (medium-high weight)
            (TileTypes.PAVEMENT_GRASS_TOP, 0.5f)           // Can connect to top transition (medium weight)
        });
    }
    
    // Generate rules for building tiles (walls, windows)
    private static void GenerateBuildingRules(WFCSettings settings)
    {
        // ================ WALL FRONT RULES ================
        
        // ----- WALL_FRONT_MIDDLE (Standard wall piece) -----
        AddRule(settings, TileTypes.WALL_FRONT_MIDDLE, "left", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),             // Can connect to another middle piece
            (TileTypes.WALL_FRONT_CORNER_TOP_RIGHT, 0.8f),   // Can connect to top-right corner
            (TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT, 0.8f), // Can connect to bottom-right corner
            (TileTypes.WALL_FRONT_RIGHT_END, 0.7f),          // Can connect to right end piece
            (TileTypes.WALL_WINDOW_TOP, 0.6f),               // Can connect to window top
            (TileTypes.WALL_WINDOW_BOTTOM, 0.6f)             // Can connect to window bottom
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_MIDDLE, "right", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),             // Can connect to another middle piece
            (TileTypes.WALL_FRONT_CORNER_TOP_LEFT, 0.8f),    // Can connect to top-left corner
            (TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT, 0.8f), // Can connect to bottom-left corner
            (TileTypes.WALL_FRONT_LEFT_END, 0.7f),           // Can connect to left end piece
            (TileTypes.WALL_WINDOW_TOP, 0.6f),               // Can connect to window top
            (TileTypes.WALL_WINDOW_BOTTOM, 0.6f)             // Can connect to window bottom
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_MIDDLE, "up", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),             // Can connect to another middle piece
            (TileTypes.WALL_FRONT_TOP_END, 0.9f),            // Can connect to top end piece
            (TileTypes.WALL_WINDOW_TOP, 0.8f)                // Can connect to window top
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_MIDDLE, "down", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),             // Can connect to another middle piece
            (TileTypes.WALL_FRONT_BOTTOM_END, 0.9f),         // Can connect to bottom end piece
            (TileTypes.WALL_WINDOW_BOTTOM, 0.8f),            // Can connect to window bottom
            (TileTypes.PAVEMENT, 0.6f)                       // Can connect to pavement
        });
        
        // ----- WALL_FRONT_CORNER_TOP_LEFT (Wall top-left corner) -----
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_TOP_LEFT, "left", new[]
        {
            (TileTypes.WALL_FRONT_TOP_END, 0.8f),            // Can connect to top end
            (TileTypes.GRASS, 0.6f),                         // Can connect to grass
            (TileTypes.FLOWERS, 0.5f),                       // Can connect to flowers
            (TileTypes.PAVEMENT, 0.7f)                       // Can connect to pavement
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_TOP_LEFT, "right", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),            // Best connects to wall middle
            (TileTypes.WALL_WINDOW_TOP, 0.8f)               // Can connect to window top
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_TOP_LEFT, "up", new[]
        {
            (TileTypes.GRASS, 0.8f),                        // Can connect to grass
            (TileTypes.FLOWERS, 0.7f),                      // Can connect to flowers
            (TileTypes.PAVEMENT, 0.9f)                      // Better connects to pavement
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_TOP_LEFT, "down", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),            // Best connects to wall middle
            (TileTypes.WALL_FRONT_LEFT_END, 0.9f),          // Can connect to wall left end
            (TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT, 0.8f) // Can connect to bottom-left corner
        });
        
        // ----- WALL_FRONT_CORNER_TOP_RIGHT (Wall top-right corner) -----
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_TOP_RIGHT, "right", new[]
        {
            (TileTypes.WALL_FRONT_TOP_END, 0.8f),           // Can connect to top end
            (TileTypes.GRASS, 0.6f),                        // Can connect to grass
            (TileTypes.FLOWERS, 0.5f),                      // Can connect to flowers
            (TileTypes.PAVEMENT, 0.7f)                      // Can connect to pavement
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_TOP_RIGHT, "left", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),            // Best connects to wall middle
            (TileTypes.WALL_WINDOW_TOP, 0.8f)               // Can connect to window top
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_TOP_RIGHT, "up", new[]
        {
            (TileTypes.GRASS, 0.8f),                        // Can connect to grass
            (TileTypes.FLOWERS, 0.7f),                      // Can connect to flowers
            (TileTypes.PAVEMENT, 0.9f)                      // Better connects to pavement
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_TOP_RIGHT, "down", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),            // Best connects to wall middle
            (TileTypes.WALL_FRONT_RIGHT_END, 0.9f),         // Can connect to wall right end
            (TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT, 0.8f) // Can connect to bottom-right corner
        });
        
        // ----- WALL_FRONT_CORNER_BOTTOM_LEFT (Wall bottom-left corner) -----
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT, "left", new[]
        {
            (TileTypes.GRASS, 0.8f),                        // Can connect to grass
            (TileTypes.FLOWERS, 0.7f),                      // Can connect to flowers
            (TileTypes.PAVEMENT, 1.0f)                      // Best connects to pavement
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT, "right", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),            // Best connects to wall middle
            (TileTypes.WALL_WINDOW_BOTTOM, 0.8f),           // Can connect to window bottom
            (TileTypes.WALL_FRONT_BOTTOM_END, 0.7f)         // Can connect to wall bottom end
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT, "up", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),            // Best connects to wall middle
            (TileTypes.WALL_FRONT_LEFT_END, 0.9f),          // Can connect to wall left end
            (TileTypes.WALL_FRONT_CORNER_TOP_LEFT, 0.8f)    // Can connect to top-left corner
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT, "down", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                     // Best connects to pavement
            (TileTypes.GRASS, 0.7f),                        // Can connect to grass
            (TileTypes.FLOWERS, 0.6f)                       // Can connect to flowers
        });
        
        // ----- WALL_FRONT_CORNER_BOTTOM_RIGHT (Wall bottom-right corner) -----
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT, "right", new[]
        {
            (TileTypes.GRASS, 0.8f),                        // Can connect to grass
            (TileTypes.FLOWERS, 0.7f),                      // Can connect to flowers
            (TileTypes.PAVEMENT, 1.0f)                      // Best connects to pavement
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT, "left", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),            // Best connects to wall middle
            (TileTypes.WALL_WINDOW_BOTTOM, 0.8f),           // Can connect to window bottom
            (TileTypes.WALL_FRONT_BOTTOM_END, 0.7f)         // Can connect to wall bottom end
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT, "up", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),            // Best connects to wall middle
            (TileTypes.WALL_FRONT_RIGHT_END, 0.9f),         // Can connect to wall right end
            (TileTypes.WALL_FRONT_CORNER_TOP_RIGHT, 0.8f)   // Can connect to top-right corner
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT, "down", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                     // Best connects to pavement
            (TileTypes.GRASS, 0.7f),                        // Can connect to grass
            (TileTypes.FLOWERS, 0.6f)                       // Can connect to flowers
        });
        
        // ----- WALL_FRONT_TOP_END (Wall top end) -----
        AddRule(settings, TileTypes.WALL_FRONT_TOP_END, "left", new[]
        {
            (TileTypes.WALL_FRONT_TOP_END, 0.9f),           // Can connect to another top end
            (TileTypes.WALL_FRONT_CORNER_TOP_RIGHT, 0.8f)   // Can connect to top-right corner
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_TOP_END, "right", new[]
        {
            (TileTypes.WALL_FRONT_TOP_END, 0.9f),           // Can connect to another top end
            (TileTypes.WALL_FRONT_CORNER_TOP_LEFT, 0.8f)    // Can connect to top-left corner
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_TOP_END, "up", new[]
        {
            (TileTypes.GRASS, 0.9f),                        // Can connect to grass
            (TileTypes.FLOWERS, 0.8f),                      // Can connect to flowers
            (TileTypes.PAVEMENT, 1.0f)                      // Best connects to pavement
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_TOP_END, "down", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),            // Best connects to wall middle
            (TileTypes.WALL_WINDOW_TOP, 0.9f)               // Can connect to window top
        });
        
        // ----- WALL_FRONT_BOTTOM_END (Wall bottom end) -----
        AddRule(settings, TileTypes.WALL_FRONT_BOTTOM_END, "left", new[]
        {
            (TileTypes.WALL_FRONT_BOTTOM_END, 0.9f),        // Can connect to another bottom end
            (TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT, 0.8f) // Can connect to bottom-right corner
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_BOTTOM_END, "right", new[]
        {
            (TileTypes.WALL_FRONT_BOTTOM_END, 0.9f),        // Can connect to another bottom end
            (TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT, 0.8f) // Can connect to bottom-left corner
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_BOTTOM_END, "up", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),           // Best connects to wall middle
            (TileTypes.WALL_WINDOW_BOTTOM, 0.9f)           // Can connect to window bottom
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_BOTTOM_END, "down", new[]
        {
            (TileTypes.PAVEMENT, 1.0f),                    // Best connects to pavement
            (TileTypes.GRASS, 0.8f),                       // Can connect to grass
            (TileTypes.FLOWERS, 0.7f)                      // Can connect to flowers
        });
        
        // ----- WALL_FRONT_LEFT_END (Wall left end) -----
        AddRule(settings, TileTypes.WALL_FRONT_LEFT_END, "left", new[]
        {
            (TileTypes.GRASS, 0.8f),                      // Can connect to grass
            (TileTypes.FLOWERS, 0.7f),                    // Can connect to flowers
            (TileTypes.PAVEMENT, 1.0f)                    // Best connects to pavement
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_LEFT_END, "right", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),          // Best connects to wall middle
            (TileTypes.WALL_WINDOW_TOP, 0.8f),            // Can connect to window top
            (TileTypes.WALL_WINDOW_BOTTOM, 0.8f)          // Can connect to window bottom
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_LEFT_END, "up", new[]
        {
            (TileTypes.WALL_FRONT_TOP_END, 0.9f),         // Can connect to top end
            (TileTypes.WALL_FRONT_CORNER_TOP_LEFT, 0.8f)  // Can connect to top-left corner
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_LEFT_END, "down", new[]
        {
            (TileTypes.WALL_FRONT_BOTTOM_END, 0.9f),       // Can connect to bottom end
            (TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT, 0.8f) // Can connect to bottom-left corner
        });
        
        // ----- WALL_FRONT_RIGHT_END (Wall right end) -----
        AddRule(settings, TileTypes.WALL_FRONT_RIGHT_END, "right", new[]
        {
            (TileTypes.GRASS, 0.8f),                       // Can connect to grass
            (TileTypes.FLOWERS, 0.7f),                     // Can connect to flowers
            (TileTypes.PAVEMENT, 1.0f)                     // Best connects to pavement
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_RIGHT_END, "left", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),           // Best connects to wall middle
            (TileTypes.WALL_WINDOW_TOP, 0.8f),             // Can connect to window top
            (TileTypes.WALL_WINDOW_BOTTOM, 0.8f)           // Can connect to window bottom
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_RIGHT_END, "up", new[]
        {
            (TileTypes.WALL_FRONT_TOP_END, 0.9f),          // Can connect to top end
            (TileTypes.WALL_FRONT_CORNER_TOP_RIGHT, 0.8f)  // Can connect to top-right corner
        });
        
        AddRule(settings, TileTypes.WALL_FRONT_RIGHT_END, "down", new[]
        {
            (TileTypes.WALL_FRONT_BOTTOM_END, 0.9f),        // Can connect to bottom end
            (TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT, 0.8f) // Can connect to bottom-right corner
        });
        
        // ================ WINDOW RULES ================
        
        // ----- WALL_WINDOW_TOP (Window top part) -----
        AddRule(settings, TileTypes.WALL_WINDOW_TOP, "left", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),           // Best connects to wall middle
            (TileTypes.WALL_FRONT_CORNER_TOP_RIGHT, 0.8f)  // Can connect to top-right corner
        });
        
        AddRule(settings, TileTypes.WALL_WINDOW_TOP, "right", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),           // Best connects to wall middle
            (TileTypes.WALL_FRONT_CORNER_TOP_LEFT, 0.8f)   // Can connect to top-left corner
        });
        
        AddRule(settings, TileTypes.WALL_WINDOW_TOP, "up", new[]
        {
            (TileTypes.WALL_FRONT_TOP_END, 0.9f),          // Can connect to top end
            (TileTypes.WALL_FRONT_MIDDLE, 0.8f)            // Can connect to wall middle
        });
        
        AddRule(settings, TileTypes.WALL_WINDOW_TOP, "down", new[]
        {
            (TileTypes.WALL_WINDOW_BOTTOM, 1.0f)           // Must connect to window bottom part
        });
        
        // ----- WALL_WINDOW_BOTTOM (Window bottom part) -----
        AddRule(settings, TileTypes.WALL_WINDOW_BOTTOM, "left", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),           // Best connects to wall middle
            (TileTypes.WALL_FRONT_CORNER_BOTTOM_RIGHT, 0.8f) // Can connect to bottom-right corner
        });
        
        AddRule(settings, TileTypes.WALL_WINDOW_BOTTOM, "right", new[]
        {
            (TileTypes.WALL_FRONT_MIDDLE, 1.0f),           // Best connects to wall middle
            (TileTypes.WALL_FRONT_CORNER_BOTTOM_LEFT, 0.8f) // Can connect to bottom-left corner
        });
        
        AddRule(settings, TileTypes.WALL_WINDOW_BOTTOM, "up", new[]
        {
            (TileTypes.WALL_WINDOW_TOP, 1.0f)              // Must connect to window top part
        });
        
        AddRule(settings, TileTypes.WALL_WINDOW_BOTTOM, "down", new[]
        {
            (TileTypes.WALL_FRONT_BOTTOM_END, 0.9f),       // Can connect to bottom end
            (TileTypes.WALL_FRONT_MIDDLE, 0.8f)            // Can connect to wall middle
        });
    }

    // Add rule to settings
    private static void AddRule(WFCSettings settings, int fromTileId, string direction,
        (int state, float weight)[] possibleStates)
    {
        var key = (fromTileId, direction);
        settings.Rules[key] = new List<(int state, float weight)>(possibleStates);
    }
}