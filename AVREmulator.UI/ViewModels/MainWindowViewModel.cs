using System.IO;
using System.Threading.Tasks;

namespace AVREmulator.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
	public string Path => "D:\\github\\AVREmulator\\AVREmulatorTests\\AVRTestProgram\\atmelTest.hex";


	public async Task GetProgram()
	{
		
	}
}