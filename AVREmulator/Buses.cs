namespace AVREmulator;

public enum ControllSignal
{
    READ,
    WRITE,
}

public class DataBus
{
    public byte Data { get; set; }
    public UInt16 Address { get; set; }
    public ControllSignal Control { get; set; }

    
}

public class ProgramBus
{
    // TO DO: improve this mechanism
    public FlashMemory flashMemory;
    public UInt16 Data { get; set; }
    public UInt16 Address { get; set; }
    public byte Control { get; set; }
}
