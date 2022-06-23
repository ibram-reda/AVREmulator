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
    #region test data
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
    #endregion
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
    #region test data
    [InlineData(0xef0f, 16, 0xff)] // ldi r16,0xff
    [InlineData(0xefff, 31, 0xff)] // ldi r31,0xff
    [InlineData(0xe512, 17, 0x52)] // ldi r17,0x52
    [InlineData(0xe0d8, 29, 0x08)] // ldi	r29, 0x08
    #endregion
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
    #region test data
    [InlineData(0xcfff, 0x12, -01, 0x12)]
    [InlineData(0xcffE, 0x12, -02, 0x11)]
    [InlineData(0xc002, 0x12, 02, 0x15)]
    #endregion
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
    #region test data
    [InlineData(0x91Fc, 31)]
    [InlineData(0x91ec, 30)]
    [InlineData(0x911c, 17)]
    [InlineData(0x910c, 16)]
    [InlineData(0x90fc, 15)]
    [InlineData(0x90ec, 14)]
    [InlineData(0x901c, 1)]
    [InlineData(0x900c, 0)]
    #endregion
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
    #region test data
    [InlineData(0x91FD, 31)]
    [InlineData(0x91eD, 30)]
    [InlineData(0x911D, 17)]
    [InlineData(0x910D, 16)]
    [InlineData(0x90fD, 15)]
    [InlineData(0x90eD, 14)]
    [InlineData(0x901D, 1)]
    [InlineData(0x900D, 0)]
    #endregion
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
    #region test data
    [InlineData(0x91FE, 31)]
    [InlineData(0x91eE, 30)]
    [InlineData(0x911E, 17)]
    [InlineData(0x910E, 16)]
    [InlineData(0x90fE, 15)]
    [InlineData(0x90eE, 14)]
    [InlineData(0x901E, 1)]
    [InlineData(0x900E, 0)]
    #endregion
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
    #region test data
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
    #endregion
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
    #region test data
    [InlineData(0x8009, 00, 1)]
    [InlineData(0x8019, 01, 1)]
    [InlineData(0x8029, 02, 1)]
    [InlineData(0x8039, 03, 1)]
    [InlineData(0x8049, 04, 1)]
    [InlineData(0x8059, 05, 1)]
    [InlineData(0x80D9, 13, 1)]
    [InlineData(0x80E9, 14, 1)]
    [InlineData(0x80F9, 15, 1)]
    [InlineData(0x8109, 16, 1)]
    [InlineData(0x8119, 17, 1)]
    [InlineData(0x81C9, 28, 1)]
    [InlineData(0x81F9, 31, 1)]

    [InlineData(0x800B, 00, 3)]
    [InlineData(0x801B, 01, 3)]
    [InlineData(0x802B, 02, 3)]
    [InlineData(0x803B, 03, 3)]
    [InlineData(0x804B, 04, 3)]
    [InlineData(0x805B, 05, 3)]
    [InlineData(0x80DB, 13, 3)]
    [InlineData(0x80EB, 14, 3)]
    [InlineData(0x80FB, 15, 3)]
    [InlineData(0x810B, 16, 3)]
    [InlineData(0x811B, 17, 3)]
    [InlineData(0x81CB, 28, 3)]
    [InlineData(0x81FB, 31, 3)]

    [InlineData(0x800F, 00, 7)]
    [InlineData(0x801F, 01, 7)]
    [InlineData(0x802F, 02, 7)]
    [InlineData(0x803F, 03, 7)]
    [InlineData(0x804F, 04, 7)]
    [InlineData(0x805F, 05, 7)]
    [InlineData(0x80DF, 13, 7)]
    [InlineData(0x80EF, 14, 7)]
    [InlineData(0x80FF, 15, 7)]
    [InlineData(0x810F, 16, 7)]
    [InlineData(0x811F, 17, 7)]
    [InlineData(0x81CF, 28, 7)]
    [InlineData(0x81FF, 31, 7)]

    [InlineData(0x8408, 00, 8)]
    [InlineData(0x8418, 01, 8)]
    [InlineData(0x8428, 02, 8)]
    [InlineData(0x8438, 03, 8)]
    [InlineData(0x8448, 04, 8)]
    [InlineData(0x8458, 05, 8)]
    [InlineData(0x84D8, 13, 8)]
    [InlineData(0x84E8, 14, 8)]
    [InlineData(0x84F8, 15, 8)]
    [InlineData(0x8508, 16, 8)]
    [InlineData(0x8518, 17, 8)]
    [InlineData(0x85C8, 28, 8)]
    [InlineData(0x85F8, 31, 8)]

    [InlineData(0x840C, 00, 12)]
    [InlineData(0x841C, 01, 12)]
    [InlineData(0x842C, 02, 12)]
    [InlineData(0x843C, 03, 12)]
    [InlineData(0x844C, 04, 12)]
    [InlineData(0x845C, 05, 12)]
    [InlineData(0x84DC, 13, 12)]
    [InlineData(0x84EC, 14, 12)]
    [InlineData(0x84FC, 15, 12)]
    [InlineData(0x850C, 16, 12)]
    [InlineData(0x851C, 17, 12)]
    [InlineData(0x85CC, 28, 12)]
    [InlineData(0x85FC, 31, 12)]

    [InlineData(0x840F, 00, 15)]
    [InlineData(0x841F, 01, 15)]
    [InlineData(0x842F, 02, 15)]
    [InlineData(0x843F, 03, 15)]
    [InlineData(0x844F, 04, 15)]
    [InlineData(0x845F, 05, 15)]
    [InlineData(0x84DF, 13, 15)]
    [InlineData(0x84EF, 14, 15)]
    [InlineData(0x84FF, 15, 15)]
    [InlineData(0x850F, 16, 15)]
    [InlineData(0x851F, 17, 15)]
    [InlineData(0x85CF, 28, 15)]
    [InlineData(0x85FF, 31, 15)]

    [InlineData(0x8C08, 00, 24)]
    [InlineData(0x8C18, 01, 24)]
    [InlineData(0x8C28, 02, 24)]
    [InlineData(0x8C38, 03, 24)]
    [InlineData(0x8C48, 04, 24)]
    [InlineData(0x8C58, 05, 24)]
    [InlineData(0x8CD8, 13, 24)]
    [InlineData(0x8CE8, 14, 24)]
    [InlineData(0x8CF8, 15, 24)]
    [InlineData(0x8D08, 16, 24)]
    [InlineData(0x8D18, 17, 24)]
    [InlineData(0x8DC8, 28, 24)]
    [InlineData(0x8DF8, 31, 24)]

    [InlineData(0x8C0F, 00, 31)]
    [InlineData(0x8C1F, 01, 31)]
    [InlineData(0x8C2F, 02, 31)]
    [InlineData(0x8C3F, 03, 31)]
    [InlineData(0x8C4F, 04, 31)]
    [InlineData(0x8C5F, 05, 31)]
    [InlineData(0x8CDF, 13, 31)]
    [InlineData(0x8CEF, 14, 31)]
    [InlineData(0x8CFF, 15, 31)]
    [InlineData(0x8D0F, 16, 31)]
    [InlineData(0x8D1F, 17, 31)]
    [InlineData(0x8DCF, 28, 31)]
    [InlineData(0x8DFF, 31, 31)]

    [InlineData(0xA009, 00, 33)]
    [InlineData(0xA019, 01, 33)]
    [InlineData(0xA029, 02, 33)]
    [InlineData(0xA039, 03, 33)]
    [InlineData(0xA049, 04, 33)]
    [InlineData(0xA059, 05, 33)]
    [InlineData(0xA0D9, 13, 33)]
    [InlineData(0xA0E9, 14, 33)]
    [InlineData(0xA0F9, 15, 33)]
    [InlineData(0xA109, 16, 33)]
    [InlineData(0xA119, 17, 33)]
    [InlineData(0xA1C9, 28, 33)]
    [InlineData(0xA1F9, 31, 33)]

    [InlineData(0xAC0F, 00, 63)]
    [InlineData(0xAC1F, 01, 63)]
    [InlineData(0xAC2F, 02, 63)]
    [InlineData(0xAC3F, 03, 63)]
    [InlineData(0xAC4F, 04, 63)]
    [InlineData(0xAC5F, 05, 63)]
    [InlineData(0xACDF, 13, 63)]
    [InlineData(0xACEF, 14, 63)]
    [InlineData(0xACFF, 15, 63)]
    [InlineData(0xAD0F, 16, 63)]
    [InlineData(0xAD1F, 17, 63)]
    [InlineData(0xADCF, 28, 63)]
    [InlineData(0xADFF, 31, 63)]
    #endregion
    public void DecodeInstruction_LDD_Y_test(ushort opcode, int d,int q)
    {
        string Mnemonics = $"LDD r{d}, Y+{q}";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address +q] = val;
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
    #region test data
    [InlineData(0x91F9, 31)]
    [InlineData(0x91e9, 30)]
    [InlineData(0x9119, 17)]
    [InlineData(0x9109, 16)]
    [InlineData(0x90f9, 15)]
    [InlineData(0x90e9, 14)]
    [InlineData(0x9019, 1)]
    [InlineData(0x9009, 0)]
    #endregion
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
    #region test data
    [InlineData(0x91FA, 31)]
    [InlineData(0x91eA, 30)]
    [InlineData(0x911A, 17)]
    [InlineData(0x910A, 16)]
    [InlineData(0x90fA, 15)]
    [InlineData(0x90eA, 14)]
    [InlineData(0x901A, 1)]
    [InlineData(0x900A, 0)]
    #endregion
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

    [Theory]
    #region test data
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
    #endregion
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
    #region test data
    [InlineData(0x8001, 00, 1)]
    [InlineData(0x8011, 01, 1)]
    [InlineData(0x8021, 02, 1)]
    [InlineData(0x8031, 03, 1)]
    [InlineData(0x8041, 04, 1)]
    [InlineData(0x8051, 05, 1)]
    [InlineData(0x80D1, 13, 1)]
    [InlineData(0x80E1, 14, 1)]
    [InlineData(0x80F1, 15, 1)]
    [InlineData(0x8101, 16, 1)]
    [InlineData(0x8111, 17, 1)]
    [InlineData(0x81C1, 28, 1)]
    [InlineData(0x81F1, 31, 1)]

    [InlineData(0x8003, 00, 3)]
    [InlineData(0x8013, 01, 3)]
    [InlineData(0x8023, 02, 3)]
    [InlineData(0x8033, 03, 3)]
    [InlineData(0x8043, 04, 3)]
    [InlineData(0x8053, 05, 3)]
    [InlineData(0x80D3, 13, 3)]
    [InlineData(0x80E3, 14, 3)]
    [InlineData(0x80F3, 15, 3)]
    [InlineData(0x8103, 16, 3)]
    [InlineData(0x8113, 17, 3)]
    [InlineData(0x81C3, 28, 3)]
    [InlineData(0x81F3, 31, 3)]

    [InlineData(0x8007, 00, 7)]
    [InlineData(0x8017, 01, 7)]
    [InlineData(0x8027, 02, 7)]
    [InlineData(0x8037, 03, 7)]
    [InlineData(0x8047, 04, 7)]
    [InlineData(0x8057, 05, 7)]
    [InlineData(0x80D7, 13, 7)]
    [InlineData(0x80E7, 14, 7)]
    [InlineData(0x80F7, 15, 7)]
    [InlineData(0x8107, 16, 7)]
    [InlineData(0x8117, 17, 7)]
    [InlineData(0x81C7, 28, 7)]
    [InlineData(0x81F7, 31, 7)]

    [InlineData(0x8400, 00, 8)]
    [InlineData(0x8410, 01, 8)]
    [InlineData(0x8420, 02, 8)]
    [InlineData(0x8430, 03, 8)]
    [InlineData(0x8440, 04, 8)]
    [InlineData(0x8450, 05, 8)]
    [InlineData(0x84D0, 13, 8)]
    [InlineData(0x84E0, 14, 8)]
    [InlineData(0x84F0, 15, 8)]
    [InlineData(0x8500, 16, 8)]
    [InlineData(0x8510, 17, 8)]
    [InlineData(0x85C0, 28, 8)]
    [InlineData(0x85F0, 31, 8)]

    [InlineData(0x8404, 00, 12)]
    [InlineData(0x8414, 01, 12)]
    [InlineData(0x8424, 02, 12)]
    [InlineData(0x8434, 03, 12)]
    [InlineData(0x8444, 04, 12)]
    [InlineData(0x8454, 05, 12)]
    [InlineData(0x84D4, 13, 12)]
    [InlineData(0x84E4, 14, 12)]
    [InlineData(0x84F4, 15, 12)]
    [InlineData(0x8504, 16, 12)]
    [InlineData(0x8514, 17, 12)]
    [InlineData(0x85C4, 28, 12)]
    [InlineData(0x85F4, 31, 12)]

    [InlineData(0x8406, 00, 14)]
    [InlineData(0x8416, 01, 14)]
    [InlineData(0x8426, 02, 14)]
    [InlineData(0x8436, 03, 14)]
    [InlineData(0x8446, 04, 14)]
    [InlineData(0x8456, 05, 14)]
    [InlineData(0x84D6, 13, 14)]
    [InlineData(0x84E6, 14, 14)]
    [InlineData(0x84F6, 15, 14)]
    [InlineData(0x8506, 16, 14)]
    [InlineData(0x8516, 17, 14)]
    [InlineData(0x85C6, 28, 14)]
    [InlineData(0x85F6, 31, 14)]

    [InlineData(0x8C00, 00, 24)]
    [InlineData(0x8C10, 01, 24)]
    [InlineData(0x8C20, 02, 24)]
    [InlineData(0x8C30, 03, 24)]
    [InlineData(0x8C40, 04, 24)]
    [InlineData(0x8C50, 05, 24)]
    [InlineData(0x8CD0, 13, 24)]
    [InlineData(0x8CE0, 14, 24)]
    [InlineData(0x8CF0, 15, 24)]
    [InlineData(0x8D00, 16, 24)]
    [InlineData(0x8D10, 17, 24)]
    [InlineData(0x8DC0, 28, 24)]
    [InlineData(0x8DF0, 31, 24)]

    [InlineData(0x8C07, 00, 31)]
    [InlineData(0x8C17, 01, 31)]
    [InlineData(0x8C27, 02, 31)]
    [InlineData(0x8C37, 03, 31)]
    [InlineData(0x8C47, 04, 31)]
    [InlineData(0x8C57, 05, 31)]
    [InlineData(0x8CD7, 13, 31)]
    [InlineData(0x8CE7, 14, 31)]
    [InlineData(0x8CF7, 15, 31)]
    [InlineData(0x8D07, 16, 31)]
    [InlineData(0x8D17, 17, 31)]
    [InlineData(0x8DC7, 28, 31)]
    [InlineData(0x8DF7, 31, 31)]

    [InlineData(0xA001, 00, 33)]
    [InlineData(0xA011, 01, 33)]
    [InlineData(0xA021, 02, 33)]
    [InlineData(0xA031, 03, 33)]
    [InlineData(0xA041, 04, 33)]
    [InlineData(0xA051, 05, 33)]
    [InlineData(0xA0D1, 13, 33)]
    [InlineData(0xA0E1, 14, 33)]
    [InlineData(0xA0F1, 15, 33)]
    [InlineData(0xA101, 16, 33)]
    [InlineData(0xA111, 17, 33)]
    [InlineData(0xA1C1, 28, 33)]
    [InlineData(0xA1F1, 31, 33)]

    [InlineData(0xAC07, 00, 63)]
    [InlineData(0xAC17, 01, 63)]
    [InlineData(0xAC27, 02, 63)]
    [InlineData(0xAC37, 03, 63)]
    [InlineData(0xAC47, 04, 63)]
    [InlineData(0xAC57, 05, 63)]
    [InlineData(0xACD7, 13, 63)]
    [InlineData(0xACE7, 14, 63)]
    [InlineData(0xACF7, 15, 63)]
    [InlineData(0xAD07, 16, 63)]
    [InlineData(0xAD17, 17, 63)]
    [InlineData(0xADC7, 28, 63)]
    [InlineData(0xADF7, 31, 63)]
    #endregion
    public void DecodeInstruction_LDD_Z_test(ushort opcode, int d, int q)
    {
        string Mnemonics = $"LDD r{d}, Z+{q}";
        ushort address = 0xff;
        byte val = 0xcc;

        DataBus dataBus = new();
        var ram = new Ram(dataBus);
        ram.RAM[address + q] = val;
        var cpu = new CPU(dataBus, new());
        cpu.Z = address;
        cpu.PC = 100;

        var instruction = cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, cpu.r[d]);
        Assert.Equal(101, cpu.PC);
    }
    [Theory]
    #region test data
    [InlineData(0x91d1, 29)]
    [InlineData(0x91c1, 28)]
    [InlineData(0x9111, 17)]
    [InlineData(0x9101, 16)]
    [InlineData(0x90f1, 15)]
    [InlineData(0x90e1, 14)]
    [InlineData(0x9011, 1)]
    [InlineData(0x9001, 0)]
    #endregion
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
    #region test data
    [InlineData(0x91d2, 29)]
    [InlineData(0x91c2, 28)]
    [InlineData(0x9112, 17)]
    [InlineData(0x9102, 16)]
    [InlineData(0x90f2, 15)]
    [InlineData(0x90e2, 14)]
    [InlineData(0x9012, 1)]
    [InlineData(0x9002, 0)]
    #endregion
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
    #region test data
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
    #endregion
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
    #region test data
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
    #endregion
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
    #region test data
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
    #endregion
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
    #region test data
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
    #endregion
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
