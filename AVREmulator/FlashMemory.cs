using System.Collections.ObjectModel;

namespace AVREmulator;

public class FlashMemory : Memory<UInt16>
{  
    public FlashMemory():base(0xffff)
    {
    }


    public void Load(int startAddress, UInt16[] Data)
    {
        int address = startAddress;
        foreach(var data in Data)
        {
            Write(address++, data);
        }
    }
    
}
