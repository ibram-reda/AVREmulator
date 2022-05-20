using AVREmulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AVREmulatorTests;

public class AVRControllerTests
{
    [Fact]
    public void LoadHexSuccessfully()
    {
        AVRController controller = new("atmelTest.hex");

        FlashMemory flash = controller.FlashMemory;
        Assert.NotNull(flash.Memory);

        Assert.Equal(0x940c, flash.Memory[0]);
        Assert.Equal(0x004a, flash.Memory[1]);
        Assert.Equal(0x940c, flash.Memory[2]);
        Assert.Equal(0x0054, flash.Memory[3]);

        Assert.Equal(0x940c, flash.Memory[0x10]);
        Assert.Equal(0x0054, flash.Memory[0x2f]);

        Assert.Equal(0x0054, flash.Memory[0x2f]);
        Assert.Equal(0xcfff, flash.Memory[0xcf]);
    }
}
