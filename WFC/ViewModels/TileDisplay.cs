using System.ComponentModel;
using System.Windows.Media;

namespace WFC.ViewModels;

public class TileDisplay : INotifyPropertyChanged
{
    public ImageSource Image { get; set; }
    public float X { get; set; }
    public float Y { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}