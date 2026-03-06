using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PiSearch.App.Converters;

/// <summary>Returns Visibility.Visible when the value is true, else Collapsed.</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>Returns Visibility.Collapsed when the value is true, else Visible.</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}

/// <summary>Formats a double as a percentage string (e.g., "42.73 %").</summary>
[ValueConversion(typeof(double), typeof(string))]
public sealed class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double d ? $"{d * 100.0:F2} %" : "—";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}

/// <summary>Formats a large number with thousand-separator (e.g., 1234567 → "1,234,567").</summary>
[ValueConversion(typeof(long), typeof(string))]
public sealed class LargeNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is long l ? l.ToString("N0", culture) : value?.ToString() ?? "—";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}

/// <summary>Formats digits/second into a human-readable speed (e.g., "12.3 M/s").</summary>
[ValueConversion(typeof(double), typeof(string))]
public sealed class VelocityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double v) return "—";
        if (v >= 1_000_000) return $"{v / 1_000_000.0:F1} M/s";
        if (v >= 1_000)     return $"{v / 1_000.0:F1} K/s";
        return $"{v:F0} /s";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}

/// <summary>Returns a neon-green brush when the value is true, else a dim grey.</summary>
[ValueConversion(typeof(bool), typeof(Brush))]
public sealed class FoundBrushConverter : IValueConverter
{
    private static readonly Brush NeonGreen = new SolidColorBrush(Color.FromRgb(0x39, 0xFF, 0x14));
    private static readonly Brush DimGrey   = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));

    static FoundBrushConverter()
    {
        NeonGreen.Freeze();
        DimGrey.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? NeonGreen : DimGrey;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}
