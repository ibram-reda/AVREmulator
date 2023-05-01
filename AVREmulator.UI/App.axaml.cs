using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AVREmulator.UI.ViewModels;
using AVREmulator.UI.Views;

namespace AVREmulator.UI
{
	public partial class App : Application
	{
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				desktop.MainWindow = new MainWindow
				{
					DataContext = new MainWindowViewModel(),
				};
			}

			base.OnFrameworkInitializationCompleted();
		}
	}
}