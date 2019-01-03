using System;
using System.Globalization;
using System.Windows.Data;

namespace TrackMvvm.Utilities
{
    public class TotalTimeMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var totalTasksDuration = values[0];
            var totalDuration = values[1];
#if DEBUG
            return ((double) totalTasksDuration).ToString("0.0") + " / " +
                   ((TimeSpan) totalDuration).TotalSeconds.ToString("0.0");
#else
            return ((double) totalTasksDuration / 60 / 60).ToString("0.0") + " / " +
                   ((TimeSpan) totalDuration).TotalHours.ToString("0.0");
#endif
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}