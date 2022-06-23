namespace AVREmulator;

public class Ram : IDataBusDevice
{
    private readonly DeviceInformation _deviceInformation;
    public readonly int RAM_MAX_SIZE;
    public byte[] RAM ;

    public DeviceInformation DeviceInformation => _deviceInformation;


    public Ram() : this(0xffff) 
    {
    }
    public Ram(int SIZE)
    {
        RAM_MAX_SIZE = SIZE;
        RAM = new byte[RAM_MAX_SIZE];
        _deviceInformation = new()
        {
            Name = "RAM",
            StartAddress = 0,
            EndAddress = SIZE
        };
    }
    public void ConnectTO(DataBus dataBus)
    {
        dataBus.Register(0, RAM_MAX_SIZE, this);
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
