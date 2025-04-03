using System.Globalization;
using System.Windows.Data;

namespace WFC.Services
{
    public class GridSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int gridDimension)
            {
                // Convert grid dimensions to canvas dimensions by multiplying by tile size
                return gridDimension * 100; // 100 is our tile size
            }
            return 100; // Default if conversion fails
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}