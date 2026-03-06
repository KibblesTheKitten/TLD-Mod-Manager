using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace TLD_Mod_Manager.Converters;

public class StringToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // We'll implement proper async loading later. For now, return null (placeholder)
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}