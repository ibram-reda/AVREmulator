using AVREmulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AVREmulatorTests;

public class RamTests
{
    [Fact]
    public void ReadAndWriteCorrectly()
    {
        byte value =  0x56;
        int address = 0xff;
        var ram = new Ram();
        ram.Write(address, value);
        var actualValue = ram.Read(address);
        Assert.Equal(value, actualValue);

    }

    [Fact]
    public void ResetTest()
    {
        byte value = 0x56;
        int address = 0xff;
        var ram = new Ram();
        ram.Write(address, value);
        var actualValue = ram.Read(address);
        Assert.Equal(value, actualValue);

        ram.Reset();

        actualValue = ram.Read(address);
        Assert.Equal(0, actualValue);


    }
}
