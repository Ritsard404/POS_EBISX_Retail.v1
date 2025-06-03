using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EBISX_POS.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string textOptions)
            {
                var options = textOptions.Split('|');
                if (options.Length == 2)
                {
                    return boolValue ? options[0] : options[1];
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 