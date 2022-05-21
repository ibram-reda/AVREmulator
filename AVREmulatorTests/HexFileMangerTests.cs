using AVREmulator;
using Xunit;

namespace AVREmulatorTests;

public class HexFileMangerTests
{
    [Fact]
    public void LoadHexFileCorrectllyToRom()
    {
        FlashMemory flash = new(new());
        HexFileManger.Load("atmelTest.hex", flash);

        Assert.NotNull(flash.Memory);

        Assert.Equal(0x940c, flash.Memory[0]);
        Assert.Equal(0x004a, flash.Memory[1]);
        Assert.Equal(0x940c, flash.Memory[2]);
        Assert.Equal(0x0054, flash.Memory[3]);

        Assert.Equal(0x940c, flash.Memory[8]);
        Assert.Equal(0x0054, flash.Memory[9]);

        Assert.Equal(0x2411, flash.Memory[74]);
        Assert.Equal(0xBE1F, flash.Memory[75]);
        Assert.Equal(0xEFCF, flash.Memory[76]);

        Assert.Equal(0xBFCD, flash.Memory[79]);
        Assert.Equal(0x940E, flash.Memory[80]);

        Assert.Equal(0xB98A, flash.Memory[87]);
        Assert.Equal(0xB81B, flash.Memory[88]);
        Assert.Equal(0xB18B, flash.Memory[89]);
        Assert.Equal(0x9580, flash.Memory[90]);

        Assert.Equal(0x0000, flash.Memory[100]);
        Assert.Equal(0xCFF3, flash.Memory[101]);
        Assert.Equal(0x94F8, flash.Memory[102]);
        Assert.Equal(0xCFFF, flash.Memory[103]);
    }

    [Fact]
    public void LineDiscriptionTest()
    {
        string line = ":1000B0001BB88BB180958BB92FEF8FE493EC215057";
        LineDiscription info = new LineDiscription(line);

        Assert.Equal((uint)0x10, info.Size);
        Assert.Equal(0x00B0, info.Address);
        Assert.Equal((uint)0, info.LineType);
        Assert.NotEmpty(info.Data);
        Assert.Equal(8, info.Data.Length);
    }
}


