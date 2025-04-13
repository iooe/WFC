using System.ComponentModel;
using System.Windows.Media;
using WFC.Models;

namespace WFC.ViewModels;

public class TileDisplay : INotifyPropertyChanged
{
    private ImageSource _image;
    
    // Reference to the source Tile
    public Tile SourceTile { get; set; }
    
    public ImageSource Image 
    { 
        get => _image;
        set
        {
            _image = value;
            OnPropertyChanged(nameof(Image));
        }
    }
    
    public float X { get; set; }
    public float Y { get; set; }

    // Constructor that takes a Tile and positions
    public TileDisplay(Tile sourceTile, float x, float y)
    {
        SourceTile = sourceTile;
        X = x;
        Y = y;
        
        // Load a fresh random image for this specific tile instance
        RefreshImage();
    }
    
    // Method to refresh the image
    public void RefreshImage()
    {
        if (SourceTile != null)
        {
            // Ask the source tile to load a new random image
            SourceTile.LoadRandomImage();
            
            // Use that image
            Image = SourceTile.Image;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}