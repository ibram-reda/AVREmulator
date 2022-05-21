using AVREmulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AVREmulatorTests;

public class ExtentionsTest
{
    [Theory]
    [InlineData(0xfcde,0,0xe)]
    [InlineData(0xfcde,1,0xd)]
    [InlineData(0xfcde,2,0xc)]
    [InlineData(0xfcde,3,0xf)]
    public void GetNippleTest(ushort opcode,int nipplenumber,int expected)
    {
        var actual = opcode.GetNipple(nipplenumber);
        Assert.Equal(expected, actual);
    }
}
