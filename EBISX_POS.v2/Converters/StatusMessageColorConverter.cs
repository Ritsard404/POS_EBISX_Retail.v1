using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace EBISX_POS.Converters
{
    public class StatusMessageColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string message)
            {
                if (message.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
                    return new SolidColorBrush(Colors.Red);
                if (message.StartsWith("Success", StringComparison.OrdinalIgnoreCase))
                    return new SolidColorBrush(Colors.Green);
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 