using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FlowDirection = System.Windows.FlowDirection;
using Point = System.Windows.Point;

namespace WFC.Models;

/// <summary>
/// Represents a tile in the WFC grid
/// </summary>
public class Tile
{
    private static Random random = new Random();
    
    /// <summary>
    /// Internal numeric ID (index in the tile array)
    /// </summary>
    public int Id { get; }
    
    /// <summary>
    /// Unique string identifier
    /// </summary>
    public string TileId { get; }
    
    /// <summary>
    /// Display name of the tile
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Image source for the tile
    /// </summary>
    public ImageSource Image { get; private set; }
    
    /// <summary>
    /// Path to the folder containing tile images
    /// </summary>
    public string FolderPath { get; }
    
    /// <summary>
    /// Category of the tile
    /// </summary>
    public string Category { get; }
    
    /// <summary>
    /// Properties of the tile
    /// </summary>
    public Dictionary<string, string> Properties { get; }
    
    // Track all images in the folder
    private List<string> _availableImages;
    
    /// <summary>
    /// Create a new tile
    /// </summary>
    public Tile(int id, string tileId, string name, string folderPath, string category = null, Dictionary<string, string> properties = null)
    {
        Id = id;
        TileId = tileId;
        Name = name;
        FolderPath = folderPath;
        Category = category ?? "Default";
        Properties = properties ?? new Dictionary<string, string>();
        
        // Find all available images
        FindAvailableImages();
        
        // Load an initial random image
        LoadRandomImage();
    }
    
    /// <summary>
    /// Find all available images in the folder
    /// </summary>
    private void FindAvailableImages()
    {
        _availableImages = new List<string>();
        
        try
        {
            // Check if we're running in development or deployed environment
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string resourcesPath = Path.Combine(baseDir, "Resources", FolderPath);
            
            // Check if directory exists
            if (!Directory.Exists(resourcesPath))
            {
                Console.WriteLine($"Directory not found: {resourcesPath}");
                CreateFallbackImageInMemory();
                return;
            }
            
            // Get all image files
            var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp" };
            
            foreach (var file in Directory.GetFiles(resourcesPath))
            {
                string extension = Path.GetExtension(file).ToLowerInvariant();
                if (imageExtensions.Contains(extension))
                {
                    _availableImages.Add(file);
                }
            }
            
            // If no images found, check for default image
            if (_availableImages.Count == 0)
            {
                string defaultPath = Path.Combine(resourcesPath, "default.png");
                if (File.Exists(defaultPath))
                {
                    _availableImages.Add(defaultPath);
                }
                else
                {
                    CreateFallbackImageInMemory();
                }
            }
            
            Console.WriteLine($"Found {_availableImages.Count} images in {resourcesPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding images in folder {FolderPath}: {ex.Message}");
            CreateFallbackImageInMemory();
        }
    }
    
    /// <summary>
    /// Load a random image from available images
    /// </summary>
    public void LoadRandomImage()
    {
        // If no available images, find them or create fallback
        if (_availableImages == null || _availableImages.Count == 0)
        {
            FindAvailableImages();
        }
        
        try
        {
            // Choose a random image
            if (_availableImages != null && _availableImages.Count > 0)
            {
                string randomImageFile = _availableImages[random.Next(_availableImages.Count)];
                
                // Load the image
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                
                // Use file stream to handle paths with spaces
                using (var stream = new FileStream(randomImageFile, FileMode.Open, FileAccess.Read))
                {
                    // Create memory stream to copy the file stream
                    MemoryStream memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    memoryStream.Position = 0;
                    
                    bitmap.StreamSource = memoryStream;
                }
                
                bitmap.EndInit();
                bitmap.Freeze(); // Ensure thread safety
                
                Image = bitmap;
                Console.WriteLine($"Loaded random image: {randomImageFile}");
            }
            else
            {
                CreateFallbackImageInMemory();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading random image: {ex.Message}");
            CreateFallbackImageInMemory();
        }
    }
    
    /// <summary>
    /// Create a fallback image in memory
    /// </summary>
    private void CreateFallbackImageInMemory()
    {
        try
        {
            // Create a DrawingVisual
            var drawingVisual = new DrawingVisual();
            
            // Get a drawing context
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                // Set color based on category
                Color color;
                switch (Category.ToLowerInvariant())
                {
                    case "grass":
                        color = Colors.LightGreen;
                        break;
                    case "flowers":
                        color = Colors.Pink;
                        break;
                    case "pavement":
                        color = Colors.Gray;
                        break;
                    case "building":
                        color = Colors.SandyBrown;
                        break;
                    case "water":
                        color = Colors.LightBlue;
                        break;
                    default:
                        color = Colors.White;
                        break;
                }
                
                // Draw background rectangle
                Rect rect = new Rect(0, 0, 100, 100);
                drawingContext.DrawRectangle(new SolidColorBrush(color), null, rect);
                
                // Add tile name as text
                FormattedText formattedText = new FormattedText(
                    Name,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    12,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);
                
                drawingContext.DrawText(formattedText, 
                    new Point((100 - formattedText.Width) / 2, (100 - formattedText.Height) / 2));
            }
            
            // Create a RenderTargetBitmap
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(
                100, 100, 96, 96, PixelFormats.Pbgra32);
            
            renderTargetBitmap.Render(drawingVisual);
            
            // Set as image
            Image = renderTargetBitmap;
            
            Console.WriteLine("Created fallback image in memory");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating fallback image: {ex.Message}");
            // Last resort: create a 1x1 pixel image
            Image = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
        }
    }
}