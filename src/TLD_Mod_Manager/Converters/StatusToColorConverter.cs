using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace TLD_Mod_Manager.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            "WORKING" => Brush.Parse("#4caf50"),   // Green
            "NOT WORKING" => Brush.Parse("#f44336"), // Red
            "NOT UPDATED" => Brush.Parse("#ff9800"), // Orange
            "BETA" => Brush.Parse("#2196f3"),       // Blue
            _ => Brush.Parse("#9e9e9e")              // Grey default
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}