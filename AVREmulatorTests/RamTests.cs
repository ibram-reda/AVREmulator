using AVREmulator;
using Xunit;

namespace AVREmulatorTests;

public class RamTests
{
    private readonly Ram _ram;
    public RamTests()
    {
        _ram = new();
    }
    [Fact]
    public void ReadAndWriteCorrectly()
    {
        byte value = 0x56;
        int address = 0xff;        
        _ram.Write(address, value);
        
        var actualValue = _ram.Read(address);
       
        Assert.Equal(value, actualValue);
    }

    [Fact]
    public void ResetTest()
    {
        byte value = 0x56;
        int address = 0xff;        
        _ram.Write(address, value);

        _ram.Reset();
        var actualValue = _ram.Read(address);
        
        Assert.Equal(0, actualValue);
    }
}
