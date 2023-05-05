using AVREmulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AVREmulatorTests;

public class FlashMemoryTests
{
	private readonly FlashMemory _flash;
	public FlashMemoryTests()
	{
		_flash = new();
	}
	[Fact]
	public void ReadAndWriteCorrectly()
	{
		UInt16 value = 0x56ab;
		int address = 0xff;
		_flash.Write(address, value);

		var actualValue = _flash.Read(address);

		Assert.Equal(value, actualValue);
	}

}
