using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace EBISX_POS.Converters
{
    public class BoolToOnlineOfflineTextConverter : IValueConverter
    {
        // value is the bound boolean
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b && targetType == typeof(string))
            {
                return b ? "Online" : "Offline";
            }
            // fallback
            return Avalonia.Data.BindingNotification.UnsetValue;
        }

        // only needed for two‑way binding; you can throw if you never bind back
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s && targetType == typeof(bool))
            {
                return s.Equals("Online", StringComparison.OrdinalIgnoreCase);
            }
            return Avalonia.Data.BindingNotification.UnsetValue;
        }
    }
}
