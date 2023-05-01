using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace AVREmulator.UI.Converters;

public class BitConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is UInt16 rValue)
		{
			return GetBit((short)rValue, int.Parse(parameter?.ToString()!));
		}
		throw new NotImplementedException();
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	private bool GetBit(short value, int bitNumber)
	{
		return (value & (1 << bitNumber)) != 0;
	}
}
