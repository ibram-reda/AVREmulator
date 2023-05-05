using System;

namespace AVREmulator;

public class Ram : Memory<byte>
{
    public Ram() : base(0xffff) 
    {
    }

    public PartialObservableCollection<byte> GetPortion(int startAddress,int Count)
    {
        return new PartialObservableCollection<byte>(_memory, startAddress, Count);

	}


    /// <summary>
    /// clear Ram on power up or reset
    /// </summary>
    public void Reset()
    {
        for(int i = 0; i < _memory.Count; i++)
        {
            _memory[i] = 0;
        }
    }
}
