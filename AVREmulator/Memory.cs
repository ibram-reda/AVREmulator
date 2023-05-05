using System.Collections.ObjectModel;

namespace AVREmulator;

public abstract class Memory<T> where T : struct
{
	public readonly int MemorySize;
	protected ObservableCollection<T> _memory;

	public ObservableCollection<T> ActualMemory { get { return _memory; } }

	public Memory(int memorySize)
	{
		_memory = new ObservableCollection<T>();
		for (int i = 0; i < memorySize; i++)
		{
			_memory.Add(default(T));
		}
		this.MemorySize = memorySize;
	}

	/// <summary>
	/// Read value from Memory
	/// </summary>
	/// <param name="Address">the address to read from</param>
	/// <returns>the value in memory in that address</returns>
	public T Read(int Address)
	{
		if(Address < 0 || Address > MemorySize)
			throw new ArgumentOutOfRangeException(nameof(Address));
		return _memory[Address];
	}

	/// <summary>
	/// Write value in memory
	/// </summary>
	/// <param name="Address">address location where data will write to it</param>
	/// <param name="DataValue">value to put in memory</param>
	public void Write(int Address, T DataValue)
	{
		if (Address < 0 || Address > MemorySize)
			throw new ArgumentOutOfRangeException(nameof(Address));
		_memory[Address] = DataValue;
	}

	public T this[int address]
	{
		get => Read(address);
		set => Write(address, value);
	}
}

