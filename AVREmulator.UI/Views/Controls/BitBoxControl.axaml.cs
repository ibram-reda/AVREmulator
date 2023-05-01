using Avalonia;
using Avalonia.Controls.Primitives;

namespace AVREmulator.UI.Views;

public class BitBoxControl : TemplatedControl
{
	public static readonly StyledProperty<bool> BitValueProperty =
		AvaloniaProperty.Register<RegisterControl, bool>(nameof(BitValue), false);

	public bool BitValue
	{
		get => (bool)GetValue(BitValueProperty);
		set => SetValue(BitValueProperty, value);
	}
}
