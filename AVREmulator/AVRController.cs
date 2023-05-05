namespace AVREmulator;

public class AVRController
{
    private readonly CPU _cpu;
    private readonly Ram _ram;
    private readonly FlashMemory _flashMemory;
    
    public CPU CPU => _cpu;
    public Ram Ram => _ram;
    public FlashMemory FlashMemory => _flashMemory;
	/// <summary>
	/// Hardware Fuses used to control the hardware specification 
	/// <br/> <br/>
	/// for more info see page 294 in mazidi book
	/// </summary>
	public UInt16 HardwareFuses = 0x99E1;

    public AVRController(string programHexPath)
    {
        _ram = new();
        _flashMemory = new();
        _cpu = new(_ram,_flashMemory);

        CodeBurnerEmulator.LoadFromHexFile(programHexPath, _flashMemory);
    }

    /// <summary>
    /// connect a vcc to the controller
    /// </summary>
    public void PowerUp()
    {
        Reset();
        var consumedCycles = 0;
        while (consumedCycles < 1000)
        {
            consumedCycles += _cpu.RunNextInstruction();
        }
    }

    /// <summary>
    /// remove a vcc from the controller
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void PowerDown()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// reset the micro controller
    /// </summary>
    public void Reset()
    {
        _cpu.Reset();
        _ram.Reset();
    }
}
