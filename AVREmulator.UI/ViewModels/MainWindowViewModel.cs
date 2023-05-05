using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AVREmulator.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
	public MainWindowViewModel()
	{
		OpenFileDialogInteraction = new();
		CloseAppInteraction = new();
		OpenFileDialogCommand = ReactiveCommand.CreateFromTask(async () =>
		  {
			  var result = await OpenFileDialogInteraction.Handle(HexFilePath);
			  HexFilePath = result;
			  if(result != null) 
				Controller = new AVRController(result);
		  });

		CloseAppCommand = ReactiveCommand.Create(async () => await CloseAppInteraction.Handle(null));
	}
	string _hexFilePath = "";
	AVRController? _Controller;


	
	public AVRController? Controller
	{
		get { return _Controller; }
		set 
		{ 
			this.RaiseAndSetIfChanged(ref _Controller, value);
		}
	}
	public string HexFilePath
	{
		get { return _hexFilePath; }
		set { this.RaiseAndSetIfChanged(ref _hexFilePath, value); }
	}

	public Interaction<string?, string?> OpenFileDialogInteraction { get; }
	public Interaction<string?, string?> CloseAppInteraction { get; }
	public ICommand OpenFileDialogCommand { get; } 

	public ICommand CloseAppCommand { get; }

}