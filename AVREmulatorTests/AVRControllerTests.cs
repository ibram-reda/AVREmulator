using AVREmulator;
using System;
using System.Collections.Generic;
using System.IO;
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
        var ContaingFolder = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent;
        var path = Path.Combine(ContaingFolder.FullName, @"AVRTestProgram\ldiAndrjmp.hex");
        AVRController Controller = new(path);
        Controller.PowerUp();

        Assert.Equal(0x29, Controller.CPU.r18);

    }
}
