using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TicTacToeFancy.Converters;

public class BoolToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var parameterString = parameter?.ToString();
        if (parameterString is null)
        {
            return string.Empty;
        }

        var parts = parameterString.Split('|');
        if (parts.Length != 2)
        {
            return string.Empty;
        }

        var boolValue = value is bool state && state;
        return boolValue ? parts[0] : parts[1];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
