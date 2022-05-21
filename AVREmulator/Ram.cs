namespace AVREmulator;

public class Ram
{
    public const int RAM_MAX_SIZE = 0xffff;
    public byte[] RAM = new byte[RAM_MAX_SIZE];
    private DataBus _dataBus;

    public Ram(DataBus dataBus)
    {
        _dataBus = dataBus;
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
