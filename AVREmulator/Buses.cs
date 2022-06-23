namespace AVREmulator;

public enum ControllSignal
{
    READ,
    WRITE,
}

public interface IDataBusDevice
{
    public void ConnectTO(DataBus dataBus);
    public byte Read(int Address);
    public void Write(int Address, byte DataValue);
    public DeviceInformation DeviceInformation { get;  }
}

public class DeviceInformation
{
    public string Name { get; set; } = string.Empty;
    public int StartAddress { get; set; }
    public int EndAddress { get; set; }
}

public class DataBus
{
    public Dictionary<(int from, int to), IDataBusDevice> ConnectedDevicesMap { get; set; } = new();

    public bool Register(int from,int to, IDataBusDevice device)
    {
        // To Do : check if period is correct
        ConnectedDevicesMap[(from, to)] = device;
        return true;
    }

    public byte Read(int Address)
    {
        var device = GetDataBusDevice(Address);
        return device.Read(Address);
    }

    public void Write(int Address, byte DataValue)
    {
        var device = GetDataBusDevice(Address);
        device.Write(Address, DataValue);
    }

    public IDataBusDevice GetDataBusDevice(int Address)
    {
        foreach (var device in ConnectedDevicesMap)
        {
            if (device.Key.from <= Address && Address <= device.Key.to)
            {
                return device.Value;
            }
        }
        throw new Exception($"no devices in Databus serve this Address : {Address}");
    }
     
}

public class ProgramBus
{
    // TO DO: improve this mechanism
    public FlashMemory flashMemory;
    public UInt16 Data { get; set; }
    public UInt16 Address { get; set; }
    public byte Control { get; set; }
}
