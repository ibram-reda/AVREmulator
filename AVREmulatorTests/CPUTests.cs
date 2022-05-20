using AVREmulator;
using Xunit;

namespace AVREmulatorTests;

public class CPUTests
{
    [Fact]
    public void CPURegisters_Maped_Correctly()
    {
        CPU cpu = new CPU();
        for(int i = 0; i < 32; i++)
        {
            Assert.Equal(byte.MinValue,cpu.r[i]);

            var register = cpu.GetType().GetProperty($"r{i}");
            Assert.Equal(byte.MinValue, register?.GetValue(cpu));
            
            register?.SetValue(cpu, (byte)0x62);
            Assert.Equal((byte)0x62,cpu.r[i]);
            Assert.Equal((byte)0x62, register?.GetValue(cpu));

        }
    }
}
