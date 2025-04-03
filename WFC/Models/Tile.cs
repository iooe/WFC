using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;

public class Tile
{
    private static Random random = new Random();
    
    public int Id { get; }
    public string Name { get; }
    public ImageSource Image { get; private set; }
    public string FolderPath { get; }
    public string CurrentImagePath { get; private set; }
    
    // Constructor for specifying a folder path
    public Tile(int id, string name, string folderPath)
    {
        Id = id;
        Name = name;
        FolderPath = folderPath;
        
        // Load a random image from the folder
        LoadRandomImage();
    }
    
    // Constructor for a specific image file
    public Tile(int id, string name, string imagePath, bool isSpecificFile)
    {
        Id = id;
        Name = name;
        
        if (isSpecificFile)
        {
            FolderPath = Path.GetDirectoryName(imagePath) ?? "";
            CurrentImagePath = imagePath;
            LoadSpecificImage(imagePath);
        }
        else
        {
            FolderPath = imagePath;
            LoadRandomImage();
        }
    }
    
    // Load a random image from the folder
    public void LoadRandomImage()
    {
        try
        {
            // Check if we're running in development or deployed environment
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string resourcesPath = Path.Combine(baseDir, "Resources", FolderPath);
            
            // Debug: Log the path we're looking in
            Console.WriteLine($"Looking for images in: {resourcesPath}");
            
            // Check if directory exists
            if (!Directory.Exists(resourcesPath))
            {
                Console.WriteLine($"Directory not found: {resourcesPath}");
                CreateFallbackImageInMemory();
                return;
            }
            
            // Debug: List all files in the directory
            Console.WriteLine($"Files in directory {resourcesPath}:");
            foreach (var file in Directory.GetFiles(resourcesPath))
            {
                Console.WriteLine($"  - {Path.GetFileName(file)}");
            }
            
            // Get all image files in the directory with proper extension handling
            var imageFiles = Directory.GetFiles(resourcesPath)
                .Where(f => {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
                })
                .ToArray();
                
            if (imageFiles.Length == 0)
            {
                Console.WriteLine($"No image files found in: {resourcesPath}");
                CreateFallbackImageInMemory();
                return;
            }
            
            // Select a random image file
            string randomImageFile = imageFiles[random.Next(imageFiles.Length)];
            Console.WriteLine($"Selected image: {randomImageFile}");
            
            // IMPORTANT: Use a more direct approach to load the image that handles spaces correctly
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            
            // Use this approach to handle spaces in file paths
            using (var stream = new FileStream(randomImageFile, FileMode.Open, FileAccess.Read))
            {
                bitmap.StreamSource = stream.CloneStream();
            }
            
            bitmap.EndInit();
            bitmap.Freeze(); // Important for thread safety
            
            Image = bitmap;
            CurrentImagePath = randomImageFile;
            Console.WriteLine($"Successfully loaded image: {randomImageFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading random image from folder {FolderPath}: {ex.Message}");
            CreateFallbackImageInMemory();
        }
    }
    
    // Load a specific image
    private void LoadSpecificImage(string imagePath)
    {
        try
        {
            // First, try to find the exact file in the Resources folder
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(baseDir, "Resources", FolderPath, imagePath);
            
            // Debug: Log the file we're looking for
            Console.WriteLine($"Looking for specific image at: {fullPath}");
            
            if (!File.Exists(fullPath))
            {
                // Try to find the file by name without worrying about extension
                string directory = Path.Combine(baseDir, "Resources", FolderPath);
                string nameWithoutExt = Path.GetFileNameWithoutExtension(imagePath);
                
                if (Directory.Exists(directory))
                {
                    // List all files and find a match by name
                    var matchingFiles = Directory.GetFiles(directory)
                        .Where(f => Path.GetFileNameWithoutExtension(f)
                            .Equals(nameWithoutExt, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                        
                    if (matchingFiles.Length > 0)
                    {
                        fullPath = matchingFiles[0];
                        Console.WriteLine($"Found matching file: {fullPath}");
                    }
                    else
                    {
                        Console.WriteLine($"No matching file found for: {nameWithoutExt}");
                        LoadRandomImage();
                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"Directory not found: {directory}");
                    LoadRandomImage();
                    return;
                }
            }
            
            // IMPORTANT: Use a more direct approach to load the image that handles spaces correctly
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            
            // Use this approach to handle spaces in file paths
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            {
                bitmap.StreamSource = stream.CloneStream();
            }
            
            bitmap.EndInit();
            bitmap.Freeze(); // Important for thread safety
            
            Image = bitmap;
            CurrentImagePath = fullPath;
            Console.WriteLine($"Successfully loaded specific image: {fullPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading specific image {imagePath}: {ex.Message}");
            LoadRandomImage();
        }
    }
    
    // Create a simple fallback image in memory
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
                    FolderPath,
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
            CurrentImagePath = "fallback-image";
            
            Console.WriteLine("Created fallback image in memory");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating fallback image: {ex.Message}");
            // Absolute last resort - create a 1x1 pixel image
            Image = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
            CurrentImagePath = "1x1-pixel-fallback";
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