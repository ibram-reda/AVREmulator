using AVREmulator;
using Xunit;

namespace AVREmulatorTests;

public class CPUTests
{
    
    [Fact]
    public void CPURegisters_Hardware_Maped_Correctly()
    {
        CPU cpu = new CPU(new(),new());
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

    [Theory]
    [InlineData(0xef0f,16,0xff)] // ldi r16,0xff
    [InlineData(0xefff,31,0xff)] // ldi r31,0xff
    [InlineData(0xe512,17,0x52)] // ldi r17,0x52
    [InlineData(0xe0d8, 29, 0x08)] // ldi	r29, 0x08
    public void LDi_Factory_test(ushort opcode,int d,byte k)
    {
        CPU cpu = new CPU(new(), new());
        
        var instruction = cpu.LDi(opcode);
        instruction.Executable.Invoke();

        Assert.Equal(k, cpu.r[d]); // << most important
        Assert.Equal("LDI", instruction.Verb);
        Assert.Equal($"r{d}", instruction.Operand1);
        Assert.Equal($"LDI r{d}, 0x{k:x2}",instruction.Mnemonics,ignoreCase:true);
        Assert.Equal(1, instruction.WestedCycle);

    }

    [Theory]
    [InlineData(0xef0f, 16, 0xff)] // ldi r16,0xff
    [InlineData(0xefff, 31, 0xff)] // ldi r31,0xff
    [InlineData(0xe512, 17, 0x52)] // ldi r17,0x52
    [InlineData(0xe0d8, 29, 0x08)] // ldi	r29, 0x08
    public void DecodeInstruction_ldi_test(ushort opcode, int d, byte k)
    {
        CPU cpu = new CPU(new(), new());

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal($"LDI r{d}, 0x{k:x2}",instruction.Mnemonics,ignoreCase:true);
        Assert.Equal("LDI", instruction.Verb);
        Assert.Equal($"r{d}", instruction.Operand1);
    }
    [Theory]
    [InlineData(0xcfff,0x12,0x12)]
    [InlineData(0xcffE,0x12,0x11)]
    [InlineData(0xc002,0x12,0x15)]
    public void RJMP_Factory_test(ushort opcode,int pc,int ExpectedPC)
    {
        CPU cpu = new CPU(new(), new());
        cpu.PC = pc;

        var instruction = cpu.RJMP(opcode);
        instruction.Executable.Invoke();

        Assert.Equal(ExpectedPC, cpu.PC);
        Assert.Equal("RJMP", instruction.Verb);
        Assert.Equal(2, instruction.WestedCycle);
    }

    [Theory]
    [InlineData(0xcfff, 0x12,  -01)]
    [InlineData(0xcffE, 0x12, -02)]
    [InlineData(0xc002, 0x12,  02)]
    public void DecodeInstruction_RJMP_test(ushort opcode, int pc, int k)
    {
        CPU cpu = new CPU(new(), new());
        cpu.PC = pc;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal("RJMP", instruction.Verb);
        Assert.Equal(2, instruction.WestedCycle);
        Assert.Equal($"RJMP 0x{k:x3}", instruction.Mnemonics, ignoreCase: true);
    }
}
