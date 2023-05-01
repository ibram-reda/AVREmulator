using Avalonia;
using Avalonia.Controls.Primitives;
using Microsoft.CodeAnalysis.Operations;
using System;

namespace AVREmulator.UI.Views;

public class RegisterControl : TemplatedControl
{
	public static readonly StyledProperty<string> RegNameProperty = 
		AvaloniaProperty.Register<RegisterControl,string>(nameof(RegName),"reg Name");

	public string RegName
	{
		get => (string)GetValue(RegNameProperty);
		set => SetValue(RegNameProperty, value);
	}

	public static readonly StyledProperty<UInt16> RegValueProperty =
		AvaloniaProperty.Register<RegisterControl, UInt16>(nameof(RegValue),0);
	public UInt16 RegValue
	{
		get => (UInt16)GetValue(RegValueProperty);
		set => SetValue(RegValueProperty, value);
	}

}
