using LifeOS.Models;
using System.Globalization;
using System.Windows.Data;
using MediaColor = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace LifeOS.Views;

public class PriorityColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Priority p ? p switch
        {
            Priority.High => new MediaSolidColorBrush(MediaColor.FromRgb(220, 50, 50)),
            Priority.Medium => new MediaSolidColorBrush(MediaColor.FromRgb(230, 140, 30)),
            Priority.Low => new MediaSolidColorBrush(MediaColor.FromRgb(80, 160, 80)),
            _ => MediaBrushes.Gray
        } : MediaBrushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
