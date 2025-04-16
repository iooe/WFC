using System.Globalization;
using System.Windows.Data;

namespace WFC.Services.Render
{
    /// <summary>
    /// Converter for calculating viewport indicator dimensions on minimap
    /// </summary>
    public class ViewportConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 4 || !(values[0] is double) || !(values[1] is double) 
                || !(values[2] is double) || !(values[3] is double))
                return 1.0;

            double viewportSize = (double)values[0];
            double extentSize = (double)values[1];
            double contentSize = (double)values[2];
            double zoomLevel = (double)values[3];
            
            // Calculate visible area proportion
            double proportion = viewportSize / (extentSize * zoomLevel);
            
            // Calculate indicator size
            // (never larger than minimap size)
            double result = Math.Min(150 * proportion, 150);
            
            // Minimum indicator size
            return Math.Max(result, 10);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}