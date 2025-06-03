using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EBISX_POS.Converters
{
    public class StatusToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "Active" : "InActive";
            }
            return "InActive";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.Equals("Active", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
} 