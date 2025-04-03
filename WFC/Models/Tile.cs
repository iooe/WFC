using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FlowDirection = System.Windows.FlowDirection;
using Point = System.Windows.Point;

public class Tile
{
    private static Random random = new Random();
    
    public int Id { get; }
    public string Name { get; }
    public ImageSource Image { get; private set; }
    public string FolderPath { get; }
    
    // Keep track of all images in the folder
    private List<string> availableImages;
    
    // Constructor for specifying a folder path
    public Tile(int id, string name, string folderPath)
    {
        Id = id;
        Name = name;
        FolderPath = folderPath;
        
        // Find all available images in the folder
        FindAvailableImages();
        
        // Load an initial random image
        LoadRandomImage();
    }
    
    // Find all available images in the folder
    private void FindAvailableImages()
    {
        availableImages = new List<string>();
        
        try
        {
            // Check if we're running in development or deployed environment
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string resourcesPath = Path.Combine(baseDir, "Resources", FolderPath);
            
            // Check if directory exists
            if (!Directory.Exists(resourcesPath))
            {
                Console.WriteLine($"Directory not found: {resourcesPath}");
                return;
            }
            
            // Get all image files with appropriate extensions
            var imageExtensions = new[] { ".png", ".jpg", ".jpeg" };
            
            foreach (var file in Directory.GetFiles(resourcesPath))
            {
                string extension = Path.GetExtension(file).ToLowerInvariant();
                if (imageExtensions.Contains(extension))
                {
                    availableImages.Add(file);
                }
            }
            
            // If no images found, create default
            if (availableImages.Count == 0)
            {
                string defaultPath = Path.Combine(resourcesPath, "default.png");
                if (File.Exists(defaultPath))
                {
                    availableImages.Add(defaultPath);
                }
            }
            
            Console.WriteLine($"Found {availableImages.Count} images in {resourcesPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding images in folder {FolderPath}: {ex.Message}");
        }
    }
    
    // Load a random image
    public void LoadRandomImage()
    {
        // If we have no images or need to refresh the list
        if (availableImages == null || availableImages.Count == 0)
        {
            FindAvailableImages();
        }
        
        try
        {
            // Choose a random image from available ones
            if (availableImages.Count > 0)
            {
                string randomImageFile = availableImages[random.Next(availableImages.Count)];
                
                // Load the image
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                
                // Use a FileStream approach to handle spaces correctly
                using (var stream = new FileStream(randomImageFile, FileMode.Open, FileAccess.Read))
                {
                    // Create a memory stream to copy the file stream
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
    
    // Create a fallback image in memory
    private void CreateFallbackImageInMemory()
    {
        try
        {
            // Create a DrawingVisual
            var drawingVisual = new DrawingVisual();
            
            // Get a drawing context from the DrawingVisual
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                // Set color based on folder name
                Color color;
                if (FolderPath.Contains("grass", StringComparison.OrdinalIgnoreCase))
                {
                    color = Colors.LightGreen;
                }
                else if (FolderPath.Contains("flower", StringComparison.OrdinalIgnoreCase))
                {
                    color = Colors.Pink;
                }
                else if (FolderPath.Contains("pavement", StringComparison.OrdinalIgnoreCase))
                {
                    color = Colors.Gray;
                }
                else
                {
                    color = Colors.LightBlue;
                }
                
                // Draw a colored rectangle as the background
                Rect rect = new Rect(0, 0, 100, 100);
                drawingContext.DrawRectangle(new SolidColorBrush(color), null, rect);
                
                // Add folder name as text
                FormattedText formattedText = new FormattedText(
                    Path.GetFileName(FolderPath),
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    12,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);
                
                drawingContext.DrawText(formattedText, 
                    new Point((100 - formattedText.Width) / 2, (100 - formattedText.Height) / 2));
            }
            
            // Create a RenderTargetBitmap from the DrawingVisual
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(
                100, 100, 96, 96, PixelFormats.Pbgra32);
            
            renderTargetBitmap.Render(drawingVisual);
            
            // Set the RenderTargetBitmap as the Image
            Image = renderTargetBitmap;
            
            Console.WriteLine("Created fallback image in memory");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating fallback image: {ex.Message}");
            // Absolute last resort - create a 1x1 pixel image
            Image = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
        }
    }
}

// Extension method to clone a stream
public static class StreamExtensions
{
    public static MemoryStream CloneStream(this Stream stream)
    {
        MemoryStream ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }
}