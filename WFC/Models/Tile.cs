using System.Windows.Media;
using System.Windows.Media.Imaging;

public class Tile
{
    public int Id { get; }
    public string Name { get; }
    public ImageSource Image { get; }
    public string Path { get; }

    public Tile(int id, string name, string imagePath)
    {
        Id = id;
        Name = name;
        Path = imagePath;
        try
        {
            // Убираем "resources/" из пути и используем правильный формат URI для WPF
            Image = new BitmapImage(new Uri($"pack://application:,,,/Resources/{imagePath}", UriKind.Absolute));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading image: {imagePath}. Error: {ex.Message}");
            throw;
        }
    }

}