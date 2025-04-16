using System.Globalization;
using System.Windows.Data;

namespace WFC.Services.Render;


/// <summary>
/// Converter for calculating viewport indicator position on minimap
/// </summary>
public class MinimapConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!(value is double))
            return 0.0;

        double offset = (double)value;
        double maxOffset = 150; // Minimap size
            
        // Scale scrollviewer offset to minimap size
        return offset / 10;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}