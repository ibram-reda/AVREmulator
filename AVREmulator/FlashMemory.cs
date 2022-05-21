namespace AVREmulator;

public class FlashMemory
{
    public const int MEMORY_MAX_SIZE = 0xffff;
    private readonly ProgramBus _programBus;
    public UInt16[] Memory = new UInt16[MEMORY_MAX_SIZE];

    /// <summary>
    /// Read value from flash memory
    /// </summary>
    /// <param name="Address">the address of the flash memory</param>
    /// <returns>the value in flash memory</returns>
    public UInt16 Read(int Address)
    {
        return Memory[Address];
    }

    /// <summary>
    /// Write value in flash memory
    /// </summary>
    /// <param name="Address">address in the flash memory</param>
    /// <param name="DataValue">value to put in flash memory</param>
    public void Write(int Address, UInt16 DataValue)
    {
        Memory[Address] = DataValue;
    }

    public void Load(int startAddress, UInt16[] Data)
    {
        int address = startAddress;
        foreach(var data in Data)
        {
            Write(address++, data);
        }
    }
    /// <summary>
    /// flash should not reset ever
    /// </summary>
    public void Reset()
    {
        // do not delete the mrmory of the flash
    }

    public FlashMemory(ProgramBus programBus)
    {
        _programBus = programBus;
    }
}
