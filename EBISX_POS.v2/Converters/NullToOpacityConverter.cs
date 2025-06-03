using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace EBISX_POS.Converters
{
    public class NullToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInverse = parameter != null && parameter.ToString()?.ToLower() == "inverse";
            bool isNull = value == null;
            
            // If inverse, return 1.0 when null, 0.0 when not null
            // If not inverse, return 0.0 when null, 1.0 when not null
            return (isInverse ? isNull : !isNull) ? 1.0 : 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
