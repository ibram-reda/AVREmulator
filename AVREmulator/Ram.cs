using System;

namespace AVREmulator;

public class Ram 
{
    public readonly int RAM_MAX_SIZE;
    public byte[] RAM ;

    public Ram() : this(0xffff) 
    {
    }
    public Ram(int SIZE)
    {
        RAM_MAX_SIZE = SIZE;
        RAM = new byte[RAM_MAX_SIZE];
    }

    /// <summary>
    /// Read value from rame
    /// </summary>
    /// <param name="Address">the address to read from</param>
    /// <returns>the value in ram in the passed address</returns>
    public byte Read(int Address)
    {
        return RAM[Address];
    }

    /// <summary>
    /// Write value in ram
    /// </summary>
    /// <param name="Address">address in the rame</param>
    /// <param name="DataValue">value to put in ram</param>
    public void Write(int Address,byte DataValue)
    {
        RAM[Address] = DataValue;
    }

    public ArraySegment<byte> GetPortion(int startAddress,int Count)
    {
        return new ArraySegment<byte>(RAM, startAddress, Count);

	}

    public byte this[int address]
    {
        get => Read(address);
        set => Write(address, value);
    }

    /// <summary>
    /// clear Ram on power up or reset
    /// </summary>
    public void Reset()
    {
        for(int i = 0; i < RAM.Length; i++)
        {
            RAM[i] = 0;
        }
    }
}
