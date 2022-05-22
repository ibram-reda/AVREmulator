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
    public void PowerUP_Test()
    {
        AVRController Controller = new(@".\AVRTestProgram\ldiAndrjmp.hex");
        Controller.PowerUp();

        Assert.Equal(0x29, Controller.CPU.r18);

    }
}
