using System;
using System.Globalization;
using System.Windows.Data;

namespace TrackMvvm.Utilities
{
    public class SecondsToHoursConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is double seconds ? (seconds / 60.0 / 60.0).ToString("0.0") : "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 0d;
        }
    }
}