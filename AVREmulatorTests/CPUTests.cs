using AVREmulator;
using Xunit;

namespace AVREmulatorTests;

public class CPUTests
{

    [Fact]
    public void CPURegisters_Hardware_Maped_Correctly()
    {
        CPU cpu = new CPU(new(), new());
        for (int i = 0; i < 32; i++)
        {
            Assert.Equal(byte.MinValue, cpu.r[i]);

            var register = cpu.GetType().GetProperty($"r{i}");
            Assert.Equal(byte.MinValue, register?.GetValue(cpu));

            register?.SetValue(cpu, (byte)0x62);
            Assert.Equal((byte)0x62, cpu.r[i]);
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
    [InlineData(0x2C01, 0, 1)]
    [InlineData(0x2c60, 6, 0)]
    [InlineData(0x2Ce5, 14, 5)]
    [InlineData(0x2Cff, 15, 15)]
    [InlineData(0x2d01, 16, 1)]
    [InlineData(0x2d1f, 17, 15)]
    [InlineData(0x2da0, 26, 0)]
    [InlineData(0x2df8, 31, 8)]
    [InlineData(0x2e00, 0, 16)]
    [InlineData(0x2e51, 5, 17)]
    [InlineData(0x2eff, 15, 31)]
    [InlineData(0x2f00, 16, 16)]
    [InlineData(0x2fed, 30, 29)]
    [InlineData(0x2fc0, 28, 16)]
    [InlineData(0x2fff, 31, 31)]
    public void DecodeInstruction_MOv_test(ushort opcode, int dest, int source)
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

        Assert.Equal($"LDI r{d}, 0x{k:x2}", instruction.Mnemonics, ignoreCase: true);
        Assert.Equal("LDI", instruction.Verb);
        Assert.Equal($"r{d}", instruction.Operand1);
        Assert.Equal(1, instruction.WestedCycle);

        instruction.Executable.Invoke();

        Assert.Equal(k, cpu.r[d]); // << most important
    }

    [Theory]
    [InlineData(0xcfff, 0x12, -01, 0x12)]
    [InlineData(0xcffE, 0x12, -02, 0x11)]
    [InlineData(0xc002, 0x12, 02, 0x15)]
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
        ushort opcode = 0x016E; // movw r12, r28
        CPU cpu = new CPU(new(), new());
        cpu.r28 = 0xf5;
        cpu.r29 = 0x68;
        cpu.PC = 100;

        var instruction = cpu.DecodeInstruction(opcode);
        instruction.Executable.Invoke();

        Assert.Equal(cpu.r28, cpu.r12);
        Assert.Equal(cpu.r29, cpu.r13);
        Assert.Equal("MOVW", instruction.Verb);
        Assert.Equal(1, instruction.WestedCycle);
        Assert.Equal("MOVW r12, r28", instruction.Mnemonics, ignoreCase: true);
        Assert.Equal(101, cpu.PC);
    }


