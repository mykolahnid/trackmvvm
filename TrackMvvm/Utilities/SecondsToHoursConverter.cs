using System;
using System.Globalization;
using System.Windows.Data;

namespace TrackMvvm.Utilities
{
    public class SecondsToHoursConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
#if DEBUG
            return value;
#else
            return value is double seconds ? (seconds / 60.0 / 60.0).ToString("0.0") : "0";
#endif
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 0d;
        }
    }
}