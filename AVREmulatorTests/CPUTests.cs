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

    [Fact]
    public void DecodeInstruction_nop_test()
    {
        ushort opcode = 0;
        var pc = 52;
        CPU cpu = new CPU(new(), new());
        cpu.PC = pc;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal("NOP", instruction.Verb);
        Assert.Equal("NOP", instruction.Mnemonics);
        Assert.Equal(1, instruction.WestedCycle);

        instruction.Executable.Invoke();
        Assert.Equal(pc + 1, cpu.PC);
    }

    [Theory]
    [InlineData(0x2C01,  0,  1)]
    [InlineData(0x2c60,  6,  0)]
    [InlineData(0x2Ce5, 14,  5)]
    [InlineData(0x2Cff, 15, 15)]
    [InlineData(0x2d01, 16,  1)]
    [InlineData(0x2d1f, 17, 15)]
    [InlineData(0x2da0, 26,  0)]
    [InlineData(0x2df8, 31,  8)]
    [InlineData(0x2e00,  0, 16)]
    [InlineData(0x2e51,  5, 17)]
    [InlineData(0x2eff, 15, 31)]
    [InlineData(0x2f00, 16, 16)]
    [InlineData(0x2fed, 30, 29)]
    [InlineData(0x2fc0, 28, 16)]
    [InlineData(0x2fff, 31, 31)]
    public void DecodeInstruction_MOv_test(ushort opcode,  int dest, int source)
    {
        string mnemonics = $"MOV r{dest}, r{source}";
        CPU cpu = new CPU(new(), new());
        cpu.r[dest] = 0xca;
        cpu.r[source] = 0xfb;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(mnemonics, instruction.Mnemonics);
        Assert.Equal("MOV", instruction.Verb);

        instruction.Executable.Invoke();

        Assert.Equal(0xfb, cpu.r[dest]);
        Assert.Equal(0xfb, cpu.r[source]);

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
        Assert.Equal(1, instruction.WestedCycle);

        instruction.Executable.Invoke();

        Assert.Equal(k, cpu.r[d]); // << most important
    }
    
    [Theory]
    [InlineData(0xcfff, 0x12, -01, 0x12)]
    [InlineData(0xcffE, 0x12, -02, 0x11)]
    [InlineData(0xc002, 0x12,  02, 0x15)]
    public void DecodeInstruction_RJMP_test(ushort opcode, int pc, int k, int ExpectedPC)
    {
        CPU cpu = new CPU(new(), new());
        cpu.PC = pc;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal("RJMP", instruction.Verb);
        Assert.Equal(2, instruction.WestedCycle);
        Assert.Equal($"RJMP 0x{k:x3}", instruction.Mnemonics, ignoreCase: true);

        instruction.Executable.Invoke();

        Assert.Equal(ExpectedPC, cpu.PC);
    }
    
    [Fact]
    public void DecodeInstruction_MOVW_test()
    {
        ushort opcode =  0x016E; // movw r12, r28
        CPU cpu = new CPU(new(), new());
        cpu.r28 = 0xf5;
        cpu.r29 = 0x68;

        var instruction = cpu.DecodeInstruction(opcode);
        instruction.Executable.Invoke();

        Assert.Equal(cpu.r28, cpu.r12);
        Assert.Equal(cpu.r29, cpu.r13);
        Assert.Equal("MOVW", instruction.Verb);
        Assert.Equal(1, instruction.WestedCycle);
        Assert.Equal("MOVW r12, r28", instruction.Mnemonics, ignoreCase:true);
    }




    //[Theory]
    //[InlineData(0xfb,10, 0xce, 0xff)] // -5 * 10 = 0xffce
    //[InlineData(0xfb,0xfb,25,0)] //-5*-5=25
    //[InlineData(5,5,25,0)] //5*5=25
    //[InlineData(5,0,0,0,true)] //5*0=0
    //[InlineData(80,1000,0x80,0x38,false,true)] //80*1000=80000 overflow
    //public void MULS_instruction_test(byte v1,byte v2,byte expectedr0,byte expectedr1,bool zFlag = false,bool cFlag =false)
    //{
    //    CPU cpu = new CPU(new(), new());
    //    cpu.r20 = v1; //-5
    //    cpu.r21 = v2;

    //    var instruction = cpu.MULS(0x0245);//MULS r20*r21
    //    instruction.Executable.Invoke();

    //    Assert.Equal(expectedr1, cpu.r1);
    //    Assert.Equal(expectedr0, cpu.r0);
    //    Assert.Equal(zFlag,cpu.GetFlag(CPU.Flag.Z));
    //    Assert.Equal(cFlag,cpu.GetFlag(CPU.Flag.C));
    //}
}