    [Theory]
    [InlineData(0x91Fc, 31)]
    [InlineData(0x91ec, 30)]
    [InlineData(0x911c, 17)]
    [InlineData(0x910c, 16)]
    [InlineData(0x90fc, 15)]
    [InlineData(0x90ec, 14)]
    [InlineData(0x901c, 1)]
    [InlineData(0x900c, 0)]
    public void DecodeInstruction_LD_X_test(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, X";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.X = address;
        cpu.PC = 100;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, cpu.r[d]);
        Assert.Equal(101, cpu.PC);
    }

    [Theory]
    [InlineData(0x91FD, 31)]
    [InlineData(0x91eD, 30)]
    [InlineData(0x911D, 17)]
    [InlineData(0x910D, 16)]
    [InlineData(0x90fD, 15)]
    [InlineData(0x90eD, 14)]
    [InlineData(0x901D, 1)]
    [InlineData(0x900D, 0)]
    public void DecodeInstruction_LD_X_postincrement_test(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, X+";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.X = address;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, cpu.r[d]);
        Assert.Equal(address + 1, cpu.X);

    }

    [Theory]
    [InlineData(0x91FE, 31)]
    [InlineData(0x91eE, 30)]
    [InlineData(0x911E, 17)]
    [InlineData(0x910E, 16)]
    [InlineData(0x90fE, 15)]
    [InlineData(0x90eE, 14)]
    [InlineData(0x901E, 1)]
    [InlineData(0x900E, 0)]
    public void DecodeInstruction_LD_X_preDecremant_test(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, -X";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.X = address;
        cpu.X++; // should be decrement to address afterr execution
        cpu.PC = 100;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, cpu.r[d]);
        Assert.Equal(address, cpu.X);
        Assert.Equal(101, cpu.PC);

    }

    [Theory]
    [InlineData(0x91AD, 26)]
    [InlineData(0x91BD, 27)]
    public void DecodeInstruction_LD_X_postincrement_Throw_undifendBehaviour_on_r26(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, X+";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.X = address;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        Assert.Throws<UndifiendBehaviorException>(instruction.Executable);
    }

    [Theory]
    [InlineData(0x91AE, 26)]
    [InlineData(0x91BE, 27)]
    public void DecodeInstruction_LD_X_PreDecrement_Throw_undifendBehaviour_on_r26(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, -X";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.X = address;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        Assert.Throws<UndifiendBehaviorException>(instruction.Executable);
    }

    [Theory]
    [InlineData(0x8008,  0)]
    [InlineData(0x8018,  1)]
    [InlineData(0x8028,  2)]
    [InlineData(0x8038,  3)]
    [InlineData(0x8048,  4)]
    [InlineData(0x8058,  5)]
    [InlineData(0x80D8, 13)]
    [InlineData(0x80E8, 14)]
    [InlineData(0x80F8, 15)]
    [InlineData(0x8108, 16)]
    [InlineData(0x8118, 17)]
    [InlineData(0x81C8, 28)]
    [InlineData(0x81F8, 31)]
    public void DecodeInstruction_LD_Y_test(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, Y";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.Y = address;
        cpu.PC = 100;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, cpu.r[d]);
        Assert.Equal(101, cpu.PC);
    }

    [Theory]
    [InlineData(0x91F9, 31)]
    [InlineData(0x91e9, 30)]
    [InlineData(0x9119, 17)]
    [InlineData(0x9109, 16)]
    [InlineData(0x90f9, 15)]
    [InlineData(0x90e9, 14)]
    [InlineData(0x9019, 1)]
    [InlineData(0x9009, 0)]
    public void DecodeInstruction_LD_Y_postincrement_test(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, Y+";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.Y = address;
        cpu.PC = 120;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, cpu.r[d]);
        Assert.Equal(address + 1, cpu.Y);
        Assert.Equal(121, cpu.PC);

    }

    [Theory]
    [InlineData(0x91FA, 31)]
    [InlineData(0x91eA, 30)]
    [InlineData(0x911A, 17)]
    [InlineData(0x910A, 16)]
    [InlineData(0x90fA, 15)]
    [InlineData(0x90eA, 14)]
    [InlineData(0x901A, 1)]
    [InlineData(0x900A, 0)]
    public void DecodeInstruction_LD_Y_preDecremant_test(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, -Y";
        int address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.Y = (ushort)(address +1);
        cpu.PC = 100;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, cpu.r[d]);
        Assert.Equal(address, cpu.Y);
        Assert.Equal(101, cpu.PC);

    }

    [Theory]
    [InlineData(0x91C9, 28)]
    [InlineData(0x91D9, 29)]
    public void DecodeInstruction_LD_Y_postincrement_Throw_undifendBehaviour_on_r28(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, Y+";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.Y = address;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        Assert.Throws<UndifiendBehaviorException>(instruction.Executable);
    }

    [Theory]
    [InlineData(0x91CA, 28)]
    [InlineData(0x91DA, 29)]
    public void DecodeInstruction_LD_Y_PreDecrement_Throw_undifendBehaviour_on_r28(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, -Y";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.Y = address;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        Assert.Throws<UndifiendBehaviorException>(instruction.Executable);
    }

    ///****************************************************


    [Theory]
    [InlineData(0x8000, 0)]
    [InlineData(0x8010, 1)]
    [InlineData(0x8020, 2)]
    [InlineData(0x8030, 3)]
    [InlineData(0x8040, 4)]
    [InlineData(0x8050, 5)]
    [InlineData(0x80D0, 13)]
    [InlineData(0x80E0, 14)]
    [InlineData(0x80F0, 15)]
    [InlineData(0x8100, 16)]
    [InlineData(0x8110, 17)]
    [InlineData(0x81C0, 28)]
    [InlineData(0x81F0, 31)]
    public void DecodeInstruction_LD_Z_test(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, Z";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.Z = address;
        cpu.PC = 100;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        Assert.Equal(1, instruction.WestedCycle);

        instruction.Executable.Invoke();

        Assert.Equal(val, cpu.r[d]);
        Assert.Equal(101, cpu.PC);
    }

    [Theory]
    [InlineData(0x91d1, 29)]
    [InlineData(0x91c1, 28)]
    [InlineData(0x9111, 17)]
    [InlineData(0x9101, 16)]
    [InlineData(0x90f1, 15)]
    [InlineData(0x90e1, 14)]
    [InlineData(0x9011, 1)]
    [InlineData(0x9001, 0)]
    public void DecodeInstruction_LD_Z_postincrement_test(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, Z+";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.Z = address;
        cpu.PC = 120;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        Assert.Equal(1, instruction.WestedCycle);

        instruction.Executable.Invoke();

        Assert.Equal(val, cpu.r[d]);
        Assert.Equal(address + 1, cpu.Z);
        Assert.Equal(121, cpu.PC);

    }

    [Theory]
    [InlineData(0x91d2, 29)]
    [InlineData(0x91c2, 28)]
    [InlineData(0x9112, 17)]
    [InlineData(0x9102, 16)]
    [InlineData(0x90f2, 15)]
    [InlineData(0x90e2, 14)]
    [InlineData(0x9012, 1)]
    [InlineData(0x9002, 0)]
    public void DecodeInstruction_LD_Z_preDecremant_test(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, -Z";
        int address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.Z = (ushort)(address + 1);
        cpu.PC = 100;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        Assert.Equal(2, instruction.WestedCycle);

        instruction.Executable.Invoke();

        Assert.Equal(val, cpu.r[d]);
        Assert.Equal(address, cpu.Z);
        Assert.Equal(101, cpu.PC);

    }

    [Theory]
    [InlineData(0x91f1, 31)]
    [InlineData(0x91e1, 30)]
    public void DecodeInstruction_LD_Z_postincrement_Throw_undifendBehaviour_on_r28(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, Z+";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.Z = address;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        Assert.Throws<UndifiendBehaviorException>(instruction.Executable);
    }

    [Theory]
    [InlineData(0x91f2, 31)]
    [InlineData(0x91e2, 30)]
    public void DecodeInstruction_LD_Z_PreDecrement_Throw_undifendBehaviour_on_r28(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, -Z";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.Z = address;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        Assert.Throws<UndifiendBehaviorException>(instruction.Executable);
    }

    [Fact]
    public void DecodeInstruction_LPM_test()
    {
        ushort opcode = 0x95C8;
        string Mnemonics = "LPM";
        int address = 100;
        ushort val = 0xefcd;

        ProgramBus programBus = new();
        FlashMemory flashMemory = new(programBus);
        flashMemory.Write(address, val);
        CPU cpu = new(new(), programBus);
        cpu.Z = (ushort)(address << 1);
        cpu.r0 = 0;


        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        instruction.Executable.Invoke();

        Assert.Equal(0xcd, cpu.r0);

        cpu.Z |=1 ;
        cpu.r0 = 0;
        instruction = cpu.DecodeInstruction(opcode);
        instruction.Executable.Invoke();
        Assert.Equal(0xef, cpu.r0);

    }

    [Theory]
    [InlineData(0X9004, 0)]
    [InlineData(0X9014, 1)]
    [InlineData(0X9024, 2)]
    [InlineData(0X9034, 3)]
    [InlineData(0X9044, 4)]
                     
    [InlineData(0X90c4, 12)]
    [InlineData(0X90d4, 13)]
    [InlineData(0X90e4, 14)]
    [InlineData(0X90f4, 15)]
                     
    [InlineData(0X9104, 16)]
    [InlineData(0X9114, 17)]
    [InlineData(0X91E4, 30)]
    [InlineData(0X91F4, 31)]
    public void DecodeInstruction_LPM_Z_test(ushort opcode, int d)
    {
        string Mnemonics = $"LPM r{d}, Z";
        int address = 100;
        ushort val = 0xefcd;

        ProgramBus programBus = new();
        FlashMemory flashMemory = new(programBus);
        flashMemory.Write(address, val);
        CPU cpu = new(new(), programBus);
        cpu.Z = (ushort)(address << 1);
        


        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        instruction.Executable.Invoke();

        Assert.Equal(0xcd, cpu.r[d]);
        // try to fetch high byte
        cpu.Z = (ushort)((address<<1)|1);
        instruction = cpu.DecodeInstruction(opcode);
        instruction.Executable.Invoke();
        Assert.Equal(0xef, cpu.r[d]);
    }

    [Theory]
    [InlineData(0X9005, 0)]
    [InlineData(0X9015, 1)]
    [InlineData(0X9025, 2)]
    [InlineData(0X9035, 3)]
    [InlineData(0X9045, 4)]
                     
    [InlineData(0X90c5, 12)]
    [InlineData(0X90d5, 13)]
    [InlineData(0X90e5, 14)]
    [InlineData(0X90f5, 15)]
                     
    [InlineData(0X9105, 16)]
    [InlineData(0X9115, 17)]
    [InlineData(0X91c5, 28)]
    [InlineData(0X91d5, 29)]
    public void DecodeInstruction_LPM_Z_postdecrement_test(ushort opcode, int d)
    {
        string Mnemonics = $"LPM r{d}, Z+";
        int flashAddress = 100;
        int zPointer = 100 << 1;
        ushort val = 0xefcd;

        ProgramBus programBus = new();
        FlashMemory flashMemory = new(programBus);
        flashMemory.Write(flashAddress, val);
        CPU cpu = new(new(), programBus);
        cpu.Z = (ushort)zPointer;
        cpu.PC = 400;



        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        Assert.Equal(3,instruction.WestedCycle);
        instruction.Executable.Invoke();

        Assert.Equal(0xcd, cpu.r[d]);
        Assert.Equal(zPointer + 1, cpu.Z); // check if Z incremented corectlly
        Assert.Equal(401, cpu.PC);

        // try to fetch high byte
        // z alredy incremented
        instruction = cpu.DecodeInstruction(opcode);
        instruction.Executable.Invoke();
        Assert.Equal(0xef, cpu.r[d]);
    }

    [Theory]
    [InlineData(0X91e5, 30)]
    [InlineData(0X91f5, 31)]
    public void DecodeInstruction_LPM_Z_postdecrement_throw_undifendBehaviour(ushort opcode, int d)
    {
        string Mnemonics = $"LPM r{d}, Z+";
        int flashAddress = 100;
        int zPointer = 100 << 1;
        ushort val = 0xefcd;

        ProgramBus programBus = new();
        FlashMemory flashMemory = new(programBus);
        flashMemory.Write(flashAddress, val);
        CPU cpu = new(new(), programBus);
        cpu.Z = (ushort)zPointer;
        cpu.PC = 400;



        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        
        Assert.Throws<UndifiendBehaviorException>(instruction.Executable);
    }

    [Theory]
    [InlineData(0X900F,0)]
    [InlineData(0X901F,1)]
    [InlineData(0X902F,2)]
    [InlineData(0X903F,3)]
    [InlineData(0X904F,4)]

    [InlineData(0X90cF,12)]
    [InlineData(0X90dF,13)]
    [InlineData(0X90eF,14)]
    [InlineData(0X90fF,15)]

    [InlineData(0X910F,16)]
    [InlineData(0X911F, 17)]
    [InlineData(0X91EF, 30)]
    [InlineData(0X91FF, 31)]
    public void DecodeInstruction_POP_test(ushort opcode,int d)
    {
        string Mnemonics = $"POP r{d}";
        int address = 100;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address] = val;
        var cpu = new CPU(dataBus, new());
        cpu.SP = (ushort)(address-1);
        cpu.PC = 102;
        cpu.r[d] = 0;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        instruction.Executable.Invoke();
        Assert.Equal(val, cpu.r[d]);
        Assert.Equal(address, cpu.SP);
        Assert.Equal(103, cpu.PC);


    }

    [Theory]
    [InlineData(0X9000, 0)]
    [InlineData(0X9010, 1)]
    [InlineData(0X9020, 2)]
    [InlineData(0X9030, 3)]
    [InlineData(0X9040, 4)]
                     
    [InlineData(0X90c0, 12)]
    [InlineData(0X90d0, 13)]
    [InlineData(0X90e0, 14)]
    [InlineData(0X90f0, 15)]
                     
    [InlineData(0X9100, 16)]
    [InlineData(0X9110, 17)]
    [InlineData(0X91E0, 30)]
    [InlineData(0X91F0, 31)]
    public void DecodeInstruction_LDS_Test(ushort opcode, int d)
    {

        ushort k = 0x052d;
        string Mnemonics = $"LDS r{d}, 0x{k:x4}";
        byte val = 0xce;
        int flashAddress = 100;

        ProgramBus programBus = new();
        FlashMemory flashMemory = new(programBus);
        flashMemory.Write(flashAddress, opcode);
        flashMemory.Write(flashAddress +1, k);
        DataBus dataBus = new();
        Ram ram = new(dataBus);
        ram.Write(k, val);
        CPU cpu = new(dataBus, programBus);
        cpu.PC = flashAddress;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics,instruction.Mnemonics);
        Assert.Equal(2,instruction.WestedCycle);

        instruction.Executable.Invoke();

        Assert.Equal(val, cpu.r[d]);
        Assert.Equal(flashAddress + 2, cpu.PC);
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
