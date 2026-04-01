using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace OBD2Suite.Converters
{
    /// <summary>Converts bool to green/red SolidColorBrush.</summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool bv && bv;
            return b ? new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))
                     : new SolidColorBrush(Color.FromRgb(0xFF, 0x52, 0x52));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Converts bool to Visibility (true = Visible, false = Collapsed).</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }

    /// <summary>Inverse bool to Visibility (false = Visible).</summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Scales a numeric value to a width: (value / max) * MaxWidth.</summary>
    public class ValueToWidthConverter : IValueConverter
    {
        public double MaxWidth { get; set; } = 200;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double d) double.TryParse(value?.ToString(), out d);
            if (parameter is string s && double.TryParse(s, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double maxVal) && maxVal > 0)
                return Math.Min(MaxWidth, d / maxVal * MaxWidth);
            return 0.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Converts a severity string to an accent color brush.</summary>
    public class SeverityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Critical" => new SolidColorBrush(Color.FromRgb(0xFF, 0x52, 0x52)),
                "High"     => new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x35)),
                "Medium"   => new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)),
                "Low"      => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                _          => new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Null → Collapsed, non-null → Visible.</summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value != null ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
