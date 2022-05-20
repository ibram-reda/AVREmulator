using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVREmulator;

public class AVRController
{
    public readonly CPU CPU = new CPU();
    public readonly Ram Ram = new Ram();
    public readonly FlashMemory FlashMemory = new FlashMemory();
    /// <summary>
    /// Hardware Fuses used to controll the hardware spcification 
    /// <br/> <br/>
    /// for more info see page 294 in mazidi book
    /// </summary>
    public UInt16 HardwareFuses = 0x99E1;

    public AVRController(string programHexPath)
    {
        HexFileManger.Load(programHexPath, FlashMemory);
    }

    public void Start()
    {
        throw new NotImplementedException();
    }

    public void Stop()
    {
        throw new NotImplementedException();
    }
}
