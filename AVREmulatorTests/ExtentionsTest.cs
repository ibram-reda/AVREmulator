using AVREmulator;
using Xunit;

namespace AVREmulatorTests;

public class ExtentionsTest
{
	[Theory]
	[InlineData(0xfcde, 0, 0xe)]
	[InlineData(0xfcde, 1, 0xd)]
	[InlineData(0xfcde, 2, 0xc)]
	[InlineData(0xfcde, 3, 0xf)]
	public void GetNippleTest(ushort opcode, int nipplenumber, int expected)
	{
		var actual = opcode.GetNipple(nipplenumber);
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void CompineTest()
	{
		byte b1 = 0x5a;
		byte b2 = 0x25;
		var combine = Extentions.Combine(b1, b2);
		Assert.Equal(0x5a25, combine);
	}
}
