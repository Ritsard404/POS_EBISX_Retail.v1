using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace EBISX_POS.Converters
{
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type _targetType, object _parameter, CultureInfo _culture)
            => (value as bool? ?? false) ? Brushes.Green : Brushes.Red;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

