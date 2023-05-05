using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using AVREmulator.UI.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AVREmulator.UI.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
	public MainWindow()
	{
		InitializeComponent();

		this.WhenActivated(d => d(ViewModel!.OpenFileDialogInteraction.RegisterHandler(DoShowDialogAsync!)));
		this.WhenActivated(d => d(ViewModel!.CloseAppInteraction.RegisterHandler(Exit!)));
		
	}


	private async Task DoShowDialogAsync(InteractionContext<string, string?> interaction)
	{
		var options = new FilePickerOpenOptions()
		{
			AllowMultiple = false,
			Title = "Choose your Hex File",
			FileTypeFilter = new List<FilePickerFileType>() {
				new ("Hex File"){
					Patterns = new List<string>(){"*.hex"}
				},
				FilePickerFileTypes.All
			},
		};
		var result = await StorageProvider
			.OpenFilePickerAsync(options);
		interaction.SetOutput(result.FirstOrDefault()?.Path.LocalPath);
	}
	

	public Task Exit(InteractionContext<string, string?> interaction)
	{
		Close();
		return Task.CompletedTask;
	}
}