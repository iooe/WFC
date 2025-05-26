# A Hybrid Framework for Procedural Content Generation

A sophisticated WPF application implementing the Wave Function Collapse (WFC) algorithm enhanced with neural network quality assessment and a flexible plugin system for procedural map generation.

## Overview

This framework combines the Wave Function Collapse algorithm with machine learning to generate high-quality procedural maps. It features a plugin-based architecture that allows easy extension of tile sets and generation rules, making it suitable for game development, procedural content research, and creative applications.

### Key Features

- **Wave Function Collapse Algorithm** - Core implementation with constraint propagation
- **Neural Network Quality Assessment** - Accord.NET-based model for evaluating generated maps
- **Plugin System** - Extensible architecture for tiles, rules, and post-processing
- **Batch Generation** - Generate multiple maps with configurable parameters
- **Interactive UI** - Real-time visualization with zoom and pan controls
- **Export Options** - Save as PNG images or individual tile files
- **Training System** - Collect user ratings to improve quality assessment

## Requirements

- Windows 10/11
- .NET 8.0 Runtime
- Visual Studio 2022 or later (for development)
- Windows Forms support (included in project)

## Getting Started

### Installation

1. Clone or download the repository

2. Restore NuGet packages:
```bash
dotnet restore
```

3. Build the project:
```bash
dotnet build
```

4. Run the application:
```bash
dotnet run --project WFC
```

### Quick Start

1. Launch the application
2. Adjust grid size (default: 15x15)
3. Click "Generate" to create a new map
4. Use mouse wheel to zoom, right-click to pan
5. Export your map using the Export buttons

## Architecture

### Core Components

```
WFC/
├── Models/                 # Data models
│   ├── Tile/              # Tile, Cell, TileDefinition
│   ├── NeuralNetwork/     # ML models and interfaces
│   └── *.cs               # Core models
├── Services/              # Core services
│   ├── WFC/              # Wave Function Collapse implementation
│   ├── ML/               # Neural network training and assessment
│   ├── Export/           # PNG and tile exporters
│   ├── BatchGeneration/  # Batch map generation
│   ├── System/           # Dialog, FileSystem, Visual helpers
│   └── Render/           # UI converters
├── Plugins/              # Plugin implementations
│   ├── Basic/            # GrassPlugin, FlowersPlugin, PavementPlugin
│   ├── Buildings/        # BuildingPlugin with generation logic
│   ├── Terrain/          # TerrainGenerationPlugin
│   ├── ML/               # NeuralGuidancePlugin
│   └── Core/             # Plugin interfaces
├── ViewModels/           # MVVM ViewModels
├── Factories/            # Factory implementations
├── Tests/                # Unit and integration tests
└── Resources/            # Tile images organized by category
```

### Design Patterns

- **MVVM** - Model-View-ViewModel for WPF
- **Dependency Injection** - Microsoft.Extensions.DependencyInjection
- **Factory Pattern** - TileFactory, ModelFactory, ExporterFactory
- **Plugin Architecture** - Dynamic loading and configuration

## Plugin System

### Available Plugins

1. **Basic Tile Sets**
   - `wfc.basic.grass` - Basic grass tiles
   - `wfc.basic.flowers` - Flower tiles
   - `wfc.basic.pavement` - Pavement with transitions

2. **Advanced Plugins**
   - `wfc.buildings` - Building generation with walls, windows, corners
   - `wfc.terrain` - Terrain generation with noise maps
   - `wfc.neural.guidance` - ML-guided generation

### Plugin Types

#### Tile Set Plugins (`ITileSetPlugin`)
```csharp
public class CustomTilePlugin : ITileSetPlugin
{
    public string Id => "custom.tiles";
    public string Name => "Custom Tiles";
    
    public IEnumerable<TileDefinition> GetTileDefinitions()
    {
        // Define your tiles
    }
    
    public IEnumerable<TileRuleDefinition> GetRuleDefinitions()
    {
        // Define connection rules
    }
}
```

#### Generation Hook Plugins (`IGenerationHookPlugin`)
```csharp
public void OnBeforeCollapse(int x, int y, 
    IEnumerable<int> possibleStates, GenerationContext context)
{
    // Influence tile selection during generation
}
```

