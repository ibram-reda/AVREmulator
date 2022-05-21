using AVREmulator;
using Xunit;

namespace AVREmulatorTests;

public class RamTests
{
    [Fact]
    public void ReadAndWriteCorrectly()
    {
        byte value = 0x56;
        int address = 0xff;
        var ram = new Ram(new());
        ram.Write(address, value);
        var actualValue = ram.Read(address);
        Assert.Equal(value, actualValue);
    }

    [Fact]
    public void ResetTest()
    {
        byte value = 0x56;
        int address = 0xff;
        var ram = new Ram(new());
        ram.Write(address, value);

        ram.Reset();

        var actualValue = ram.Read(address);
        Assert.Equal(0, actualValue);

    }
}
