using System.Collections.ObjectModel;

namespace AVREmulator;

public record PartialObservableCollection<T>(ObservableCollection<T> Parrent, int Start, int Count)
{
	int End = Start + Count;

	public T this[int Address]
	{
		get
		{
			var newAdd = Address + Start;
			if (newAdd < Start || newAdd > End) 
				throw new ArgumentOutOfRangeException("address");
			return Parrent[newAdd];
		}
		set
		{
			var newAdd = Address + Start;
			if (newAdd < Start || newAdd > End)
				throw new ArgumentOutOfRangeException("address"); 
			Parrent[newAdd] = value;
		}
	}
}