#### Post-Processor Plugins (`IPostProcessorPlugin`)
```csharp
public Tile[,] ProcessGrid(Tile[,] grid, GenerationContext context)
{
    // Modify the generated grid
    return grid;
}
```

## Neural Network Integration

### Quality Assessment Model

The framework uses Accord.NET neural network (8-16-8-1 architecture) to evaluate maps:

- **Input Features** (8 neurons)
  - Variety score
  - Transition density
  - Tile type ratios (grass, flowers, pavement, building, water)
  - Transition count

- **Quality Dimensions**
  - **Coherence** - Smooth transitions between tiles
  - **Aesthetics** - Visual balance and variety
  - **Playability** - Navigability and obstacle distribution

### Training Workflow

1. Generate maps with different parameters
2. Rate each map (1-5 stars) using the rating buttons
3. Enable "Save for training" to collect examples
4. Click "Train Quality Model" when you have enough ratings
5. Model saves to `Models/accord_quality_model.bin`

### Neural Guidance

When enabled, the `NeuralGuidancePlugin`:
- Evaluates partial maps during generation
- Adjusts tile probabilities based on quality scores
- Performs post-processing to improve low-scoring areas

## Usage Guide

### Generation Tab

**Basic Generation**
- Grid Size: Set width and height (max 256x256)
- Random Seed: Optional, for reproducible results
- Enable Debug Render: Visualize generation process

**Quality Assessment**
- Automatic scoring after generation
- Dimensional breakdown with progress bars
- Feedback suggestions
- Rate maps 1-5 stars for training

**Export Options**
- Export PNG: Save complete map as image
- Export Tiles: Save individual 100x100 tile images

### Plugins Tab

- View all available plugins
- Enable/disable plugins with checkboxes
- Click "Apply Changes" to reload tile configurations
- Plugin types shown (Tile Set, Generation Hook, etc.)

### Batch Generation Tab

**Parameters**
- Number of Maps: 1-100
- Grid Size: Independent from single generation
- Use Fixed Seed: Same seed for all maps
- Export Format: PNG, Tiles, or Both

**Workflow**
1. Set parameters
2. Choose export folder
3. Click "Generate Batch"
4. View results and elapsed time
5. Open export folder directly from UI

## Development

### Project Configuration

The project uses:
- .NET 8.0 Windows
- WPF for UI
- Windows Forms for dialogs
- MSTest for testing
- Moq for mocking

### Adding Custom Tiles

1. Create tile images (100x100 px recommended)
2. Place in `Resources/your-category/`
3. Create a plugin class:

```csharp
public class MyTilePlugin : ITileSetPlugin
{
    public IEnumerable<TileDefinition> GetTileDefinitions()
    {
        yield return new TileDefinition
        {
            Id = "my.tile",
            Name = "My Tile",
            Category = "custom",
            ResourcePath = "my-category",
            Properties = new Dictionary<string, string>
            {
                { "walkable", "true" }
            }
        };
    }
}
```

### Testing

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter TestCategory=Integration
```

Test projects:
- `CoreIntegrationTests` - Full pipeline testing
- `WFCServiceTests` - Algorithm testing
- `PluginIntegrationTests` - Plugin system
- `QualityAssessmentModelTests` - ML components

## Performance Notes

- **Grid Size Impact**: O(n²) for n×n grid
- **Plugin Overhead**: ~5-10ms per active plugin
- **Neural Evaluation**: ~100-200ms per complete map
- **Batch Generation**: Parallel processing available
- **Memory Usage**: ~4MB per 100×100 grid

## Known Issues

- Large grids (>100×100) may cause UI lag during animation
- Neural network requires at least 2 rated examples to train
- Some tile combinations may create impossible constraints

## License

This project is licensed under the MIT License - see the LICENSE file for details.

Copyright (c) 2025 Roman Y

## Acknowledgments

- Wave Function Collapse algorithm concept by Maxim Gumin
- Accord.NET Framework for machine learning capabilities
- Tile artwork included for demonstration purposes

---

**Note**: This is an educational/research project demonstrating the integration of procedural generation algorithms with machine learning for content quality assessment.