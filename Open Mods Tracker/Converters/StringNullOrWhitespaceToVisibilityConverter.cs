using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace OpenModsTracker.Converters;

public sealed class StringNullOrWhitespaceToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isEmpty = value is not string s || string.IsNullOrWhiteSpace(s);
        var invert = parameter?.ToString()?.Equals("invert", StringComparison.OrdinalIgnoreCase) == true;
        if (invert) isEmpty = !isEmpty;

        return isEmpty ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}

