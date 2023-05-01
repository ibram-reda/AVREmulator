using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace AVREmulator.UI.Converters;

public class BitColorConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{

		if (value is bool val)
		{
			if (!val)
				return Brushes.Gray;
			return Brushes.Green;
		} 
		return null;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
