using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TrackMvvm.Utilities
{
    /// <summary>
    /// Converter that returns different colors based on task state:
    /// - Red: Task is active locally
    /// - Orange: Task is being tracked by another device
    /// - Black: Task is inactive
    /// </summary>
    public class TaskStateToBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                return Brushes.Black;

            bool isActive = values[0] is bool active && active;
            bool isRemoteTracking = values[1] is bool remote && remote;

            if (isActive)
                return Brushes.Red;

            if (isRemoteTracking)
                return Brushes.DarkMagenta;

            return Brushes.Black;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new[] { DependencyProperty.UnsetValue, DependencyProperty.UnsetValue };
        }
    }
}
