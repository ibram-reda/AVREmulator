//#define RequiredHavyTest 
using AVREmulator;
using Xunit;

namespace AVREmulatorTests;
public class CPUTests
{
    private readonly CPU _cpu;
    private readonly DataBus _dataBus;
    private readonly ProgramBus _programBus;
    private readonly Ram _ram;
    private readonly FlashMemory _flashMemory;
    public CPUTests()
    {
        _dataBus = new();
        _programBus = new();
        _ram = new();
        _ram.ConnectTO(_dataBus);
        _cpu = new(_dataBus, _programBus);
        _flashMemory = new(_programBus);
    }

    [Fact]
    public void CPURegisters_Hardware_Maped_Correctly()
    {
        for (int i = 0; i < 32; i++)
        {
            Assert.Equal(byte.MinValue, _cpu.r[i]);

            var register = _cpu.GetType().GetProperty($"r{i}");
            Assert.Equal(byte.MinValue, register?.GetValue(_cpu));

            register?.SetValue(_cpu, (byte)0x62);
            Assert.Equal((byte)0x62, _cpu.r[i]);
            Assert.Equal((byte)0x62, register?.GetValue(_cpu));

        }
    }

    [Fact]
    public void DecodeInstruction_nop_test()
    {
        ushort opcode = 0;
        var pc = 52;
       
        _cpu.PC = pc;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal("NOP", instruction.Verb);
        Assert.Equal("NOP", instruction.Mnemonics);
        Assert.Equal(1, instruction.WestedCycle);

        instruction.Executable.Invoke();
        Assert.Equal(pc + 1, _cpu.PC);
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
        _cpu.r[dest] = 0xca;
        _cpu.r[source] = 0xfb;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(mnemonics, instruction.Mnemonics);
        Assert.Equal("MOV", instruction.Verb);

        instruction.Executable.Invoke();

        Assert.Equal(0xfb, _cpu.r[dest]);
        Assert.Equal(0xfb, _cpu.r[source]);

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
        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal($"LDI r{d}, 0x{k:x2}", instruction.Mnemonics, ignoreCase: true);
        Assert.Equal("LDI", instruction.Verb);
        Assert.Equal($"r{d}", instruction.Operand1);
        Assert.Equal(1, instruction.WestedCycle);

        instruction.Executable.Invoke();

        Assert.Equal(k, _cpu.r[d]); // << most important
    }

    [Theory]
    #region test data
    [InlineData(0xcfff, 0x12, -01, 0x12)]
    [InlineData(0xcffE, 0x12, -02, 0x11)]
    [InlineData(0xc002, 0x12, 02, 0x15)]
    #endregion
    public void DecodeInstruction_RJMP_test(ushort opcode, int pc, int k, int ExpectedPC)
    {
        _cpu.PC = pc;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal("RJMP", instruction.Verb);
        Assert.Equal(2, instruction.WestedCycle);
        Assert.Equal($"RJMP 0x{k:x3}", instruction.Mnemonics, ignoreCase: true);

        instruction.Executable.Invoke();

        Assert.Equal(ExpectedPC, _cpu.PC);
    }

    [Fact]
    public void DecodeInstruction_MOVW_test()
    {
        ushort opcode = 0x016E; // movw r12, r28
        _cpu.r28 = 0xf5;
        _cpu.r29 = 0x68;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);
        instruction.Executable.Invoke();

        Assert.Equal(_cpu.r28, _cpu.r12);
        Assert.Equal(_cpu.r29, _cpu.r13);
        Assert.Equal("MOVW", instruction.Verb);
        Assert.Equal(1, instruction.WestedCycle);
        Assert.Equal("MOVW r12, r28", instruction.Mnemonics, ignoreCase: true);
        Assert.Equal(101, _cpu.PC);
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
                
        _ram.RAM[address] = val;
        _cpu.X = address;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _cpu.r[d]);
        Assert.Equal(101, _cpu.PC);
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

        _ram.RAM[address] = val;
        _cpu.X = address;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _cpu.r[d]);
        Assert.Equal(address + 1, _cpu.X);

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

        _ram.RAM[address] = val;
        _cpu.X = address;
        _cpu.X++; // should be decrement to address afterr execution
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _cpu.r[d]);
        Assert.Equal(address, _cpu.X);
        Assert.Equal(101, _cpu.PC);

    }

    [Theory]
    [InlineData(0x91AD, 26)]
    [InlineData(0x91BD, 27)]
    public void DecodeInstruction_LD_X_postincrement_Throw_undifendBehaviour_on_r26(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, X+";
        ushort address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address] = val;
        _cpu.X = address;

        var instruction = _cpu.DecodeInstruction(opcode);

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

        _ram.RAM[address] = val;
        _cpu.X = address;

        var instruction = _cpu.DecodeInstruction(opcode);

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

        _ram.RAM[address] = val;
        _cpu.Y = address;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _cpu.r[d]);
        Assert.Equal(101, _cpu.PC);
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

        _ram.RAM[address +q] = val;
        _cpu.Y = address;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _cpu.r[d]);
        Assert.Equal(101, _cpu.PC);
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

        _ram.RAM[address] = val;
        _cpu.Y = address;
        _cpu.PC = 120;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _cpu.r[d]);
        Assert.Equal(address + 1, _cpu.Y);
        Assert.Equal(121, _cpu.PC);

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

        _ram.RAM[address] = val;
        _cpu.Y = (ushort)(address +1);
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _cpu.r[d]);
        Assert.Equal(address, _cpu.Y);
        Assert.Equal(101, _cpu.PC);

    }

    [Theory]
    [InlineData(0x91C9, 28)]
    [InlineData(0x91D9, 29)]
    public void DecodeInstruction_LD_Y_postincrement_Throw_undifendBehaviour_on_r28(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, Y+";
        ushort address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address] = val;
        _cpu.Y = address;

        var instruction = _cpu.DecodeInstruction(opcode);

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

        _ram.RAM[address] = val;
        _cpu.Y = address;

        var instruction = _cpu.DecodeInstruction(opcode);

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

        _ram.RAM[address] = val;
        _cpu.Z = address;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        Assert.Equal(1, instruction.WestedCycle);

        instruction.Executable.Invoke();

        Assert.Equal(val, _cpu.r[d]);
        Assert.Equal(101, _cpu.PC);
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

        _ram.RAM[address + q] = val;
        _cpu.Z = address;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _cpu.r[d]);
        Assert.Equal(101, _cpu.PC);
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

        _ram.RAM[address] = val;
        _cpu.Z = address;
        _cpu.PC = 120;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        Assert.Equal(1, instruction.WestedCycle);

        instruction.Executable.Invoke();

        Assert.Equal(val, _cpu.r[d]);
        Assert.Equal(address + 1, _cpu.Z);
        Assert.Equal(121, _cpu.PC);

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

        _ram.RAM[address] = val;
        _cpu.Z = (ushort)(address + 1);
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        Assert.Equal(2, instruction.WestedCycle);

        instruction.Executable.Invoke();

        Assert.Equal(val, _cpu.r[d]);
        Assert.Equal(address, _cpu.Z);
        Assert.Equal(101, _cpu.PC);

    }

    [Theory]
    [InlineData(0x91f1, 31)]
    [InlineData(0x91e1, 30)]
    public void DecodeInstruction_LD_Z_postincrement_Throw_undifendBehaviour_on_r28(ushort opcode, ushort d)
    {
        string Mnemonics = $"LD r{d}, Z+";
        ushort address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address] = val;
        _cpu.Z = address;

        var instruction = _cpu.DecodeInstruction(opcode);

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
        _ram.RAM[address] = val;
        _cpu.Z = address;

        var instruction = _cpu.DecodeInstruction(opcode);

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

        _flashMemory.Write(address, val);
        _cpu.Z = (ushort)(address << 1);
        _cpu.r0 = 0;


        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        instruction.Executable.Invoke();

        Assert.Equal(0xcd, _cpu.r0);

        _cpu.Z |=1 ;
        _cpu.r0 = 0;
        instruction = _cpu.DecodeInstruction(opcode);
        instruction.Executable.Invoke();
        Assert.Equal(0xef, _cpu.r0);

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
        _flashMemory.Write(address, val);
        _cpu.Z = (ushort)(address << 1);
        
        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        instruction.Executable.Invoke();

        Assert.Equal(0xcd, _cpu.r[d]);
        // try to fetch high byte
        _cpu.Z = (ushort)((address<<1)|1);
        instruction = _cpu.DecodeInstruction(opcode);
        instruction.Executable.Invoke();
        Assert.Equal(0xef, _cpu.r[d]);
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
        _flashMemory.Write(flashAddress, val);
        _cpu.Z = (ushort)zPointer;
        _cpu.PC = 400;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        Assert.Equal(3,instruction.WestedCycle);
        instruction.Executable.Invoke();

        Assert.Equal(0xcd, _cpu.r[d]);
        Assert.Equal(zPointer + 1, _cpu.Z); // check if Z incremented corectlly
        Assert.Equal(401, _cpu.PC);

        // try to fetch high byte
        // z alredy incremented
        instruction = _cpu.DecodeInstruction(opcode);
        instruction.Executable.Invoke();
        Assert.Equal(0xef, _cpu.r[d]);
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
                
        _flashMemory.Write(flashAddress, val);
        _cpu.Z = (ushort)zPointer;
        _cpu.PC = 400;

        var instruction = _cpu.DecodeInstruction(opcode);

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
                
        _ram.RAM[address] = val;
        _cpu.SP = (ushort)(address-1);
        _cpu.PC = 102;
        _cpu.r[d] = 0;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        instruction.Executable.Invoke();
        Assert.Equal(val, _cpu.r[d]);
        Assert.Equal(address, _cpu.SP);
        Assert.Equal(103, _cpu.PC);
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
    public void DecodeInstruction_LDS_test(ushort opcode, int d)
    {
        ushort k = 0x052d;
        string Mnemonics = $"LDS r{d}, 0x{k:x4}";
        byte val = 0xce;
        int flashAddress = 100;      
        _flashMemory.Write(flashAddress, opcode);
        _flashMemory.Write(flashAddress +1, k);
        _ram.Write(k, val);
        _cpu.PC = flashAddress;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics,instruction.Mnemonics);
        Assert.Equal(2,instruction.WestedCycle);
        Assert.Equal(2, instruction.Size);
        instruction.Executable.Invoke();

        Assert.Equal(val, _cpu.r[d]);
        Assert.Equal(flashAddress + 2, _cpu.PC);
    }

    [Theory]
    #region test data
    [InlineData(0x920c, 00)]
    [InlineData(0x921c, 01)]
    [InlineData(0x922c, 02)]
    [InlineData(0x923c, 03)]
    [InlineData(0x924c, 04)]
    [InlineData(0x925c, 05)]
    [InlineData(0x926c, 06)]
    [InlineData(0x927c, 07)]
    [InlineData(0x928c, 08)]
    [InlineData(0x929c, 09)]
    [InlineData(0x92ac, 10)]
    [InlineData(0x92bc, 11)]
    [InlineData(0x92cc, 12)]
    [InlineData(0x92dc, 13)]
    [InlineData(0x92ec, 14)]
    [InlineData(0x92fc, 15)]
    [InlineData(0x930c, 16)]
    [InlineData(0x931c, 17)]
    [InlineData(0x932c, 18)]
    [InlineData(0x933c, 19)]
    [InlineData(0x934c, 20)]
    [InlineData(0x935c, 21)]
    [InlineData(0x936c, 22)]
    [InlineData(0x937c, 23)]
    [InlineData(0x938c, 24)]
    [InlineData(0x939c, 25)]
    [InlineData(0x93ac, 26)]
    [InlineData(0x93bc, 27)]
    [InlineData(0x93cc, 28)]
    [InlineData(0x93dc, 29)]
    [InlineData(0x93ec, 30)]
    [InlineData(0x93fc, 31)]
    #endregion
    public void DecodeInstruction_ST_X_test(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST X, r{r}";
        ushort address = 0xcbc;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.r[r] = val;
        _cpu.X = address;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(_cpu.r[r], _ram.RAM[address]);
        Assert.Equal(101, _cpu.PC);
    }

    [Theory]
    #region test data
    [InlineData(0x920d, 00)]
    [InlineData(0x921d, 01)]
    [InlineData(0x922d, 02)]
    [InlineData(0x923d, 03)]
    [InlineData(0x924d, 04)]
    [InlineData(0x925d, 05)]
    [InlineData(0x926d, 06)]
    [InlineData(0x927d, 07)]
    [InlineData(0x928d, 08)]
    [InlineData(0x929d, 09)]
    [InlineData(0x92ad, 10)]
    [InlineData(0x92bd, 11)]
    [InlineData(0x92cd, 12)]
    [InlineData(0x92dd, 13)]
    [InlineData(0x92ed, 14)]
    [InlineData(0x92fd, 15)]
    [InlineData(0x930d, 16)]
    [InlineData(0x931d, 17)]
    [InlineData(0x932d, 18)]
    [InlineData(0x933d, 19)]
    [InlineData(0x934d, 20)]
    [InlineData(0x935d, 21)]
    [InlineData(0x936d, 22)]
    [InlineData(0x937d, 23)]
    [InlineData(0x938d, 24)]
    [InlineData(0x939d, 25)]
    [InlineData(0x93cd, 28)]
    [InlineData(0x93dd, 29)]
    [InlineData(0x93ed, 30)]
    [InlineData(0x93fd, 31)]
    #endregion
    public void DecodeInstruction_ST_X_postincrement_test(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST X+, r{r}";
        ushort address = 0xcbc;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.r[r] = val;
        _cpu.X = address;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _ram.RAM[address]);
        Assert.Equal(address + 1, _cpu.X);
        Assert.Equal(101, _cpu.PC);
    }

    [Theory]
    [InlineData(0x93ad, 26)]
    [InlineData(0x93bd, 27)]
    public void DecodeInstruction_ST_X_postincrement_throw_UndifiendBehavior(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST X+, r{r}";
        ushort address = 0xcbc;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.r[r] = val;
        _cpu.X = address;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        Assert.Throws<UndifiendBehaviorException>(instruction.Executable);
    }

    [Theory]
    #region test data
    [InlineData(0x920e, 00)]
    [InlineData(0x921e, 01)]
    [InlineData(0x922e, 02)]
    [InlineData(0x923e, 03)]
    [InlineData(0x924e, 04)]
    [InlineData(0x925e, 05)]
    [InlineData(0x926e, 06)]
    [InlineData(0x927e, 07)]
    [InlineData(0x928e, 08)]
    [InlineData(0x929e, 09)]
    [InlineData(0x92ae, 10)]
    [InlineData(0x92be, 11)]
    [InlineData(0x92ce, 12)]
    [InlineData(0x92de, 13)]
    [InlineData(0x92ee, 14)]
    [InlineData(0x92fe, 15)]
    [InlineData(0x930e, 16)]
    [InlineData(0x931e, 17)]
    [InlineData(0x932e, 18)]
    [InlineData(0x933e, 19)]
    [InlineData(0x934e, 20)]
    [InlineData(0x935e, 21)]
    [InlineData(0x936e, 22)]
    [InlineData(0x937e, 23)]
    [InlineData(0x938e, 24)]
    [InlineData(0x939e, 25)]
    [InlineData(0x93ce, 28)]
    [InlineData(0x93de, 29)]
    [InlineData(0x93ee, 30)]
    [InlineData(0x93fe, 31)]
    #endregion
    public void DecodeInstruction_ST_X_PreDecrement_test(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST -X, r{r}";
        int address = 0xcbc;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.r[r] = val;
        _cpu.X = (ushort)(address + 1);
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _ram.RAM[address]);
        Assert.Equal(address, _cpu.X);
        Assert.Equal(101, _cpu.PC);
    }

    [Theory]
    [InlineData(0x93ae, 26)]
    [InlineData(0x93be, 27)]
    public void DecodeInstruction_ST_X_PreDecrement_throw_UndifiendBehavior(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST -X, r{r}";
        int address = 0xcbc;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.r[r] = val;
        _cpu.X = (ushort)(address + 1);
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        Assert.Throws<UndifiendBehaviorException>(instruction.Executable);
    }

    [Theory]
    #region test data
    [InlineData(0x920f, 00)]
    [InlineData(0x921f, 01)]
    [InlineData(0x922f, 02)]
    [InlineData(0x923f, 03)]
    [InlineData(0x924f, 04)]
    [InlineData(0x925f, 05)]
    [InlineData(0x926f, 06)]
    [InlineData(0x927f, 07)]
    [InlineData(0x928f, 08)]
    [InlineData(0x929f, 09)]
    [InlineData(0x92af, 10)]
    [InlineData(0x92bf, 11)]
    [InlineData(0x92cf, 12)]
    [InlineData(0x92df, 13)]
    [InlineData(0x92ef, 14)]
    [InlineData(0x92ff, 15)]
    [InlineData(0x930f, 16)]
    [InlineData(0x931f, 17)]
    [InlineData(0x932f, 18)]
    [InlineData(0x933f, 19)]
    [InlineData(0x934f, 20)]
    [InlineData(0x935f, 21)]
    [InlineData(0x936f, 22)]
    [InlineData(0x937f, 23)]
    [InlineData(0x938f, 24)]
    [InlineData(0x939f, 25)]
    [InlineData(0x93af, 26)]
    [InlineData(0x93bf, 27)]
    [InlineData(0x93cf, 28)]
    [InlineData(0x93df, 29)]
    [InlineData(0x93ef, 30)]
    [InlineData(0x93ff, 31)]
    #endregion
    public void DecodeInstruction_PUSH_test(ushort opcode, ushort r)
    {
        string Mnemonics = $"PUSH r{r}";
        int address = 100;
        byte val = 0xcc;
        _cpu.SP = (ushort)address ;
        _cpu.r[r] = val;
        _ram.RAM[address] = 0; 
        _cpu.PC = 102;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        
        instruction.Executable.Invoke();

        Assert.Equal(val, _ram.RAM[address]);
        Assert.Equal(address - 1, _cpu.SP);
        Assert.Equal(103, _cpu.PC);
    }

    [Theory]
    #region test data
    [InlineData(0x9209, 00)]
    [InlineData(0x9219, 01)]
    [InlineData(0x9229, 02)]
    [InlineData(0x9239, 03)]
    [InlineData(0x9249, 04)]
    [InlineData(0x9259, 05)]
    [InlineData(0x9269, 06)]
    [InlineData(0x9279, 07)]
    [InlineData(0x9289, 08)]
    [InlineData(0x9299, 09)]
    [InlineData(0x92a9, 10)]
    [InlineData(0x92b9, 11)]
    [InlineData(0x92c9, 12)]
    [InlineData(0x92d9, 13)]
    [InlineData(0x92e9, 14)]
    [InlineData(0x92f9, 15)]
    [InlineData(0x9309, 16)]
    [InlineData(0x9319, 17)]
    [InlineData(0x9329, 18)]
    [InlineData(0x9339, 19)]
    [InlineData(0x9349, 20)]
    [InlineData(0x9359, 21)]
    [InlineData(0x9369, 22)]
    [InlineData(0x9379, 23)]
    [InlineData(0x9389, 24)]
    [InlineData(0x9399, 25)]
    [InlineData(0x93a9, 26)]
    [InlineData(0x93b9, 27)]
    [InlineData(0x93e9, 30)]
    [InlineData(0x93f9, 31)]
    #endregion
    public void DecodeInstruction_ST_Y_postincrement_test(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST Y+, r{r}";
        ushort address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.Y = address;
        _cpu.r[r] = val;
        _cpu.PC = 120;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _ram.RAM[address]);
        Assert.Equal(address + 1, _cpu.Y);
        Assert.Equal(121, _cpu.PC);

    }

    [Theory]
    [InlineData(0x93c9, 28)]
    [InlineData(0x93d9, 29)]
    public void DecodeInstruction_ST_Y_postincrement_Throw_UndifiendBehavior(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST Y+, r{r}";
        ushort address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.Y = address;
        _cpu.r[r] = val;
        _cpu.PC = 120;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        Assert.Throws<UndifiendBehaviorException>(instruction.Executable);
    }

    [Theory]
    #region test data
    [InlineData(0x920A, 00)]
    [InlineData(0x921A, 01)]
    [InlineData(0x922A, 02)]
    [InlineData(0x923A, 03)]
    [InlineData(0x924A, 04)]
    [InlineData(0x925A, 05)]
    [InlineData(0x926A, 06)]
    [InlineData(0x927A, 07)]
    [InlineData(0x928A, 08)]
    [InlineData(0x929A, 09)]
    [InlineData(0x92aA, 10)]
    [InlineData(0x92bA, 11)]
    [InlineData(0x92cA, 12)]
    [InlineData(0x92dA, 13)]
    [InlineData(0x92eA, 14)]
    [InlineData(0x92fA, 15)]
    [InlineData(0x930A, 16)]
    [InlineData(0x931A, 17)]
    [InlineData(0x932A, 18)]
    [InlineData(0x933A, 19)]
    [InlineData(0x934A, 20)]
    [InlineData(0x935A, 21)]
    [InlineData(0x936A, 22)]
    [InlineData(0x937A, 23)]
    [InlineData(0x938A, 24)]
    [InlineData(0x939A, 25)]
    [InlineData(0x93aA, 26)]
    [InlineData(0x93bA, 27)]
    [InlineData(0x93eA, 30)]
    [InlineData(0x93fA, 31)]
    #endregion
    public void DecodeInstruction_ST_Y_preDecremant_test(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST -Y, r{r}";
        int address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.Y = (ushort)(address + 1);
        _cpu.r[r] = val;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _ram.RAM[address]);
        Assert.Equal(address, _cpu.Y);
        Assert.Equal(101, _cpu.PC);

    }

    [Theory]
    [InlineData(0x93CA, 28)]
    [InlineData(0x93DA, 29)]
    public void DecodeInstruction_ST_Y_preDecremant_Throw_undifendBehavior(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST -Y, r{r}";
        int address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.Y = (ushort)(address + 1);
        _cpu.r[r] = val;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        Assert.Throws<UndifiendBehaviorException>(instruction.Executable);
    }

    [Theory]
    #region test data
    [InlineData(0x8208, 00)]
    [InlineData(0x8218, 01)]
    [InlineData(0x8228, 02)]
    [InlineData(0x8238, 03)]
    [InlineData(0x8248, 04)]
    [InlineData(0x8258, 05)]
    [InlineData(0x8268, 06)]
    [InlineData(0x8278, 07)]
    [InlineData(0x8288, 08)]
    [InlineData(0x8298, 09)]
    [InlineData(0x82a8, 10)]
    [InlineData(0x82b8, 11)]
    [InlineData(0x82c8, 12)]
    [InlineData(0x82d8, 13)]
    [InlineData(0x82e8, 14)]
    [InlineData(0x82f8, 15)]
    [InlineData(0x8308, 16)]
    [InlineData(0x8318, 17)]
    [InlineData(0x8328, 18)]
    [InlineData(0x8338, 19)]
    [InlineData(0x8348, 20)]
    [InlineData(0x8358, 21)]
    [InlineData(0x8368, 22)]
    [InlineData(0x8378, 23)]
    [InlineData(0x8388, 24)]
    [InlineData(0x8398, 25)]
    [InlineData(0x83a8, 26)]
    [InlineData(0x83b8, 27)]
    [InlineData(0x83e8, 30)]
    [InlineData(0x83f8, 31)]
    #endregion

    public void DecodeInstruction_ST_Y_test(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST Y, r{r}";
        ushort address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.r[r] = val;
        _cpu.Y = address;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _ram.RAM[address]);
        Assert.Equal(101, _cpu.PC);
    }

    [Theory]
    #region test data
    [InlineData(0x8200, 00)]
    [InlineData(0x8210, 01)]
    [InlineData(0x8220, 02)]
    [InlineData(0x8230, 03)]
    [InlineData(0x8240, 04)]
    [InlineData(0x8250, 05)]
    [InlineData(0x8260, 06)]
    [InlineData(0x8270, 07)]
    [InlineData(0x8280, 08)]
    [InlineData(0x8290, 09)]
    [InlineData(0x82a0, 10)]
    [InlineData(0x82b0, 11)]
    [InlineData(0x82c0, 12)]
    [InlineData(0x82d0, 13)]
    [InlineData(0x82e0, 14)]
    [InlineData(0x82f0, 15)]
    [InlineData(0x8300, 16)]
    [InlineData(0x8310, 17)]
    [InlineData(0x8320, 18)]
    [InlineData(0x8330, 19)]
    [InlineData(0x8340, 20)]
    [InlineData(0x8350, 21)]
    [InlineData(0x8360, 22)]
    [InlineData(0x8370, 23)]
    [InlineData(0x8380, 24)]
    [InlineData(0x8390, 25)]
    [InlineData(0x83a0, 26)]
    [InlineData(0x83b0, 27)]
    [InlineData(0x83c0, 28)]
    [InlineData(0x83d0, 29)]
    #endregion
    public void DecodeInstruction_ST_Z_test(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST Z, r{r}";
        ushort address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.r[r] = val;
        _cpu.Z = address;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _ram.RAM[address]);
        Assert.Equal(101, _cpu.PC);
    }

    [Theory]
    #region test data
    [InlineData(0x9201, 00)]
    [InlineData(0x9211, 01)]
    [InlineData(0x9221, 02)]
    [InlineData(0x9231, 03)]
    [InlineData(0x9241, 04)]
    [InlineData(0x9251, 05)]
    [InlineData(0x9261, 06)]
    [InlineData(0x9271, 07)]
    [InlineData(0x9281, 08)]
    [InlineData(0x9291, 09)]
    [InlineData(0x92a1, 10)]
    [InlineData(0x92b1, 11)]
    [InlineData(0x92c1, 12)]
    [InlineData(0x92d1, 13)]
    [InlineData(0x92e1, 14)]
    [InlineData(0x92f1, 15)]
    [InlineData(0x9301, 16)]
    [InlineData(0x9311, 17)]
    [InlineData(0x9321, 18)]
    [InlineData(0x9331, 19)]
    [InlineData(0x9341, 20)]
    [InlineData(0x9351, 21)]
    [InlineData(0x9361, 22)]
    [InlineData(0x9371, 23)]
    [InlineData(0x9381, 24)]
    [InlineData(0x9391, 25)]
    [InlineData(0x93a1, 26)]
    [InlineData(0x93b1, 27)]
    [InlineData(0x93c1, 28)]
    [InlineData(0x93d1, 29)]
    #endregion
    public void DecodeInstruction_ST_Z_postincrement_test(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST Z+, r{r}";
        ushort address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.Z = address;
        _cpu.r[r] = val;
        _cpu.PC = 120;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _ram.RAM[address]);
        Assert.Equal(address + 1, _cpu.Z);
        Assert.Equal(121, _cpu.PC);

    }

    [Theory]
    [InlineData(0x93e1, 30)]
    [InlineData(0x93f1, 31)]
    public void DecodeInstruction_ST_Z_postincrement_Throw_UndifiendBehavior(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST Z+, r{r}";
        ushort address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.Z = address;
        _cpu.r[r] = val;
        _cpu.PC = 120;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        Assert.Throws<UndifiendBehaviorException>(instruction.Executable);
    }

    [Theory]
    #region test data
    [InlineData(0x9202, 00)]
    [InlineData(0x9212, 01)]
    [InlineData(0x9222, 02)]
    [InlineData(0x9232, 03)]
    [InlineData(0x9242, 04)]
    [InlineData(0x9252, 05)]
    [InlineData(0x9262, 06)]
    [InlineData(0x9272, 07)]
    [InlineData(0x9282, 08)]
    [InlineData(0x9292, 09)]
    [InlineData(0x92a2, 10)]
    [InlineData(0x92b2, 11)]
    [InlineData(0x92c2, 12)]
    [InlineData(0x92d2, 13)]
    [InlineData(0x92e2, 14)]
    [InlineData(0x92f2, 15)]
    [InlineData(0x9302, 16)]
    [InlineData(0x9312, 17)]
    [InlineData(0x9322, 18)]
    [InlineData(0x9332, 19)]
    [InlineData(0x9342, 20)]
    [InlineData(0x9352, 21)]
    [InlineData(0x9362, 22)]
    [InlineData(0x9372, 23)]
    [InlineData(0x9382, 24)]
    [InlineData(0x9392, 25)]
    [InlineData(0x93a2, 26)]
    [InlineData(0x93b2, 27)]
    [InlineData(0x93c2, 28)]
    [InlineData(0x93d2, 29)]
    #endregion
    public void DecodeInstruction_ST_Z_preDecremant_test(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST -Z, r{r}";
        int address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.Z = (ushort)(address + 1);
        _cpu.r[r] = val;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _ram.RAM[address]);
        Assert.Equal(address, _cpu.Z);
        Assert.Equal(101, _cpu.PC);

    }

    [Theory]
    [InlineData(0x93e2, 30)]
    [InlineData(0x93f2, 31)]
    public void DecodeInstruction_ST_Z_preDecremant_Throw_undifendBehavior(ushort opcode, ushort r)
    {
        string Mnemonics = $"ST -Z, r{r}";
        int address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address] = 0;
        _cpu.Z = (ushort)(address + 1);
        _cpu.r[r] = val;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        Assert.Throws<UndifiendBehaviorException>(instruction.Executable);
    }

    [Theory]
    #region test data
    [InlineData(0x8209, 00, 01)]
    [InlineData(0x8219, 01, 01)]
    [InlineData(0x8229, 02, 01)]
    [InlineData(0x8239, 03, 01)]
    [InlineData(0x8249, 04, 01)]
    [InlineData(0x8259, 05, 01)]
    [InlineData(0x8269, 06, 01)]
    [InlineData(0x8279, 07, 01)]
    [InlineData(0x8289, 08, 01)]
    [InlineData(0x8299, 09, 01)]
    [InlineData(0x82a9, 10, 01)]
    [InlineData(0x82b9, 11, 01)]
    [InlineData(0x82c9, 12, 01)]
    [InlineData(0x82d9, 13, 01)]
    [InlineData(0x82e9, 14, 01)]
    [InlineData(0x82f9, 15, 01)]
    [InlineData(0x8309, 16, 01)]
    [InlineData(0x8319, 17, 01)]
    [InlineData(0x8329, 18, 01)]
    [InlineData(0x8339, 19, 01)]
    [InlineData(0x8349, 20, 01)]
    [InlineData(0x8359, 21, 01)]
    [InlineData(0x8369, 22, 01)]
    [InlineData(0x8379, 23, 01)]
    [InlineData(0x8389, 24, 01)]
    [InlineData(0x8399, 25, 01)]
    [InlineData(0x83a9, 26, 01)]
    [InlineData(0x83b9, 27, 01)]
    [InlineData(0x83e9, 30, 01)]
    [InlineData(0x83f9, 31, 01)]
#if RequiredHavyTest
    [InlineData(0x820a, 00, 02)]
    [InlineData(0x821a, 01, 02)]
    [InlineData(0x822a, 02, 02)]
    [InlineData(0x823a, 03, 02)]
    [InlineData(0x824a, 04, 02)]
    [InlineData(0x825a, 05, 02)]
    [InlineData(0x826a, 06, 02)]
    [InlineData(0x827a, 07, 02)]
    [InlineData(0x828a, 08, 02)]
    [InlineData(0x829a, 09, 02)]
    [InlineData(0x82aa, 10, 02)]
    [InlineData(0x82ba, 11, 02)]
    [InlineData(0x82ca, 12, 02)]
    [InlineData(0x82da, 13, 02)]
    [InlineData(0x82ea, 14, 02)]
    [InlineData(0x82fa, 15, 02)]
    [InlineData(0x830a, 16, 02)]
    [InlineData(0x831a, 17, 02)]
    [InlineData(0x832a, 18, 02)]
    [InlineData(0x833a, 19, 02)]
    [InlineData(0x834a, 20, 02)]
    [InlineData(0x835a, 21, 02)]
    [InlineData(0x836a, 22, 02)]
    [InlineData(0x837a, 23, 02)]
    [InlineData(0x838a, 24, 02)]
    [InlineData(0x839a, 25, 02)]
    [InlineData(0x83aa, 26, 02)]
    [InlineData(0x83ba, 27, 02)]
    [InlineData(0x83ea, 30, 02)]
    [InlineData(0x83fa, 31, 02)]

    [InlineData(0x820b, 00, 03)]
    [InlineData(0x821b, 01, 03)]
    [InlineData(0x822b, 02, 03)]
    [InlineData(0x823b, 03, 03)]
    [InlineData(0x824b, 04, 03)]
    [InlineData(0x825b, 05, 03)]
    [InlineData(0x826b, 06, 03)]
    [InlineData(0x827b, 07, 03)]
    [InlineData(0x828b, 08, 03)]
    [InlineData(0x829b, 09, 03)]
    [InlineData(0x82ab, 10, 03)]
    [InlineData(0x82bb, 11, 03)]
    [InlineData(0x82cb, 12, 03)]
    [InlineData(0x82db, 13, 03)]
    [InlineData(0x82eb, 14, 03)]
    [InlineData(0x82fb, 15, 03)]
    [InlineData(0x830b, 16, 03)]
    [InlineData(0x831b, 17, 03)]
    [InlineData(0x832b, 18, 03)]
    [InlineData(0x833b, 19, 03)]
    [InlineData(0x834b, 20, 03)]
    [InlineData(0x835b, 21, 03)]
    [InlineData(0x836b, 22, 03)]
    [InlineData(0x837b, 23, 03)]
    [InlineData(0x838b, 24, 03)]
    [InlineData(0x839b, 25, 03)]
    [InlineData(0x83ab, 26, 03)]
    [InlineData(0x83bb, 27, 03)]
    [InlineData(0x83eb, 30, 03)]
    [InlineData(0x83fb, 31, 03)]

    [InlineData(0x820c, 00, 04)]
    [InlineData(0x821c, 01, 04)]
    [InlineData(0x822c, 02, 04)]
    [InlineData(0x823c, 03, 04)]
    [InlineData(0x824c, 04, 04)]
    [InlineData(0x825c, 05, 04)]
    [InlineData(0x826c, 06, 04)]
    [InlineData(0x827c, 07, 04)]
    [InlineData(0x828c, 08, 04)]
    [InlineData(0x829c, 09, 04)]
    [InlineData(0x82ac, 10, 04)]
    [InlineData(0x82bc, 11, 04)]
    [InlineData(0x82cc, 12, 04)]
    [InlineData(0x82dc, 13, 04)]
    [InlineData(0x82ec, 14, 04)]
    [InlineData(0x82fc, 15, 04)]
    [InlineData(0x830c, 16, 04)]
    [InlineData(0x831c, 17, 04)]
    [InlineData(0x832c, 18, 04)]
    [InlineData(0x833c, 19, 04)]
    [InlineData(0x834c, 20, 04)]
    [InlineData(0x835c, 21, 04)]
    [InlineData(0x836c, 22, 04)]
    [InlineData(0x837c, 23, 04)]
    [InlineData(0x838c, 24, 04)]
    [InlineData(0x839c, 25, 04)]
    [InlineData(0x83ac, 26, 04)]
    [InlineData(0x83bc, 27, 04)]
    [InlineData(0x83ec, 30, 04)]
    [InlineData(0x83fc, 31, 04)]

    [InlineData(0x820d, 00, 05)]
    [InlineData(0x821d, 01, 05)]
    [InlineData(0x822d, 02, 05)]
    [InlineData(0x823d, 03, 05)]
    [InlineData(0x824d, 04, 05)]
    [InlineData(0x825d, 05, 05)]
    [InlineData(0x826d, 06, 05)]
    [InlineData(0x827d, 07, 05)]
    [InlineData(0x828d, 08, 05)]
    [InlineData(0x829d, 09, 05)]
    [InlineData(0x82ad, 10, 05)]
    [InlineData(0x82bd, 11, 05)]
    [InlineData(0x82cd, 12, 05)]
    [InlineData(0x82dd, 13, 05)]
    [InlineData(0x82ed, 14, 05)]
    [InlineData(0x82fd, 15, 05)]
    [InlineData(0x830d, 16, 05)]
    [InlineData(0x831d, 17, 05)]
    [InlineData(0x832d, 18, 05)]
    [InlineData(0x833d, 19, 05)]
    [InlineData(0x834d, 20, 05)]
    [InlineData(0x835d, 21, 05)]
    [InlineData(0x836d, 22, 05)]
    [InlineData(0x837d, 23, 05)]
    [InlineData(0x838d, 24, 05)]
    [InlineData(0x839d, 25, 05)]
    [InlineData(0x83ad, 26, 05)]
    [InlineData(0x83bd, 27, 05)]
    [InlineData(0x83ed, 30, 05)]
    [InlineData(0x83fd, 31, 05)]

    [InlineData(0x820e, 00, 06)]
    [InlineData(0x821e, 01, 06)]
    [InlineData(0x822e, 02, 06)]
    [InlineData(0x823e, 03, 06)]
    [InlineData(0x824e, 04, 06)]
    [InlineData(0x825e, 05, 06)]
    [InlineData(0x826e, 06, 06)]
    [InlineData(0x827e, 07, 06)]
    [InlineData(0x828e, 08, 06)]
    [InlineData(0x829e, 09, 06)]
    [InlineData(0x82ae, 10, 06)]
    [InlineData(0x82be, 11, 06)]
    [InlineData(0x82ce, 12, 06)]
    [InlineData(0x82de, 13, 06)]
    [InlineData(0x82ee, 14, 06)]
    [InlineData(0x82fe, 15, 06)]
    [InlineData(0x830e, 16, 06)]
    [InlineData(0x831e, 17, 06)]
    [InlineData(0x832e, 18, 06)]
    [InlineData(0x833e, 19, 06)]
    [InlineData(0x834e, 20, 06)]
    [InlineData(0x835e, 21, 06)]
    [InlineData(0x836e, 22, 06)]
    [InlineData(0x837e, 23, 06)]
    [InlineData(0x838e, 24, 06)]
    [InlineData(0x839e, 25, 06)]
    [InlineData(0x83ae, 26, 06)]
    [InlineData(0x83be, 27, 06)]
    [InlineData(0x83ee, 30, 06)]
    [InlineData(0x83fe, 31, 06)]

    [InlineData(0x820f, 00, 07)]
    [InlineData(0x821f, 01, 07)]
    [InlineData(0x822f, 02, 07)]
    [InlineData(0x823f, 03, 07)]
    [InlineData(0x824f, 04, 07)]
    [InlineData(0x825f, 05, 07)]
    [InlineData(0x826f, 06, 07)]
    [InlineData(0x827f, 07, 07)]
    [InlineData(0x828f, 08, 07)]
    [InlineData(0x829f, 09, 07)]
    [InlineData(0x82af, 10, 07)]
    [InlineData(0x82bf, 11, 07)]
    [InlineData(0x82cf, 12, 07)]
    [InlineData(0x82df, 13, 07)]
    [InlineData(0x82ef, 14, 07)]
    [InlineData(0x82ff, 15, 07)]
    [InlineData(0x830f, 16, 07)]
    [InlineData(0x831f, 17, 07)]
    [InlineData(0x832f, 18, 07)]
    [InlineData(0x833f, 19, 07)]
    [InlineData(0x834f, 20, 07)]
    [InlineData(0x835f, 21, 07)]
    [InlineData(0x836f, 22, 07)]
    [InlineData(0x837f, 23, 07)]
    [InlineData(0x838f, 24, 07)]
    [InlineData(0x839f, 25, 07)]
    [InlineData(0x83af, 26, 07)]
    [InlineData(0x83bf, 27, 07)]
    [InlineData(0x83ef, 30, 07)]
    [InlineData(0x83ff, 31, 07)]

    [InlineData(0x8608, 00, 08)]
    [InlineData(0x8618, 01, 08)]
    [InlineData(0x8628, 02, 08)]
    [InlineData(0x8638, 03, 08)]
    [InlineData(0x8648, 04, 08)]
    [InlineData(0x8658, 05, 08)]
    [InlineData(0x8668, 06, 08)]
    [InlineData(0x8678, 07, 08)]
    [InlineData(0x8688, 08, 08)]
    [InlineData(0x8698, 09, 08)]
    [InlineData(0x86a8, 10, 08)]
    [InlineData(0x86b8, 11, 08)]
    [InlineData(0x86c8, 12, 08)]
    [InlineData(0x86d8, 13, 08)]
    [InlineData(0x86e8, 14, 08)]
    [InlineData(0x86f8, 15, 08)]
    [InlineData(0x8708, 16, 08)]
    [InlineData(0x8718, 17, 08)]
    [InlineData(0x8728, 18, 08)]
    [InlineData(0x8738, 19, 08)]
    [InlineData(0x8748, 20, 08)]
    [InlineData(0x8758, 21, 08)]
    [InlineData(0x8768, 22, 08)]
    [InlineData(0x8778, 23, 08)]
    [InlineData(0x8788, 24, 08)]
    [InlineData(0x8798, 25, 08)]
    [InlineData(0x87a8, 26, 08)]
    [InlineData(0x87b8, 27, 08)]
    [InlineData(0x87e8, 30, 08)]
    [InlineData(0x87f8, 31, 08)]

    [InlineData(0x8609, 00, 09)]
    [InlineData(0x8619, 01, 09)]
    [InlineData(0x8629, 02, 09)]
    [InlineData(0x8639, 03, 09)]
    [InlineData(0x8649, 04, 09)]
    [InlineData(0x8659, 05, 09)]
    [InlineData(0x8669, 06, 09)]
    [InlineData(0x8679, 07, 09)]
    [InlineData(0x8689, 08, 09)]
    [InlineData(0x8699, 09, 09)]
    [InlineData(0x86a9, 10, 09)]
    [InlineData(0x86b9, 11, 09)]
    [InlineData(0x86c9, 12, 09)]
    [InlineData(0x86d9, 13, 09)]
    [InlineData(0x86e9, 14, 09)]
    [InlineData(0x86f9, 15, 09)]
    [InlineData(0x8709, 16, 09)]
    [InlineData(0x8719, 17, 09)]
    [InlineData(0x8729, 18, 09)]
    [InlineData(0x8739, 19, 09)]
    [InlineData(0x8749, 20, 09)]
    [InlineData(0x8759, 21, 09)]
    [InlineData(0x8769, 22, 09)]
    [InlineData(0x8779, 23, 09)]
    [InlineData(0x8789, 24, 09)]
    [InlineData(0x8799, 25, 09)]
    [InlineData(0x87a9, 26, 09)]
    [InlineData(0x87b9, 27, 09)]
    [InlineData(0x87e9, 30, 09)]
    [InlineData(0x87f9, 31, 09)]

    [InlineData(0x860a, 00, 10)]
    [InlineData(0x861a, 01, 10)]
    [InlineData(0x862a, 02, 10)]
    [InlineData(0x863a, 03, 10)]
    [InlineData(0x864a, 04, 10)]
    [InlineData(0x865a, 05, 10)]
    [InlineData(0x866a, 06, 10)]
    [InlineData(0x867a, 07, 10)]
    [InlineData(0x868a, 08, 10)]
    [InlineData(0x869a, 09, 10)]
    [InlineData(0x86aa, 10, 10)]
    [InlineData(0x86ba, 11, 10)]
    [InlineData(0x86ca, 12, 10)]
    [InlineData(0x86da, 13, 10)]
    [InlineData(0x86ea, 14, 10)]
    [InlineData(0x86fa, 15, 10)]
    [InlineData(0x870a, 16, 10)]
    [InlineData(0x871a, 17, 10)]
    [InlineData(0x872a, 18, 10)]
    [InlineData(0x873a, 19, 10)]
    [InlineData(0x874a, 20, 10)]
    [InlineData(0x875a, 21, 10)]
    [InlineData(0x876a, 22, 10)]
    [InlineData(0x877a, 23, 10)]
    [InlineData(0x878a, 24, 10)]
    [InlineData(0x879a, 25, 10)]
    [InlineData(0x87aa, 26, 10)]
    [InlineData(0x87ba, 27, 10)]
    [InlineData(0x87ea, 30, 10)]
    [InlineData(0x87fa, 31, 10)]

    [InlineData(0x860b, 00, 11)]
    [InlineData(0x861b, 01, 11)]
    [InlineData(0x862b, 02, 11)]
    [InlineData(0x863b, 03, 11)]
    [InlineData(0x864b, 04, 11)]
    [InlineData(0x865b, 05, 11)]
    [InlineData(0x866b, 06, 11)]
    [InlineData(0x867b, 07, 11)]
    [InlineData(0x868b, 08, 11)]
    [InlineData(0x869b, 09, 11)]
    [InlineData(0x86ab, 10, 11)]
    [InlineData(0x86bb, 11, 11)]
    [InlineData(0x86cb, 12, 11)]
    [InlineData(0x86db, 13, 11)]
    [InlineData(0x86eb, 14, 11)]
    [InlineData(0x86fb, 15, 11)]
    [InlineData(0x870b, 16, 11)]
    [InlineData(0x871b, 17, 11)]
    [InlineData(0x872b, 18, 11)]
    [InlineData(0x873b, 19, 11)]
    [InlineData(0x874b, 20, 11)]
    [InlineData(0x875b, 21, 11)]
    [InlineData(0x876b, 22, 11)]
    [InlineData(0x877b, 23, 11)]
    [InlineData(0x878b, 24, 11)]
    [InlineData(0x879b, 25, 11)]
    [InlineData(0x87ab, 26, 11)]
    [InlineData(0x87bb, 27, 11)]
    [InlineData(0x87eb, 30, 11)]
    [InlineData(0x87fb, 31, 11)]

    [InlineData(0x860c, 00, 12)]
    [InlineData(0x861c, 01, 12)]
    [InlineData(0x862c, 02, 12)]
    [InlineData(0x863c, 03, 12)]
    [InlineData(0x864c, 04, 12)]
    [InlineData(0x865c, 05, 12)]
    [InlineData(0x866c, 06, 12)]
    [InlineData(0x867c, 07, 12)]
    [InlineData(0x868c, 08, 12)]
    [InlineData(0x869c, 09, 12)]
    [InlineData(0x86ac, 10, 12)]
    [InlineData(0x86bc, 11, 12)]
    [InlineData(0x86cc, 12, 12)]
    [InlineData(0x86dc, 13, 12)]
    [InlineData(0x86ec, 14, 12)]
    [InlineData(0x86fc, 15, 12)]
    [InlineData(0x870c, 16, 12)]
    [InlineData(0x871c, 17, 12)]
    [InlineData(0x872c, 18, 12)]
    [InlineData(0x873c, 19, 12)]
    [InlineData(0x874c, 20, 12)]
    [InlineData(0x875c, 21, 12)]
    [InlineData(0x876c, 22, 12)]
    [InlineData(0x877c, 23, 12)]
    [InlineData(0x878c, 24, 12)]
    [InlineData(0x879c, 25, 12)]
    [InlineData(0x87ac, 26, 12)]
    [InlineData(0x87bc, 27, 12)]
    [InlineData(0x87ec, 30, 12)]
    [InlineData(0x87fc, 31, 12)]

    [InlineData(0x860d, 00, 13)]
    [InlineData(0x861d, 01, 13)]
    [InlineData(0x862d, 02, 13)]
    [InlineData(0x863d, 03, 13)]
    [InlineData(0x864d, 04, 13)]
    [InlineData(0x865d, 05, 13)]
    [InlineData(0x866d, 06, 13)]
    [InlineData(0x867d, 07, 13)]
    [InlineData(0x868d, 08, 13)]
    [InlineData(0x869d, 09, 13)]
    [InlineData(0x86ad, 10, 13)]
    [InlineData(0x86bd, 11, 13)]
    [InlineData(0x86cd, 12, 13)]
    [InlineData(0x86dd, 13, 13)]
    [InlineData(0x86ed, 14, 13)]
    [InlineData(0x86fd, 15, 13)]
    [InlineData(0x870d, 16, 13)]
    [InlineData(0x871d, 17, 13)]
    [InlineData(0x872d, 18, 13)]
    [InlineData(0x873d, 19, 13)]
    [InlineData(0x874d, 20, 13)]
    [InlineData(0x875d, 21, 13)]
    [InlineData(0x876d, 22, 13)]
    [InlineData(0x877d, 23, 13)]
    [InlineData(0x878d, 24, 13)]
    [InlineData(0x879d, 25, 13)]
    [InlineData(0x87ad, 26, 13)]
    [InlineData(0x87bd, 27, 13)]
    [InlineData(0x87ed, 30, 13)]
    [InlineData(0x87fd, 31, 13)]

    [InlineData(0x860e, 00, 14)]
    [InlineData(0x861e, 01, 14)]
    [InlineData(0x862e, 02, 14)]
    [InlineData(0x863e, 03, 14)]
    [InlineData(0x864e, 04, 14)]
    [InlineData(0x865e, 05, 14)]
    [InlineData(0x866e, 06, 14)]
    [InlineData(0x867e, 07, 14)]
    [InlineData(0x868e, 08, 14)]
    [InlineData(0x869e, 09, 14)]
    [InlineData(0x86ae, 10, 14)]
    [InlineData(0x86be, 11, 14)]
    [InlineData(0x86ce, 12, 14)]
    [InlineData(0x86de, 13, 14)]
    [InlineData(0x86ee, 14, 14)]
    [InlineData(0x86fe, 15, 14)]
    [InlineData(0x870e, 16, 14)]
    [InlineData(0x871e, 17, 14)]
    [InlineData(0x872e, 18, 14)]
    [InlineData(0x873e, 19, 14)]
    [InlineData(0x874e, 20, 14)]
    [InlineData(0x875e, 21, 14)]
    [InlineData(0x876e, 22, 14)]
    [InlineData(0x877e, 23, 14)]
    [InlineData(0x878e, 24, 14)]
    [InlineData(0x879e, 25, 14)]
    [InlineData(0x87ae, 26, 14)]
    [InlineData(0x87be, 27, 14)]
    [InlineData(0x87ee, 30, 14)]
    [InlineData(0x87fe, 31, 14)]

    [InlineData(0x860f, 00, 15)]
    [InlineData(0x861f, 01, 15)]
    [InlineData(0x862f, 02, 15)]
    [InlineData(0x863f, 03, 15)]
    [InlineData(0x864f, 04, 15)]
    [InlineData(0x865f, 05, 15)]
    [InlineData(0x866f, 06, 15)]
    [InlineData(0x867f, 07, 15)]
    [InlineData(0x868f, 08, 15)]
    [InlineData(0x869f, 09, 15)]
    [InlineData(0x86af, 10, 15)]
    [InlineData(0x86bf, 11, 15)]
    [InlineData(0x86cf, 12, 15)]
    [InlineData(0x86df, 13, 15)]
    [InlineData(0x86ef, 14, 15)]
    [InlineData(0x86ff, 15, 15)]
    [InlineData(0x870f, 16, 15)]
    [InlineData(0x871f, 17, 15)]
    [InlineData(0x872f, 18, 15)]
    [InlineData(0x873f, 19, 15)]
    [InlineData(0x874f, 20, 15)]
    [InlineData(0x875f, 21, 15)]
    [InlineData(0x876f, 22, 15)]
    [InlineData(0x877f, 23, 15)]
    [InlineData(0x878f, 24, 15)]
    [InlineData(0x879f, 25, 15)]
    [InlineData(0x87af, 26, 15)]
    [InlineData(0x87bf, 27, 15)]
    [InlineData(0x87ef, 30, 15)]
    [InlineData(0x87ff, 31, 15)]

    [InlineData(0x8a08, 00, 16)]
    [InlineData(0x8a18, 01, 16)]
    [InlineData(0x8a28, 02, 16)]
    [InlineData(0x8a38, 03, 16)]
    [InlineData(0x8a48, 04, 16)]
    [InlineData(0x8a58, 05, 16)]
    [InlineData(0x8a68, 06, 16)]
    [InlineData(0x8a78, 07, 16)]
    [InlineData(0x8a88, 08, 16)]
    [InlineData(0x8a98, 09, 16)]
    [InlineData(0x8aa8, 10, 16)]
    [InlineData(0x8ab8, 11, 16)]
    [InlineData(0x8ac8, 12, 16)]
    [InlineData(0x8ad8, 13, 16)]
    [InlineData(0x8ae8, 14, 16)]
    [InlineData(0x8af8, 15, 16)]
    [InlineData(0x8b08, 16, 16)]
    [InlineData(0x8b18, 17, 16)]
    [InlineData(0x8b28, 18, 16)]
    [InlineData(0x8b38, 19, 16)]
    [InlineData(0x8b48, 20, 16)]
    [InlineData(0x8b58, 21, 16)]
    [InlineData(0x8b68, 22, 16)]
    [InlineData(0x8b78, 23, 16)]
    [InlineData(0x8b88, 24, 16)]
    [InlineData(0x8b98, 25, 16)]
    [InlineData(0x8ba8, 26, 16)]
    [InlineData(0x8bb8, 27, 16)]
    [InlineData(0x8be8, 30, 16)]
    [InlineData(0x8bf8, 31, 16)]

    [InlineData(0x8a09, 00, 17)]
    [InlineData(0x8a19, 01, 17)]
    [InlineData(0x8a29, 02, 17)]
    [InlineData(0x8a39, 03, 17)]
    [InlineData(0x8a49, 04, 17)]
    [InlineData(0x8a59, 05, 17)]
    [InlineData(0x8a69, 06, 17)]
    [InlineData(0x8a79, 07, 17)]
    [InlineData(0x8a89, 08, 17)]
    [InlineData(0x8a99, 09, 17)]
    [InlineData(0x8aa9, 10, 17)]
    [InlineData(0x8ab9, 11, 17)]
    [InlineData(0x8ac9, 12, 17)]
    [InlineData(0x8ad9, 13, 17)]
    [InlineData(0x8ae9, 14, 17)]
    [InlineData(0x8af9, 15, 17)]
    [InlineData(0x8b09, 16, 17)]
    [InlineData(0x8b19, 17, 17)]
    [InlineData(0x8b29, 18, 17)]
    [InlineData(0x8b39, 19, 17)]
    [InlineData(0x8b49, 20, 17)]
    [InlineData(0x8b59, 21, 17)]
    [InlineData(0x8b69, 22, 17)]
    [InlineData(0x8b79, 23, 17)]
    [InlineData(0x8b89, 24, 17)]
    [InlineData(0x8b99, 25, 17)]
    [InlineData(0x8ba9, 26, 17)]
    [InlineData(0x8bb9, 27, 17)]
    [InlineData(0x8be9, 30, 17)]
    [InlineData(0x8bf9, 31, 17)]

    [InlineData(0x8a0a, 00, 18)]
    [InlineData(0x8a1a, 01, 18)]
    [InlineData(0x8a2a, 02, 18)]
    [InlineData(0x8a3a, 03, 18)]
    [InlineData(0x8a4a, 04, 18)]
    [InlineData(0x8a5a, 05, 18)]
    [InlineData(0x8a6a, 06, 18)]
    [InlineData(0x8a7a, 07, 18)]
    [InlineData(0x8a8a, 08, 18)]
    [InlineData(0x8a9a, 09, 18)]
    [InlineData(0x8aaa, 10, 18)]
    [InlineData(0x8aba, 11, 18)]
    [InlineData(0x8aca, 12, 18)]
    [InlineData(0x8ada, 13, 18)]
    [InlineData(0x8aea, 14, 18)]
    [InlineData(0x8afa, 15, 18)]
    [InlineData(0x8b0a, 16, 18)]
    [InlineData(0x8b1a, 17, 18)]
    [InlineData(0x8b2a, 18, 18)]
    [InlineData(0x8b3a, 19, 18)]
    [InlineData(0x8b4a, 20, 18)]
    [InlineData(0x8b5a, 21, 18)]
    [InlineData(0x8b6a, 22, 18)]
    [InlineData(0x8b7a, 23, 18)]
    [InlineData(0x8b8a, 24, 18)]
    [InlineData(0x8b9a, 25, 18)]
    [InlineData(0x8baa, 26, 18)]
    [InlineData(0x8bba, 27, 18)]
    [InlineData(0x8bea, 30, 18)]
    [InlineData(0x8bfa, 31, 18)]

    [InlineData(0x8a0b, 00, 19)]
    [InlineData(0x8a1b, 01, 19)]
    [InlineData(0x8a2b, 02, 19)]
    [InlineData(0x8a3b, 03, 19)]
    [InlineData(0x8a4b, 04, 19)]
    [InlineData(0x8a5b, 05, 19)]
    [InlineData(0x8a6b, 06, 19)]
    [InlineData(0x8a7b, 07, 19)]
    [InlineData(0x8a8b, 08, 19)]
    [InlineData(0x8a9b, 09, 19)]
    [InlineData(0x8aab, 10, 19)]
    [InlineData(0x8abb, 11, 19)]
    [InlineData(0x8acb, 12, 19)]
    [InlineData(0x8adb, 13, 19)]
    [InlineData(0x8aeb, 14, 19)]
    [InlineData(0x8afb, 15, 19)]
    [InlineData(0x8b0b, 16, 19)]
    [InlineData(0x8b1b, 17, 19)]
    [InlineData(0x8b2b, 18, 19)]
    [InlineData(0x8b3b, 19, 19)]
    [InlineData(0x8b4b, 20, 19)]
    [InlineData(0x8b5b, 21, 19)]
    [InlineData(0x8b6b, 22, 19)]
    [InlineData(0x8b7b, 23, 19)]
    [InlineData(0x8b8b, 24, 19)]
    [InlineData(0x8b9b, 25, 19)]
    [InlineData(0x8bab, 26, 19)]
    [InlineData(0x8bbb, 27, 19)]
    [InlineData(0x8beb, 30, 19)]
    [InlineData(0x8bfb, 31, 19)]

    [InlineData(0x8a0c, 00, 20)]
    [InlineData(0x8a1c, 01, 20)]
    [InlineData(0x8a2c, 02, 20)]
    [InlineData(0x8a3c, 03, 20)]
    [InlineData(0x8a4c, 04, 20)]
    [InlineData(0x8a5c, 05, 20)]
    [InlineData(0x8a6c, 06, 20)]
    [InlineData(0x8a7c, 07, 20)]
    [InlineData(0x8a8c, 08, 20)]
    [InlineData(0x8a9c, 09, 20)]
    [InlineData(0x8aac, 10, 20)]
    [InlineData(0x8abc, 11, 20)]
    [InlineData(0x8acc, 12, 20)]
    [InlineData(0x8adc, 13, 20)]
    [InlineData(0x8aec, 14, 20)]
    [InlineData(0x8afc, 15, 20)]
    [InlineData(0x8b0c, 16, 20)]
    [InlineData(0x8b1c, 17, 20)]
    [InlineData(0x8b2c, 18, 20)]
    [InlineData(0x8b3c, 19, 20)]
    [InlineData(0x8b4c, 20, 20)]
    [InlineData(0x8b5c, 21, 20)]
    [InlineData(0x8b6c, 22, 20)]
    [InlineData(0x8b7c, 23, 20)]
    [InlineData(0x8b8c, 24, 20)]
    [InlineData(0x8b9c, 25, 20)]
    [InlineData(0x8bac, 26, 20)]
    [InlineData(0x8bbc, 27, 20)]
    [InlineData(0x8bec, 30, 20)]
    [InlineData(0x8bfc, 31, 20)]

    [InlineData(0x8a0d, 00, 21)]
    [InlineData(0x8a1d, 01, 21)]
    [InlineData(0x8a2d, 02, 21)]
    [InlineData(0x8a3d, 03, 21)]
    [InlineData(0x8a4d, 04, 21)]
    [InlineData(0x8a5d, 05, 21)]
    [InlineData(0x8a6d, 06, 21)]
    [InlineData(0x8a7d, 07, 21)]
    [InlineData(0x8a8d, 08, 21)]
    [InlineData(0x8a9d, 09, 21)]
    [InlineData(0x8aad, 10, 21)]
    [InlineData(0x8abd, 11, 21)]
    [InlineData(0x8acd, 12, 21)]
    [InlineData(0x8add, 13, 21)]
    [InlineData(0x8aed, 14, 21)]
    [InlineData(0x8afd, 15, 21)]
    [InlineData(0x8b0d, 16, 21)]
    [InlineData(0x8b1d, 17, 21)]
    [InlineData(0x8b2d, 18, 21)]
    [InlineData(0x8b3d, 19, 21)]
    [InlineData(0x8b4d, 20, 21)]
    [InlineData(0x8b5d, 21, 21)]
    [InlineData(0x8b6d, 22, 21)]
    [InlineData(0x8b7d, 23, 21)]
    [InlineData(0x8b8d, 24, 21)]
    [InlineData(0x8b9d, 25, 21)]
    [InlineData(0x8bad, 26, 21)]
    [InlineData(0x8bbd, 27, 21)]
    [InlineData(0x8bed, 30, 21)]
    [InlineData(0x8bfd, 31, 21)]

    [InlineData(0x8a0e, 00, 22)]
    [InlineData(0x8a1e, 01, 22)]
    [InlineData(0x8a2e, 02, 22)]
    [InlineData(0x8a3e, 03, 22)]
    [InlineData(0x8a4e, 04, 22)]
    [InlineData(0x8a5e, 05, 22)]
    [InlineData(0x8a6e, 06, 22)]
    [InlineData(0x8a7e, 07, 22)]
    [InlineData(0x8a8e, 08, 22)]
    [InlineData(0x8a9e, 09, 22)]
    [InlineData(0x8aae, 10, 22)]
    [InlineData(0x8abe, 11, 22)]
    [InlineData(0x8ace, 12, 22)]
    [InlineData(0x8ade, 13, 22)]
    [InlineData(0x8aee, 14, 22)]
    [InlineData(0x8afe, 15, 22)]
    [InlineData(0x8b0e, 16, 22)]
    [InlineData(0x8b1e, 17, 22)]
    [InlineData(0x8b2e, 18, 22)]
    [InlineData(0x8b3e, 19, 22)]
    [InlineData(0x8b4e, 20, 22)]
    [InlineData(0x8b5e, 21, 22)]
    [InlineData(0x8b6e, 22, 22)]
    [InlineData(0x8b7e, 23, 22)]
    [InlineData(0x8b8e, 24, 22)]
    [InlineData(0x8b9e, 25, 22)]
    [InlineData(0x8bae, 26, 22)]
    [InlineData(0x8bbe, 27, 22)]
    [InlineData(0x8bee, 30, 22)]
    [InlineData(0x8bfe, 31, 22)]

    [InlineData(0x8a0f, 00, 23)]
    [InlineData(0x8a1f, 01, 23)]
    [InlineData(0x8a2f, 02, 23)]
    [InlineData(0x8a3f, 03, 23)]
    [InlineData(0x8a4f, 04, 23)]
    [InlineData(0x8a5f, 05, 23)]
    [InlineData(0x8a6f, 06, 23)]
    [InlineData(0x8a7f, 07, 23)]
    [InlineData(0x8a8f, 08, 23)]
    [InlineData(0x8a9f, 09, 23)]
    [InlineData(0x8aaf, 10, 23)]
    [InlineData(0x8abf, 11, 23)]
    [InlineData(0x8acf, 12, 23)]
    [InlineData(0x8adf, 13, 23)]
    [InlineData(0x8aef, 14, 23)]
    [InlineData(0x8aff, 15, 23)]
    [InlineData(0x8b0f, 16, 23)]
    [InlineData(0x8b1f, 17, 23)]
    [InlineData(0x8b2f, 18, 23)]
    [InlineData(0x8b3f, 19, 23)]
    [InlineData(0x8b4f, 20, 23)]
    [InlineData(0x8b5f, 21, 23)]
    [InlineData(0x8b6f, 22, 23)]
    [InlineData(0x8b7f, 23, 23)]
    [InlineData(0x8b8f, 24, 23)]
    [InlineData(0x8b9f, 25, 23)]
    [InlineData(0x8baf, 26, 23)]
    [InlineData(0x8bbf, 27, 23)]
    [InlineData(0x8bef, 30, 23)]
    [InlineData(0x8bff, 31, 23)]

    [InlineData(0x8e08, 00, 24)]
    [InlineData(0x8e18, 01, 24)]
    [InlineData(0x8e28, 02, 24)]
    [InlineData(0x8e38, 03, 24)]
    [InlineData(0x8e48, 04, 24)]
    [InlineData(0x8e58, 05, 24)]
    [InlineData(0x8e68, 06, 24)]
    [InlineData(0x8e78, 07, 24)]
    [InlineData(0x8e88, 08, 24)]
    [InlineData(0x8e98, 09, 24)]
    [InlineData(0x8ea8, 10, 24)]
    [InlineData(0x8eb8, 11, 24)]
    [InlineData(0x8ec8, 12, 24)]
    [InlineData(0x8ed8, 13, 24)]
    [InlineData(0x8ee8, 14, 24)]
    [InlineData(0x8ef8, 15, 24)]
    [InlineData(0x8f08, 16, 24)]
    [InlineData(0x8f18, 17, 24)]
    [InlineData(0x8f28, 18, 24)]
    [InlineData(0x8f38, 19, 24)]
    [InlineData(0x8f48, 20, 24)]
    [InlineData(0x8f58, 21, 24)]
    [InlineData(0x8f68, 22, 24)]
    [InlineData(0x8f78, 23, 24)]
    [InlineData(0x8f88, 24, 24)]
    [InlineData(0x8f98, 25, 24)]
    [InlineData(0x8fa8, 26, 24)]
    [InlineData(0x8fb8, 27, 24)]
    [InlineData(0x8fe8, 30, 24)]
    [InlineData(0x8ff8, 31, 24)]

    [InlineData(0x8e09, 00, 25)]
    [InlineData(0x8e19, 01, 25)]
    [InlineData(0x8e29, 02, 25)]
    [InlineData(0x8e39, 03, 25)]
    [InlineData(0x8e49, 04, 25)]
    [InlineData(0x8e59, 05, 25)]
    [InlineData(0x8e69, 06, 25)]
    [InlineData(0x8e79, 07, 25)]
    [InlineData(0x8e89, 08, 25)]
    [InlineData(0x8e99, 09, 25)]
    [InlineData(0x8ea9, 10, 25)]
    [InlineData(0x8eb9, 11, 25)]
    [InlineData(0x8ec9, 12, 25)]
    [InlineData(0x8ed9, 13, 25)]
    [InlineData(0x8ee9, 14, 25)]
    [InlineData(0x8ef9, 15, 25)]
    [InlineData(0x8f09, 16, 25)]
    [InlineData(0x8f19, 17, 25)]
    [InlineData(0x8f29, 18, 25)]
    [InlineData(0x8f39, 19, 25)]
    [InlineData(0x8f49, 20, 25)]
    [InlineData(0x8f59, 21, 25)]
    [InlineData(0x8f69, 22, 25)]
    [InlineData(0x8f79, 23, 25)]
    [InlineData(0x8f89, 24, 25)]
    [InlineData(0x8f99, 25, 25)]
    [InlineData(0x8fa9, 26, 25)]
    [InlineData(0x8fb9, 27, 25)]
    [InlineData(0x8fe9, 30, 25)]
    [InlineData(0x8ff9, 31, 25)]

    [InlineData(0x8e0a, 00, 26)]
    [InlineData(0x8e1a, 01, 26)]
    [InlineData(0x8e2a, 02, 26)]
    [InlineData(0x8e3a, 03, 26)]
    [InlineData(0x8e4a, 04, 26)]
    [InlineData(0x8e5a, 05, 26)]
    [InlineData(0x8e6a, 06, 26)]
    [InlineData(0x8e7a, 07, 26)]
    [InlineData(0x8e8a, 08, 26)]
    [InlineData(0x8e9a, 09, 26)]
    [InlineData(0x8eaa, 10, 26)]
    [InlineData(0x8eba, 11, 26)]
    [InlineData(0x8eca, 12, 26)]
    [InlineData(0x8eda, 13, 26)]
    [InlineData(0x8eea, 14, 26)]
    [InlineData(0x8efa, 15, 26)]
    [InlineData(0x8f0a, 16, 26)]
    [InlineData(0x8f1a, 17, 26)]
    [InlineData(0x8f2a, 18, 26)]
    [InlineData(0x8f3a, 19, 26)]
    [InlineData(0x8f4a, 20, 26)]
    [InlineData(0x8f5a, 21, 26)]
    [InlineData(0x8f6a, 22, 26)]
    [InlineData(0x8f7a, 23, 26)]
    [InlineData(0x8f8a, 24, 26)]
    [InlineData(0x8f9a, 25, 26)]
    [InlineData(0x8faa, 26, 26)]
    [InlineData(0x8fba, 27, 26)]
    [InlineData(0x8fea, 30, 26)]
    [InlineData(0x8ffa, 31, 26)]

    [InlineData(0x8e0b, 00, 27)]
    [InlineData(0x8e1b, 01, 27)]
    [InlineData(0x8e2b, 02, 27)]
    [InlineData(0x8e3b, 03, 27)]
    [InlineData(0x8e4b, 04, 27)]
    [InlineData(0x8e5b, 05, 27)]
    [InlineData(0x8e6b, 06, 27)]
    [InlineData(0x8e7b, 07, 27)]
    [InlineData(0x8e8b, 08, 27)]
    [InlineData(0x8e9b, 09, 27)]
    [InlineData(0x8eab, 10, 27)]
    [InlineData(0x8ebb, 11, 27)]
    [InlineData(0x8ecb, 12, 27)]
    [InlineData(0x8edb, 13, 27)]
    [InlineData(0x8eeb, 14, 27)]
    [InlineData(0x8efb, 15, 27)]
    [InlineData(0x8f0b, 16, 27)]
    [InlineData(0x8f1b, 17, 27)]
    [InlineData(0x8f2b, 18, 27)]
    [InlineData(0x8f3b, 19, 27)]
    [InlineData(0x8f4b, 20, 27)]
    [InlineData(0x8f5b, 21, 27)]
    [InlineData(0x8f6b, 22, 27)]
    [InlineData(0x8f7b, 23, 27)]
    [InlineData(0x8f8b, 24, 27)]
    [InlineData(0x8f9b, 25, 27)]
    [InlineData(0x8fab, 26, 27)]
    [InlineData(0x8fbb, 27, 27)]
    [InlineData(0x8feb, 30, 27)]
    [InlineData(0x8ffb, 31, 27)]

    [InlineData(0x8e0c, 00, 28)]
    [InlineData(0x8e1c, 01, 28)]
    [InlineData(0x8e2c, 02, 28)]
    [InlineData(0x8e3c, 03, 28)]
    [InlineData(0x8e4c, 04, 28)]
    [InlineData(0x8e5c, 05, 28)]
    [InlineData(0x8e6c, 06, 28)]
    [InlineData(0x8e7c, 07, 28)]
    [InlineData(0x8e8c, 08, 28)]
    [InlineData(0x8e9c, 09, 28)]
    [InlineData(0x8eac, 10, 28)]
    [InlineData(0x8ebc, 11, 28)]
    [InlineData(0x8ecc, 12, 28)]
    [InlineData(0x8edc, 13, 28)]
    [InlineData(0x8eec, 14, 28)]
    [InlineData(0x8efc, 15, 28)]
    [InlineData(0x8f0c, 16, 28)]
    [InlineData(0x8f1c, 17, 28)]
    [InlineData(0x8f2c, 18, 28)]
    [InlineData(0x8f3c, 19, 28)]
    [InlineData(0x8f4c, 20, 28)]
    [InlineData(0x8f5c, 21, 28)]
    [InlineData(0x8f6c, 22, 28)]
    [InlineData(0x8f7c, 23, 28)]
    [InlineData(0x8f8c, 24, 28)]
    [InlineData(0x8f9c, 25, 28)]
    [InlineData(0x8fac, 26, 28)]
    [InlineData(0x8fbc, 27, 28)]
    [InlineData(0x8fec, 30, 28)]
    [InlineData(0x8ffc, 31, 28)]

    [InlineData(0x8e0d, 00, 29)]
    [InlineData(0x8e1d, 01, 29)]
    [InlineData(0x8e2d, 02, 29)]
    [InlineData(0x8e3d, 03, 29)]
    [InlineData(0x8e4d, 04, 29)]
    [InlineData(0x8e5d, 05, 29)]
    [InlineData(0x8e6d, 06, 29)]
    [InlineData(0x8e7d, 07, 29)]
    [InlineData(0x8e8d, 08, 29)]
    [InlineData(0x8e9d, 09, 29)]
    [InlineData(0x8ead, 10, 29)]
    [InlineData(0x8ebd, 11, 29)]
    [InlineData(0x8ecd, 12, 29)]
    [InlineData(0x8edd, 13, 29)]
    [InlineData(0x8eed, 14, 29)]
    [InlineData(0x8efd, 15, 29)]
    [InlineData(0x8f0d, 16, 29)]
    [InlineData(0x8f1d, 17, 29)]
    [InlineData(0x8f2d, 18, 29)]
    [InlineData(0x8f3d, 19, 29)]
    [InlineData(0x8f4d, 20, 29)]
    [InlineData(0x8f5d, 21, 29)]
    [InlineData(0x8f6d, 22, 29)]
    [InlineData(0x8f7d, 23, 29)]
    [InlineData(0x8f8d, 24, 29)]
    [InlineData(0x8f9d, 25, 29)]
    [InlineData(0x8fad, 26, 29)]
    [InlineData(0x8fbd, 27, 29)]
    [InlineData(0x8fed, 30, 29)]
    [InlineData(0x8ffd, 31, 29)]

    [InlineData(0x8e0e, 00, 30)]
    [InlineData(0x8e1e, 01, 30)]
    [InlineData(0x8e2e, 02, 30)]
    [InlineData(0x8e3e, 03, 30)]
    [InlineData(0x8e4e, 04, 30)]
    [InlineData(0x8e5e, 05, 30)]
    [InlineData(0x8e6e, 06, 30)]
    [InlineData(0x8e7e, 07, 30)]
    [InlineData(0x8e8e, 08, 30)]
    [InlineData(0x8e9e, 09, 30)]
    [InlineData(0x8eae, 10, 30)]
    [InlineData(0x8ebe, 11, 30)]
    [InlineData(0x8ece, 12, 30)]
    [InlineData(0x8ede, 13, 30)]
    [InlineData(0x8eee, 14, 30)]
    [InlineData(0x8efe, 15, 30)]
    [InlineData(0x8f0e, 16, 30)]
    [InlineData(0x8f1e, 17, 30)]
    [InlineData(0x8f2e, 18, 30)]
    [InlineData(0x8f3e, 19, 30)]
    [InlineData(0x8f4e, 20, 30)]
    [InlineData(0x8f5e, 21, 30)]
    [InlineData(0x8f6e, 22, 30)]
    [InlineData(0x8f7e, 23, 30)]
    [InlineData(0x8f8e, 24, 30)]
    [InlineData(0x8f9e, 25, 30)]
    [InlineData(0x8fae, 26, 30)]
    [InlineData(0x8fbe, 27, 30)]
    [InlineData(0x8fee, 30, 30)]
    [InlineData(0x8ffe, 31, 30)]

    [InlineData(0x8e0f, 00, 31)]
    [InlineData(0x8e1f, 01, 31)]
    [InlineData(0x8e2f, 02, 31)]
    [InlineData(0x8e3f, 03, 31)]
    [InlineData(0x8e4f, 04, 31)]
    [InlineData(0x8e5f, 05, 31)]
    [InlineData(0x8e6f, 06, 31)]
    [InlineData(0x8e7f, 07, 31)]
    [InlineData(0x8e8f, 08, 31)]
    [InlineData(0x8e9f, 09, 31)]
    [InlineData(0x8eaf, 10, 31)]
    [InlineData(0x8ebf, 11, 31)]
    [InlineData(0x8ecf, 12, 31)]
    [InlineData(0x8edf, 13, 31)]
    [InlineData(0x8eef, 14, 31)]
    [InlineData(0x8eff, 15, 31)]
    [InlineData(0x8f0f, 16, 31)]
    [InlineData(0x8f1f, 17, 31)]
    [InlineData(0x8f2f, 18, 31)]
    [InlineData(0x8f3f, 19, 31)]
    [InlineData(0x8f4f, 20, 31)]
    [InlineData(0x8f5f, 21, 31)]
    [InlineData(0x8f6f, 22, 31)]
    [InlineData(0x8f7f, 23, 31)]
    [InlineData(0x8f8f, 24, 31)]
    [InlineData(0x8f9f, 25, 31)]
    [InlineData(0x8faf, 26, 31)]
    [InlineData(0x8fbf, 27, 31)]
    [InlineData(0x8fef, 30, 31)]
    [InlineData(0x8fff, 31, 31)]

    [InlineData(0xa208, 00, 32)]
    [InlineData(0xa218, 01, 32)]
    [InlineData(0xa228, 02, 32)]
    [InlineData(0xa238, 03, 32)]
    [InlineData(0xa248, 04, 32)]
    [InlineData(0xa258, 05, 32)]
    [InlineData(0xa268, 06, 32)]
    [InlineData(0xa278, 07, 32)]
    [InlineData(0xa288, 08, 32)]
    [InlineData(0xa298, 09, 32)]
    [InlineData(0xa2a8, 10, 32)]
    [InlineData(0xa2b8, 11, 32)]
    [InlineData(0xa2c8, 12, 32)]
    [InlineData(0xa2d8, 13, 32)]
    [InlineData(0xa2e8, 14, 32)]
    [InlineData(0xa2f8, 15, 32)]
    [InlineData(0xa308, 16, 32)]
    [InlineData(0xa318, 17, 32)]
    [InlineData(0xa328, 18, 32)]
    [InlineData(0xa338, 19, 32)]
    [InlineData(0xa348, 20, 32)]
    [InlineData(0xa358, 21, 32)]
    [InlineData(0xa368, 22, 32)]
    [InlineData(0xa378, 23, 32)]
    [InlineData(0xa388, 24, 32)]
    [InlineData(0xa398, 25, 32)]
    [InlineData(0xa3a8, 26, 32)]
    [InlineData(0xa3b8, 27, 32)]
    [InlineData(0xa3e8, 30, 32)]
    [InlineData(0xa3f8, 31, 32)]

    [InlineData(0xa209, 00, 33)]
    [InlineData(0xa219, 01, 33)]
    [InlineData(0xa229, 02, 33)]
    [InlineData(0xa239, 03, 33)]
    [InlineData(0xa249, 04, 33)]
    [InlineData(0xa259, 05, 33)]
    [InlineData(0xa269, 06, 33)]
    [InlineData(0xa279, 07, 33)]
    [InlineData(0xa289, 08, 33)]
    [InlineData(0xa299, 09, 33)]
    [InlineData(0xa2a9, 10, 33)]
    [InlineData(0xa2b9, 11, 33)]
    [InlineData(0xa2c9, 12, 33)]
    [InlineData(0xa2d9, 13, 33)]
    [InlineData(0xa2e9, 14, 33)]
    [InlineData(0xa2f9, 15, 33)]
    [InlineData(0xa309, 16, 33)]
    [InlineData(0xa319, 17, 33)]
    [InlineData(0xa329, 18, 33)]
    [InlineData(0xa339, 19, 33)]
    [InlineData(0xa349, 20, 33)]
    [InlineData(0xa359, 21, 33)]
    [InlineData(0xa369, 22, 33)]
    [InlineData(0xa379, 23, 33)]
    [InlineData(0xa389, 24, 33)]
    [InlineData(0xa399, 25, 33)]
    [InlineData(0xa3a9, 26, 33)]
    [InlineData(0xa3b9, 27, 33)]
    [InlineData(0xa3e9, 30, 33)]
    [InlineData(0xa3f9, 31, 33)]

    [InlineData(0xa20a, 00, 34)]
    [InlineData(0xa21a, 01, 34)]
    [InlineData(0xa22a, 02, 34)]
    [InlineData(0xa23a, 03, 34)]
    [InlineData(0xa24a, 04, 34)]
    [InlineData(0xa25a, 05, 34)]
    [InlineData(0xa26a, 06, 34)]
    [InlineData(0xa27a, 07, 34)]
    [InlineData(0xa28a, 08, 34)]
    [InlineData(0xa29a, 09, 34)]
    [InlineData(0xa2aa, 10, 34)]
    [InlineData(0xa2ba, 11, 34)]
    [InlineData(0xa2ca, 12, 34)]
    [InlineData(0xa2da, 13, 34)]
    [InlineData(0xa2ea, 14, 34)]
    [InlineData(0xa2fa, 15, 34)]
    [InlineData(0xa30a, 16, 34)]
    [InlineData(0xa31a, 17, 34)]
    [InlineData(0xa32a, 18, 34)]
    [InlineData(0xa33a, 19, 34)]
    [InlineData(0xa34a, 20, 34)]
    [InlineData(0xa35a, 21, 34)]
    [InlineData(0xa36a, 22, 34)]
    [InlineData(0xa37a, 23, 34)]
    [InlineData(0xa38a, 24, 34)]
    [InlineData(0xa39a, 25, 34)]
    [InlineData(0xa3aa, 26, 34)]
    [InlineData(0xa3ba, 27, 34)]
    [InlineData(0xa3ea, 30, 34)]
    [InlineData(0xa3fa, 31, 34)]

    [InlineData(0xa20b, 00, 35)]
    [InlineData(0xa21b, 01, 35)]
    [InlineData(0xa22b, 02, 35)]
    [InlineData(0xa23b, 03, 35)]
    [InlineData(0xa24b, 04, 35)]
    [InlineData(0xa25b, 05, 35)]
    [InlineData(0xa26b, 06, 35)]
    [InlineData(0xa27b, 07, 35)]
    [InlineData(0xa28b, 08, 35)]
    [InlineData(0xa29b, 09, 35)]
    [InlineData(0xa2ab, 10, 35)]
    [InlineData(0xa2bb, 11, 35)]
    [InlineData(0xa2cb, 12, 35)]
    [InlineData(0xa2db, 13, 35)]
    [InlineData(0xa2eb, 14, 35)]
    [InlineData(0xa2fb, 15, 35)]
    [InlineData(0xa30b, 16, 35)]
    [InlineData(0xa31b, 17, 35)]
    [InlineData(0xa32b, 18, 35)]
    [InlineData(0xa33b, 19, 35)]
    [InlineData(0xa34b, 20, 35)]
    [InlineData(0xa35b, 21, 35)]
    [InlineData(0xa36b, 22, 35)]
    [InlineData(0xa37b, 23, 35)]
    [InlineData(0xa38b, 24, 35)]
    [InlineData(0xa39b, 25, 35)]
    [InlineData(0xa3ab, 26, 35)]
    [InlineData(0xa3bb, 27, 35)]
    [InlineData(0xa3eb, 30, 35)]
    [InlineData(0xa3fb, 31, 35)]

    [InlineData(0xa20c, 00, 36)]
    [InlineData(0xa21c, 01, 36)]
    [InlineData(0xa22c, 02, 36)]
    [InlineData(0xa23c, 03, 36)]
    [InlineData(0xa24c, 04, 36)]
    [InlineData(0xa25c, 05, 36)]
    [InlineData(0xa26c, 06, 36)]
    [InlineData(0xa27c, 07, 36)]
    [InlineData(0xa28c, 08, 36)]
    [InlineData(0xa29c, 09, 36)]
    [InlineData(0xa2ac, 10, 36)]
    [InlineData(0xa2bc, 11, 36)]
    [InlineData(0xa2cc, 12, 36)]
    [InlineData(0xa2dc, 13, 36)]
    [InlineData(0xa2ec, 14, 36)]
    [InlineData(0xa2fc, 15, 36)]
    [InlineData(0xa30c, 16, 36)]
    [InlineData(0xa31c, 17, 36)]
    [InlineData(0xa32c, 18, 36)]
    [InlineData(0xa33c, 19, 36)]
    [InlineData(0xa34c, 20, 36)]
    [InlineData(0xa35c, 21, 36)]
    [InlineData(0xa36c, 22, 36)]
    [InlineData(0xa37c, 23, 36)]
    [InlineData(0xa38c, 24, 36)]
    [InlineData(0xa39c, 25, 36)]
    [InlineData(0xa3ac, 26, 36)]
    [InlineData(0xa3bc, 27, 36)]
    [InlineData(0xa3ec, 30, 36)]
    [InlineData(0xa3fc, 31, 36)]

    [InlineData(0xa20d, 00, 37)]
    [InlineData(0xa21d, 01, 37)]
    [InlineData(0xa22d, 02, 37)]
    [InlineData(0xa23d, 03, 37)]
    [InlineData(0xa24d, 04, 37)]
    [InlineData(0xa25d, 05, 37)]
    [InlineData(0xa26d, 06, 37)]
    [InlineData(0xa27d, 07, 37)]
    [InlineData(0xa28d, 08, 37)]
    [InlineData(0xa29d, 09, 37)]
    [InlineData(0xa2ad, 10, 37)]
    [InlineData(0xa2bd, 11, 37)]
    [InlineData(0xa2cd, 12, 37)]
    [InlineData(0xa2dd, 13, 37)]
    [InlineData(0xa2ed, 14, 37)]
    [InlineData(0xa2fd, 15, 37)]
    [InlineData(0xa30d, 16, 37)]
    [InlineData(0xa31d, 17, 37)]
    [InlineData(0xa32d, 18, 37)]
    [InlineData(0xa33d, 19, 37)]
    [InlineData(0xa34d, 20, 37)]
    [InlineData(0xa35d, 21, 37)]
    [InlineData(0xa36d, 22, 37)]
    [InlineData(0xa37d, 23, 37)]
    [InlineData(0xa38d, 24, 37)]
    [InlineData(0xa39d, 25, 37)]
    [InlineData(0xa3ad, 26, 37)]
    [InlineData(0xa3bd, 27, 37)]
    [InlineData(0xa3ed, 30, 37)]
    [InlineData(0xa3fd, 31, 37)]

    [InlineData(0xa20e, 00, 38)]
    [InlineData(0xa21e, 01, 38)]
    [InlineData(0xa22e, 02, 38)]
    [InlineData(0xa23e, 03, 38)]
    [InlineData(0xa24e, 04, 38)]
    [InlineData(0xa25e, 05, 38)]
    [InlineData(0xa26e, 06, 38)]
    [InlineData(0xa27e, 07, 38)]
    [InlineData(0xa28e, 08, 38)]
    [InlineData(0xa29e, 09, 38)]
    [InlineData(0xa2ae, 10, 38)]
    [InlineData(0xa2be, 11, 38)]
    [InlineData(0xa2ce, 12, 38)]
    [InlineData(0xa2de, 13, 38)]
    [InlineData(0xa2ee, 14, 38)]
    [InlineData(0xa2fe, 15, 38)]
    [InlineData(0xa30e, 16, 38)]
    [InlineData(0xa31e, 17, 38)]
    [InlineData(0xa32e, 18, 38)]
    [InlineData(0xa33e, 19, 38)]
    [InlineData(0xa34e, 20, 38)]
    [InlineData(0xa35e, 21, 38)]
    [InlineData(0xa36e, 22, 38)]
    [InlineData(0xa37e, 23, 38)]
    [InlineData(0xa38e, 24, 38)]
    [InlineData(0xa39e, 25, 38)]
    [InlineData(0xa3ae, 26, 38)]
    [InlineData(0xa3be, 27, 38)]
    [InlineData(0xa3ee, 30, 38)]
    [InlineData(0xa3fe, 31, 38)]

    [InlineData(0xa20f, 00, 39)]
    [InlineData(0xa21f, 01, 39)]
    [InlineData(0xa22f, 02, 39)]
    [InlineData(0xa23f, 03, 39)]
    [InlineData(0xa24f, 04, 39)]
    [InlineData(0xa25f, 05, 39)]
    [InlineData(0xa26f, 06, 39)]
    [InlineData(0xa27f, 07, 39)]
    [InlineData(0xa28f, 08, 39)]
    [InlineData(0xa29f, 09, 39)]
    [InlineData(0xa2af, 10, 39)]
    [InlineData(0xa2bf, 11, 39)]
    [InlineData(0xa2cf, 12, 39)]
    [InlineData(0xa2df, 13, 39)]
    [InlineData(0xa2ef, 14, 39)]
    [InlineData(0xa2ff, 15, 39)]
    [InlineData(0xa30f, 16, 39)]
    [InlineData(0xa31f, 17, 39)]
    [InlineData(0xa32f, 18, 39)]
    [InlineData(0xa33f, 19, 39)]
    [InlineData(0xa34f, 20, 39)]
    [InlineData(0xa35f, 21, 39)]
    [InlineData(0xa36f, 22, 39)]
    [InlineData(0xa37f, 23, 39)]
    [InlineData(0xa38f, 24, 39)]
    [InlineData(0xa39f, 25, 39)]
    [InlineData(0xa3af, 26, 39)]
    [InlineData(0xa3bf, 27, 39)]
    [InlineData(0xa3ef, 30, 39)]
    [InlineData(0xa3ff, 31, 39)]

    [InlineData(0xa608, 00, 40)]
    [InlineData(0xa618, 01, 40)]
    [InlineData(0xa628, 02, 40)]
    [InlineData(0xa638, 03, 40)]
    [InlineData(0xa648, 04, 40)]
    [InlineData(0xa658, 05, 40)]
    [InlineData(0xa668, 06, 40)]
    [InlineData(0xa678, 07, 40)]
    [InlineData(0xa688, 08, 40)]
    [InlineData(0xa698, 09, 40)]
    [InlineData(0xa6a8, 10, 40)]
    [InlineData(0xa6b8, 11, 40)]
    [InlineData(0xa6c8, 12, 40)]
    [InlineData(0xa6d8, 13, 40)]
    [InlineData(0xa6e8, 14, 40)]
    [InlineData(0xa6f8, 15, 40)]
    [InlineData(0xa708, 16, 40)]
    [InlineData(0xa718, 17, 40)]
    [InlineData(0xa728, 18, 40)]
    [InlineData(0xa738, 19, 40)]
    [InlineData(0xa748, 20, 40)]
    [InlineData(0xa758, 21, 40)]
    [InlineData(0xa768, 22, 40)]
    [InlineData(0xa778, 23, 40)]
    [InlineData(0xa788, 24, 40)]
    [InlineData(0xa798, 25, 40)]
    [InlineData(0xa7a8, 26, 40)]
    [InlineData(0xa7b8, 27, 40)]
    [InlineData(0xa7e8, 30, 40)]
    [InlineData(0xa7f8, 31, 40)]

    [InlineData(0xa609, 00, 41)]
    [InlineData(0xa619, 01, 41)]
    [InlineData(0xa629, 02, 41)]
    [InlineData(0xa639, 03, 41)]
    [InlineData(0xa649, 04, 41)]
    [InlineData(0xa659, 05, 41)]
    [InlineData(0xa669, 06, 41)]
    [InlineData(0xa679, 07, 41)]
    [InlineData(0xa689, 08, 41)]
    [InlineData(0xa699, 09, 41)]
    [InlineData(0xa6a9, 10, 41)]
    [InlineData(0xa6b9, 11, 41)]
    [InlineData(0xa6c9, 12, 41)]
    [InlineData(0xa6d9, 13, 41)]
    [InlineData(0xa6e9, 14, 41)]
    [InlineData(0xa6f9, 15, 41)]
    [InlineData(0xa709, 16, 41)]
    [InlineData(0xa719, 17, 41)]
    [InlineData(0xa729, 18, 41)]
    [InlineData(0xa739, 19, 41)]
    [InlineData(0xa749, 20, 41)]
    [InlineData(0xa759, 21, 41)]
    [InlineData(0xa769, 22, 41)]
    [InlineData(0xa779, 23, 41)]
    [InlineData(0xa789, 24, 41)]
    [InlineData(0xa799, 25, 41)]
    [InlineData(0xa7a9, 26, 41)]
    [InlineData(0xa7b9, 27, 41)]
    [InlineData(0xa7e9, 30, 41)]
    [InlineData(0xa7f9, 31, 41)]

    [InlineData(0xa60a, 00, 42)]
    [InlineData(0xa61a, 01, 42)]
    [InlineData(0xa62a, 02, 42)]
    [InlineData(0xa63a, 03, 42)]
    [InlineData(0xa64a, 04, 42)]
    [InlineData(0xa65a, 05, 42)]
    [InlineData(0xa66a, 06, 42)]
    [InlineData(0xa67a, 07, 42)]
    [InlineData(0xa68a, 08, 42)]
    [InlineData(0xa69a, 09, 42)]
    [InlineData(0xa6aa, 10, 42)]
    [InlineData(0xa6ba, 11, 42)]
    [InlineData(0xa6ca, 12, 42)]
    [InlineData(0xa6da, 13, 42)]
    [InlineData(0xa6ea, 14, 42)]
    [InlineData(0xa6fa, 15, 42)]
    [InlineData(0xa70a, 16, 42)]
    [InlineData(0xa71a, 17, 42)]
    [InlineData(0xa72a, 18, 42)]
    [InlineData(0xa73a, 19, 42)]
    [InlineData(0xa74a, 20, 42)]
    [InlineData(0xa75a, 21, 42)]
    [InlineData(0xa76a, 22, 42)]
    [InlineData(0xa77a, 23, 42)]
    [InlineData(0xa78a, 24, 42)]
    [InlineData(0xa79a, 25, 42)]
    [InlineData(0xa7aa, 26, 42)]
    [InlineData(0xa7ba, 27, 42)]
    [InlineData(0xa7ea, 30, 42)]
    [InlineData(0xa7fa, 31, 42)]

    [InlineData(0xa60b, 00, 43)]
    [InlineData(0xa61b, 01, 43)]
    [InlineData(0xa62b, 02, 43)]
    [InlineData(0xa63b, 03, 43)]
    [InlineData(0xa64b, 04, 43)]
    [InlineData(0xa65b, 05, 43)]
    [InlineData(0xa66b, 06, 43)]
    [InlineData(0xa67b, 07, 43)]
    [InlineData(0xa68b, 08, 43)]
    [InlineData(0xa69b, 09, 43)]
    [InlineData(0xa6ab, 10, 43)]
    [InlineData(0xa6bb, 11, 43)]
    [InlineData(0xa6cb, 12, 43)]
    [InlineData(0xa6db, 13, 43)]
    [InlineData(0xa6eb, 14, 43)]
    [InlineData(0xa6fb, 15, 43)]
    [InlineData(0xa70b, 16, 43)]
    [InlineData(0xa71b, 17, 43)]
    [InlineData(0xa72b, 18, 43)]
    [InlineData(0xa73b, 19, 43)]
    [InlineData(0xa74b, 20, 43)]
    [InlineData(0xa75b, 21, 43)]
    [InlineData(0xa76b, 22, 43)]
    [InlineData(0xa77b, 23, 43)]
    [InlineData(0xa78b, 24, 43)]
    [InlineData(0xa79b, 25, 43)]
    [InlineData(0xa7ab, 26, 43)]
    [InlineData(0xa7bb, 27, 43)]
    [InlineData(0xa7eb, 30, 43)]
    [InlineData(0xa7fb, 31, 43)]

    [InlineData(0xa60c, 00, 44)]
    [InlineData(0xa61c, 01, 44)]
    [InlineData(0xa62c, 02, 44)]
    [InlineData(0xa63c, 03, 44)]
    [InlineData(0xa64c, 04, 44)]
    [InlineData(0xa65c, 05, 44)]
    [InlineData(0xa66c, 06, 44)]
    [InlineData(0xa67c, 07, 44)]
    [InlineData(0xa68c, 08, 44)]
    [InlineData(0xa69c, 09, 44)]
    [InlineData(0xa6ac, 10, 44)]
    [InlineData(0xa6bc, 11, 44)]
    [InlineData(0xa6cc, 12, 44)]
    [InlineData(0xa6dc, 13, 44)]
    [InlineData(0xa6ec, 14, 44)]
    [InlineData(0xa6fc, 15, 44)]
    [InlineData(0xa70c, 16, 44)]
    [InlineData(0xa71c, 17, 44)]
    [InlineData(0xa72c, 18, 44)]
    [InlineData(0xa73c, 19, 44)]
    [InlineData(0xa74c, 20, 44)]
    [InlineData(0xa75c, 21, 44)]
    [InlineData(0xa76c, 22, 44)]
    [InlineData(0xa77c, 23, 44)]
    [InlineData(0xa78c, 24, 44)]
    [InlineData(0xa79c, 25, 44)]
    [InlineData(0xa7ac, 26, 44)]
    [InlineData(0xa7bc, 27, 44)]
    [InlineData(0xa7ec, 30, 44)]
    [InlineData(0xa7fc, 31, 44)]

    [InlineData(0xa60d, 00, 45)]
    [InlineData(0xa61d, 01, 45)]
    [InlineData(0xa62d, 02, 45)]
    [InlineData(0xa63d, 03, 45)]
    [InlineData(0xa64d, 04, 45)]
    [InlineData(0xa65d, 05, 45)]
    [InlineData(0xa66d, 06, 45)]
    [InlineData(0xa67d, 07, 45)]
    [InlineData(0xa68d, 08, 45)]
    [InlineData(0xa69d, 09, 45)]
    [InlineData(0xa6ad, 10, 45)]
    [InlineData(0xa6bd, 11, 45)]
    [InlineData(0xa6cd, 12, 45)]
    [InlineData(0xa6dd, 13, 45)]
    [InlineData(0xa6ed, 14, 45)]
    [InlineData(0xa6fd, 15, 45)]
    [InlineData(0xa70d, 16, 45)]
    [InlineData(0xa71d, 17, 45)]
    [InlineData(0xa72d, 18, 45)]
    [InlineData(0xa73d, 19, 45)]
    [InlineData(0xa74d, 20, 45)]
    [InlineData(0xa75d, 21, 45)]
    [InlineData(0xa76d, 22, 45)]
    [InlineData(0xa77d, 23, 45)]
    [InlineData(0xa78d, 24, 45)]
    [InlineData(0xa79d, 25, 45)]
    [InlineData(0xa7ad, 26, 45)]
    [InlineData(0xa7bd, 27, 45)]
    [InlineData(0xa7ed, 30, 45)]
    [InlineData(0xa7fd, 31, 45)]

    [InlineData(0xa60e, 00, 46)]
    [InlineData(0xa61e, 01, 46)]
    [InlineData(0xa62e, 02, 46)]
    [InlineData(0xa63e, 03, 46)]
    [InlineData(0xa64e, 04, 46)]
    [InlineData(0xa65e, 05, 46)]
    [InlineData(0xa66e, 06, 46)]
    [InlineData(0xa67e, 07, 46)]
    [InlineData(0xa68e, 08, 46)]
    [InlineData(0xa69e, 09, 46)]
    [InlineData(0xa6ae, 10, 46)]
    [InlineData(0xa6be, 11, 46)]
    [InlineData(0xa6ce, 12, 46)]
    [InlineData(0xa6de, 13, 46)]
    [InlineData(0xa6ee, 14, 46)]
    [InlineData(0xa6fe, 15, 46)]
    [InlineData(0xa70e, 16, 46)]
    [InlineData(0xa71e, 17, 46)]
    [InlineData(0xa72e, 18, 46)]
    [InlineData(0xa73e, 19, 46)]
    [InlineData(0xa74e, 20, 46)]
    [InlineData(0xa75e, 21, 46)]
    [InlineData(0xa76e, 22, 46)]
    [InlineData(0xa77e, 23, 46)]
    [InlineData(0xa78e, 24, 46)]
    [InlineData(0xa79e, 25, 46)]
    [InlineData(0xa7ae, 26, 46)]
    [InlineData(0xa7be, 27, 46)]
    [InlineData(0xa7ee, 30, 46)]
    [InlineData(0xa7fe, 31, 46)]

    [InlineData(0xa60f, 00, 47)]
    [InlineData(0xa61f, 01, 47)]
    [InlineData(0xa62f, 02, 47)]
    [InlineData(0xa63f, 03, 47)]
    [InlineData(0xa64f, 04, 47)]
    [InlineData(0xa65f, 05, 47)]
    [InlineData(0xa66f, 06, 47)]
    [InlineData(0xa67f, 07, 47)]
    [InlineData(0xa68f, 08, 47)]
    [InlineData(0xa69f, 09, 47)]
    [InlineData(0xa6af, 10, 47)]
    [InlineData(0xa6bf, 11, 47)]
    [InlineData(0xa6cf, 12, 47)]
    [InlineData(0xa6df, 13, 47)]
    [InlineData(0xa6ef, 14, 47)]
    [InlineData(0xa6ff, 15, 47)]
    [InlineData(0xa70f, 16, 47)]
    [InlineData(0xa71f, 17, 47)]
    [InlineData(0xa72f, 18, 47)]
    [InlineData(0xa73f, 19, 47)]
    [InlineData(0xa74f, 20, 47)]
    [InlineData(0xa75f, 21, 47)]
    [InlineData(0xa76f, 22, 47)]
    [InlineData(0xa77f, 23, 47)]
    [InlineData(0xa78f, 24, 47)]
    [InlineData(0xa79f, 25, 47)]
    [InlineData(0xa7af, 26, 47)]
    [InlineData(0xa7bf, 27, 47)]
    [InlineData(0xa7ef, 30, 47)]
    [InlineData(0xa7ff, 31, 47)]

    [InlineData(0xaa08, 00, 48)]
    [InlineData(0xaa18, 01, 48)]
    [InlineData(0xaa28, 02, 48)]
    [InlineData(0xaa38, 03, 48)]
    [InlineData(0xaa48, 04, 48)]
    [InlineData(0xaa58, 05, 48)]
    [InlineData(0xaa68, 06, 48)]
    [InlineData(0xaa78, 07, 48)]
    [InlineData(0xaa88, 08, 48)]
    [InlineData(0xaa98, 09, 48)]
    [InlineData(0xaaa8, 10, 48)]
    [InlineData(0xaab8, 11, 48)]
    [InlineData(0xaac8, 12, 48)]
    [InlineData(0xaad8, 13, 48)]
    [InlineData(0xaae8, 14, 48)]
    [InlineData(0xaaf8, 15, 48)]
    [InlineData(0xab08, 16, 48)]
    [InlineData(0xab18, 17, 48)]
    [InlineData(0xab28, 18, 48)]
    [InlineData(0xab38, 19, 48)]
    [InlineData(0xab48, 20, 48)]
    [InlineData(0xab58, 21, 48)]
    [InlineData(0xab68, 22, 48)]
    [InlineData(0xab78, 23, 48)]
    [InlineData(0xab88, 24, 48)]
    [InlineData(0xab98, 25, 48)]
    [InlineData(0xaba8, 26, 48)]
    [InlineData(0xabb8, 27, 48)]
    [InlineData(0xabe8, 30, 48)]
    [InlineData(0xabf8, 31, 48)]

    [InlineData(0xaa09, 00, 49)]
    [InlineData(0xaa19, 01, 49)]
    [InlineData(0xaa29, 02, 49)]
    [InlineData(0xaa39, 03, 49)]
    [InlineData(0xaa49, 04, 49)]
    [InlineData(0xaa59, 05, 49)]
    [InlineData(0xaa69, 06, 49)]
    [InlineData(0xaa79, 07, 49)]
    [InlineData(0xaa89, 08, 49)]
    [InlineData(0xaa99, 09, 49)]
    [InlineData(0xaaa9, 10, 49)]
    [InlineData(0xaab9, 11, 49)]
    [InlineData(0xaac9, 12, 49)]
    [InlineData(0xaad9, 13, 49)]
    [InlineData(0xaae9, 14, 49)]
    [InlineData(0xaaf9, 15, 49)]
    [InlineData(0xab09, 16, 49)]
    [InlineData(0xab19, 17, 49)]
    [InlineData(0xab29, 18, 49)]
    [InlineData(0xab39, 19, 49)]
    [InlineData(0xab49, 20, 49)]
    [InlineData(0xab59, 21, 49)]
    [InlineData(0xab69, 22, 49)]
    [InlineData(0xab79, 23, 49)]
    [InlineData(0xab89, 24, 49)]
    [InlineData(0xab99, 25, 49)]
    [InlineData(0xaba9, 26, 49)]
    [InlineData(0xabb9, 27, 49)]
    [InlineData(0xabe9, 30, 49)]
    [InlineData(0xabf9, 31, 49)]

    [InlineData(0xaa0a, 00, 50)]
    [InlineData(0xaa1a, 01, 50)]
    [InlineData(0xaa2a, 02, 50)]
    [InlineData(0xaa3a, 03, 50)]
    [InlineData(0xaa4a, 04, 50)]
    [InlineData(0xaa5a, 05, 50)]
    [InlineData(0xaa6a, 06, 50)]
    [InlineData(0xaa7a, 07, 50)]
    [InlineData(0xaa8a, 08, 50)]
    [InlineData(0xaa9a, 09, 50)]
    [InlineData(0xaaaa, 10, 50)]
    [InlineData(0xaaba, 11, 50)]
    [InlineData(0xaaca, 12, 50)]
    [InlineData(0xaada, 13, 50)]
    [InlineData(0xaaea, 14, 50)]
    [InlineData(0xaafa, 15, 50)]
    [InlineData(0xab0a, 16, 50)]
    [InlineData(0xab1a, 17, 50)]
    [InlineData(0xab2a, 18, 50)]
    [InlineData(0xab3a, 19, 50)]
    [InlineData(0xab4a, 20, 50)]
    [InlineData(0xab5a, 21, 50)]
    [InlineData(0xab6a, 22, 50)]
    [InlineData(0xab7a, 23, 50)]
    [InlineData(0xab8a, 24, 50)]
    [InlineData(0xab9a, 25, 50)]
    [InlineData(0xabaa, 26, 50)]
    [InlineData(0xabba, 27, 50)]
    [InlineData(0xabea, 30, 50)]
    [InlineData(0xabfa, 31, 50)]

    [InlineData(0xaa0b, 00, 51)]
    [InlineData(0xaa1b, 01, 51)]
    [InlineData(0xaa2b, 02, 51)]
    [InlineData(0xaa3b, 03, 51)]
    [InlineData(0xaa4b, 04, 51)]
    [InlineData(0xaa5b, 05, 51)]
    [InlineData(0xaa6b, 06, 51)]
    [InlineData(0xaa7b, 07, 51)]
    [InlineData(0xaa8b, 08, 51)]
    [InlineData(0xaa9b, 09, 51)]
    [InlineData(0xaaab, 10, 51)]
    [InlineData(0xaabb, 11, 51)]
    [InlineData(0xaacb, 12, 51)]
    [InlineData(0xaadb, 13, 51)]
    [InlineData(0xaaeb, 14, 51)]
    [InlineData(0xaafb, 15, 51)]
    [InlineData(0xab0b, 16, 51)]
    [InlineData(0xab1b, 17, 51)]
    [InlineData(0xab2b, 18, 51)]
    [InlineData(0xab3b, 19, 51)]
    [InlineData(0xab4b, 20, 51)]
    [InlineData(0xab5b, 21, 51)]
    [InlineData(0xab6b, 22, 51)]
    [InlineData(0xab7b, 23, 51)]
    [InlineData(0xab8b, 24, 51)]
    [InlineData(0xab9b, 25, 51)]
    [InlineData(0xabab, 26, 51)]
    [InlineData(0xabbb, 27, 51)]
    [InlineData(0xabeb, 30, 51)]
    [InlineData(0xabfb, 31, 51)]

    [InlineData(0xaa0c, 00, 52)]
    [InlineData(0xaa1c, 01, 52)]
    [InlineData(0xaa2c, 02, 52)]
    [InlineData(0xaa3c, 03, 52)]
    [InlineData(0xaa4c, 04, 52)]
    [InlineData(0xaa5c, 05, 52)]
    [InlineData(0xaa6c, 06, 52)]
    [InlineData(0xaa7c, 07, 52)]
    [InlineData(0xaa8c, 08, 52)]
    [InlineData(0xaa9c, 09, 52)]
    [InlineData(0xaaac, 10, 52)]
    [InlineData(0xaabc, 11, 52)]
    [InlineData(0xaacc, 12, 52)]
    [InlineData(0xaadc, 13, 52)]
    [InlineData(0xaaec, 14, 52)]
    [InlineData(0xaafc, 15, 52)]
    [InlineData(0xab0c, 16, 52)]
    [InlineData(0xab1c, 17, 52)]
    [InlineData(0xab2c, 18, 52)]
    [InlineData(0xab3c, 19, 52)]
    [InlineData(0xab4c, 20, 52)]
    [InlineData(0xab5c, 21, 52)]
    [InlineData(0xab6c, 22, 52)]
    [InlineData(0xab7c, 23, 52)]
    [InlineData(0xab8c, 24, 52)]
    [InlineData(0xab9c, 25, 52)]
    [InlineData(0xabac, 26, 52)]
    [InlineData(0xabbc, 27, 52)]
    [InlineData(0xabec, 30, 52)]
    [InlineData(0xabfc, 31, 52)]

    [InlineData(0xaa0d, 00, 53)]
    [InlineData(0xaa1d, 01, 53)]
    [InlineData(0xaa2d, 02, 53)]
    [InlineData(0xaa3d, 03, 53)]
    [InlineData(0xaa4d, 04, 53)]
    [InlineData(0xaa5d, 05, 53)]
    [InlineData(0xaa6d, 06, 53)]
    [InlineData(0xaa7d, 07, 53)]
    [InlineData(0xaa8d, 08, 53)]
    [InlineData(0xaa9d, 09, 53)]
    [InlineData(0xaaad, 10, 53)]
    [InlineData(0xaabd, 11, 53)]
    [InlineData(0xaacd, 12, 53)]
    [InlineData(0xaadd, 13, 53)]
    [InlineData(0xaaed, 14, 53)]
    [InlineData(0xaafd, 15, 53)]
    [InlineData(0xab0d, 16, 53)]
    [InlineData(0xab1d, 17, 53)]
    [InlineData(0xab2d, 18, 53)]
    [InlineData(0xab3d, 19, 53)]
    [InlineData(0xab4d, 20, 53)]
    [InlineData(0xab5d, 21, 53)]
    [InlineData(0xab6d, 22, 53)]
    [InlineData(0xab7d, 23, 53)]
    [InlineData(0xab8d, 24, 53)]
    [InlineData(0xab9d, 25, 53)]
    [InlineData(0xabad, 26, 53)]
    [InlineData(0xabbd, 27, 53)]
    [InlineData(0xabed, 30, 53)]
    [InlineData(0xabfd, 31, 53)]

    [InlineData(0xaa0e, 00, 54)]
    [InlineData(0xaa1e, 01, 54)]
    [InlineData(0xaa2e, 02, 54)]
    [InlineData(0xaa3e, 03, 54)]
    [InlineData(0xaa4e, 04, 54)]
    [InlineData(0xaa5e, 05, 54)]
    [InlineData(0xaa6e, 06, 54)]
    [InlineData(0xaa7e, 07, 54)]
    [InlineData(0xaa8e, 08, 54)]
    [InlineData(0xaa9e, 09, 54)]
    [InlineData(0xaaae, 10, 54)]
    [InlineData(0xaabe, 11, 54)]
    [InlineData(0xaace, 12, 54)]
    [InlineData(0xaade, 13, 54)]
    [InlineData(0xaaee, 14, 54)]
    [InlineData(0xaafe, 15, 54)]
    [InlineData(0xab0e, 16, 54)]
    [InlineData(0xab1e, 17, 54)]
    [InlineData(0xab2e, 18, 54)]
    [InlineData(0xab3e, 19, 54)]
    [InlineData(0xab4e, 20, 54)]
    [InlineData(0xab5e, 21, 54)]
    [InlineData(0xab6e, 22, 54)]
    [InlineData(0xab7e, 23, 54)]
    [InlineData(0xab8e, 24, 54)]
    [InlineData(0xab9e, 25, 54)]
    [InlineData(0xabae, 26, 54)]
    [InlineData(0xabbe, 27, 54)]
    [InlineData(0xabee, 30, 54)]
    [InlineData(0xabfe, 31, 54)]

    [InlineData(0xaa0f, 00, 55)]
    [InlineData(0xaa1f, 01, 55)]
    [InlineData(0xaa2f, 02, 55)]
    [InlineData(0xaa3f, 03, 55)]
    [InlineData(0xaa4f, 04, 55)]
    [InlineData(0xaa5f, 05, 55)]
    [InlineData(0xaa6f, 06, 55)]
    [InlineData(0xaa7f, 07, 55)]
    [InlineData(0xaa8f, 08, 55)]
    [InlineData(0xaa9f, 09, 55)]
    [InlineData(0xaaaf, 10, 55)]
    [InlineData(0xaabf, 11, 55)]
    [InlineData(0xaacf, 12, 55)]
    [InlineData(0xaadf, 13, 55)]
    [InlineData(0xaaef, 14, 55)]
    [InlineData(0xaaff, 15, 55)]
    [InlineData(0xab0f, 16, 55)]
    [InlineData(0xab1f, 17, 55)]
    [InlineData(0xab2f, 18, 55)]
    [InlineData(0xab3f, 19, 55)]
    [InlineData(0xab4f, 20, 55)]
    [InlineData(0xab5f, 21, 55)]
    [InlineData(0xab6f, 22, 55)]
    [InlineData(0xab7f, 23, 55)]
    [InlineData(0xab8f, 24, 55)]
    [InlineData(0xab9f, 25, 55)]
    [InlineData(0xabaf, 26, 55)]
    [InlineData(0xabbf, 27, 55)]
    [InlineData(0xabef, 30, 55)]
    [InlineData(0xabff, 31, 55)]

    [InlineData(0xae08, 00, 56)]
    [InlineData(0xae18, 01, 56)]
    [InlineData(0xae28, 02, 56)]
    [InlineData(0xae38, 03, 56)]
    [InlineData(0xae48, 04, 56)]
    [InlineData(0xae58, 05, 56)]
    [InlineData(0xae68, 06, 56)]
    [InlineData(0xae78, 07, 56)]
    [InlineData(0xae88, 08, 56)]
    [InlineData(0xae98, 09, 56)]
    [InlineData(0xaea8, 10, 56)]
    [InlineData(0xaeb8, 11, 56)]
    [InlineData(0xaec8, 12, 56)]
    [InlineData(0xaed8, 13, 56)]
    [InlineData(0xaee8, 14, 56)]
    [InlineData(0xaef8, 15, 56)]
    [InlineData(0xaf08, 16, 56)]
    [InlineData(0xaf18, 17, 56)]
    [InlineData(0xaf28, 18, 56)]
    [InlineData(0xaf38, 19, 56)]
    [InlineData(0xaf48, 20, 56)]
    [InlineData(0xaf58, 21, 56)]
    [InlineData(0xaf68, 22, 56)]
    [InlineData(0xaf78, 23, 56)]
    [InlineData(0xaf88, 24, 56)]
    [InlineData(0xaf98, 25, 56)]
    [InlineData(0xafa8, 26, 56)]
    [InlineData(0xafb8, 27, 56)]
    [InlineData(0xafe8, 30, 56)]
    [InlineData(0xaff8, 31, 56)]

    [InlineData(0xae09, 00, 57)]
    [InlineData(0xae19, 01, 57)]
    [InlineData(0xae29, 02, 57)]
    [InlineData(0xae39, 03, 57)]
    [InlineData(0xae49, 04, 57)]
    [InlineData(0xae59, 05, 57)]
    [InlineData(0xae69, 06, 57)]
    [InlineData(0xae79, 07, 57)]
    [InlineData(0xae89, 08, 57)]
    [InlineData(0xae99, 09, 57)]
    [InlineData(0xaea9, 10, 57)]
    [InlineData(0xaeb9, 11, 57)]
    [InlineData(0xaec9, 12, 57)]
    [InlineData(0xaed9, 13, 57)]
    [InlineData(0xaee9, 14, 57)]
    [InlineData(0xaef9, 15, 57)]
    [InlineData(0xaf09, 16, 57)]
    [InlineData(0xaf19, 17, 57)]
    [InlineData(0xaf29, 18, 57)]
    [InlineData(0xaf39, 19, 57)]
    [InlineData(0xaf49, 20, 57)]
    [InlineData(0xaf59, 21, 57)]
    [InlineData(0xaf69, 22, 57)]
    [InlineData(0xaf79, 23, 57)]
    [InlineData(0xaf89, 24, 57)]
    [InlineData(0xaf99, 25, 57)]
    [InlineData(0xafa9, 26, 57)]
    [InlineData(0xafb9, 27, 57)]
    [InlineData(0xafe9, 30, 57)]
    [InlineData(0xaff9, 31, 57)]

    [InlineData(0xae0a, 00, 58)]
    [InlineData(0xae1a, 01, 58)]
    [InlineData(0xae2a, 02, 58)]
    [InlineData(0xae3a, 03, 58)]
    [InlineData(0xae4a, 04, 58)]
    [InlineData(0xae5a, 05, 58)]
    [InlineData(0xae6a, 06, 58)]
    [InlineData(0xae7a, 07, 58)]
    [InlineData(0xae8a, 08, 58)]
    [InlineData(0xae9a, 09, 58)]
    [InlineData(0xaeaa, 10, 58)]
    [InlineData(0xaeba, 11, 58)]
    [InlineData(0xaeca, 12, 58)]
    [InlineData(0xaeda, 13, 58)]
    [InlineData(0xaeea, 14, 58)]
    [InlineData(0xaefa, 15, 58)]
    [InlineData(0xaf0a, 16, 58)]
    [InlineData(0xaf1a, 17, 58)]
    [InlineData(0xaf2a, 18, 58)]
    [InlineData(0xaf3a, 19, 58)]
    [InlineData(0xaf4a, 20, 58)]
    [InlineData(0xaf5a, 21, 58)]
    [InlineData(0xaf6a, 22, 58)]
    [InlineData(0xaf7a, 23, 58)]
    [InlineData(0xaf8a, 24, 58)]
    [InlineData(0xaf9a, 25, 58)]
    [InlineData(0xafaa, 26, 58)]
    [InlineData(0xafba, 27, 58)]
    [InlineData(0xafea, 30, 58)]
    [InlineData(0xaffa, 31, 58)]

    [InlineData(0xae0b, 00, 59)]
    [InlineData(0xae1b, 01, 59)]
    [InlineData(0xae2b, 02, 59)]
    [InlineData(0xae3b, 03, 59)]
    [InlineData(0xae4b, 04, 59)]
    [InlineData(0xae5b, 05, 59)]
    [InlineData(0xae6b, 06, 59)]
    [InlineData(0xae7b, 07, 59)]
    [InlineData(0xae8b, 08, 59)]
    [InlineData(0xae9b, 09, 59)]
    [InlineData(0xaeab, 10, 59)]
    [InlineData(0xaebb, 11, 59)]
    [InlineData(0xaecb, 12, 59)]
    [InlineData(0xaedb, 13, 59)]
    [InlineData(0xaeeb, 14, 59)]
    [InlineData(0xaefb, 15, 59)]
    [InlineData(0xaf0b, 16, 59)]
    [InlineData(0xaf1b, 17, 59)]
    [InlineData(0xaf2b, 18, 59)]
    [InlineData(0xaf3b, 19, 59)]
    [InlineData(0xaf4b, 20, 59)]
    [InlineData(0xaf5b, 21, 59)]
    [InlineData(0xaf6b, 22, 59)]
    [InlineData(0xaf7b, 23, 59)]
    [InlineData(0xaf8b, 24, 59)]
    [InlineData(0xaf9b, 25, 59)]
    [InlineData(0xafab, 26, 59)]
    [InlineData(0xafbb, 27, 59)]
    [InlineData(0xafeb, 30, 59)]
    [InlineData(0xaffb, 31, 59)]

    [InlineData(0xae0c, 00, 60)]
    [InlineData(0xae1c, 01, 60)]
    [InlineData(0xae2c, 02, 60)]
    [InlineData(0xae3c, 03, 60)]
    [InlineData(0xae4c, 04, 60)]
    [InlineData(0xae5c, 05, 60)]
    [InlineData(0xae6c, 06, 60)]
    [InlineData(0xae7c, 07, 60)]
    [InlineData(0xae8c, 08, 60)]
    [InlineData(0xae9c, 09, 60)]
    [InlineData(0xaeac, 10, 60)]
    [InlineData(0xaebc, 11, 60)]
    [InlineData(0xaecc, 12, 60)]
    [InlineData(0xaedc, 13, 60)]
    [InlineData(0xaeec, 14, 60)]
    [InlineData(0xaefc, 15, 60)]
    [InlineData(0xaf0c, 16, 60)]
    [InlineData(0xaf1c, 17, 60)]
    [InlineData(0xaf2c, 18, 60)]
    [InlineData(0xaf3c, 19, 60)]
    [InlineData(0xaf4c, 20, 60)]
    [InlineData(0xaf5c, 21, 60)]
    [InlineData(0xaf6c, 22, 60)]
    [InlineData(0xaf7c, 23, 60)]
    [InlineData(0xaf8c, 24, 60)]
    [InlineData(0xaf9c, 25, 60)]
    [InlineData(0xafac, 26, 60)]
    [InlineData(0xafbc, 27, 60)]
    [InlineData(0xafec, 30, 60)]
    [InlineData(0xaffc, 31, 60)]

    [InlineData(0xae0d, 00, 61)]
    [InlineData(0xae1d, 01, 61)]
    [InlineData(0xae2d, 02, 61)]
    [InlineData(0xae3d, 03, 61)]
    [InlineData(0xae4d, 04, 61)]
    [InlineData(0xae5d, 05, 61)]
    [InlineData(0xae6d, 06, 61)]
    [InlineData(0xae7d, 07, 61)]
    [InlineData(0xae8d, 08, 61)]
    [InlineData(0xae9d, 09, 61)]
    [InlineData(0xaead, 10, 61)]
    [InlineData(0xaebd, 11, 61)]
    [InlineData(0xaecd, 12, 61)]
    [InlineData(0xaedd, 13, 61)]
    [InlineData(0xaeed, 14, 61)]
    [InlineData(0xaefd, 15, 61)]
    [InlineData(0xaf0d, 16, 61)]
    [InlineData(0xaf1d, 17, 61)]
    [InlineData(0xaf2d, 18, 61)]
    [InlineData(0xaf3d, 19, 61)]
    [InlineData(0xaf4d, 20, 61)]
    [InlineData(0xaf5d, 21, 61)]
    [InlineData(0xaf6d, 22, 61)]
    [InlineData(0xaf7d, 23, 61)]
    [InlineData(0xaf8d, 24, 61)]
    [InlineData(0xaf9d, 25, 61)]
    [InlineData(0xafad, 26, 61)]
    [InlineData(0xafbd, 27, 61)]
    [InlineData(0xafed, 30, 61)]
    [InlineData(0xaffd, 31, 61)]

    [InlineData(0xae0e, 00, 62)]
    [InlineData(0xae1e, 01, 62)]
    [InlineData(0xae2e, 02, 62)]
    [InlineData(0xae3e, 03, 62)]
    [InlineData(0xae4e, 04, 62)]
    [InlineData(0xae5e, 05, 62)]
    [InlineData(0xae6e, 06, 62)]
    [InlineData(0xae7e, 07, 62)]
    [InlineData(0xae8e, 08, 62)]
    [InlineData(0xae9e, 09, 62)]
    [InlineData(0xaeae, 10, 62)]
    [InlineData(0xaebe, 11, 62)]
    [InlineData(0xaece, 12, 62)]
    [InlineData(0xaede, 13, 62)]
    [InlineData(0xaeee, 14, 62)]
    [InlineData(0xaefe, 15, 62)]
    [InlineData(0xaf0e, 16, 62)]
    [InlineData(0xaf1e, 17, 62)]
    [InlineData(0xaf2e, 18, 62)]
    [InlineData(0xaf3e, 19, 62)]
    [InlineData(0xaf4e, 20, 62)]
    [InlineData(0xaf5e, 21, 62)]
    [InlineData(0xaf6e, 22, 62)]
    [InlineData(0xaf7e, 23, 62)]
    [InlineData(0xaf8e, 24, 62)]
    [InlineData(0xaf9e, 25, 62)]
    [InlineData(0xafae, 26, 62)]
    [InlineData(0xafbe, 27, 62)]
    [InlineData(0xafee, 30, 62)]
    [InlineData(0xaffe, 31, 62)]
#endif
    [InlineData(0xae0f, 00, 63)]
    [InlineData(0xae1f, 01, 63)]
    [InlineData(0xae2f, 02, 63)]
    [InlineData(0xae3f, 03, 63)]
    [InlineData(0xae4f, 04, 63)]
    [InlineData(0xae5f, 05, 63)]
    [InlineData(0xae6f, 06, 63)]
    [InlineData(0xae7f, 07, 63)]
    [InlineData(0xae8f, 08, 63)]
    [InlineData(0xae9f, 09, 63)]
    [InlineData(0xaeaf, 10, 63)]
    [InlineData(0xaebf, 11, 63)]
    [InlineData(0xaecf, 12, 63)]
    [InlineData(0xaedf, 13, 63)]
    [InlineData(0xaeef, 14, 63)]
    [InlineData(0xaeff, 15, 63)]
    [InlineData(0xaf0f, 16, 63)]
    [InlineData(0xaf1f, 17, 63)]
    [InlineData(0xaf2f, 18, 63)]
    [InlineData(0xaf3f, 19, 63)]
    [InlineData(0xaf4f, 20, 63)]
    [InlineData(0xaf5f, 21, 63)]
    [InlineData(0xaf6f, 22, 63)]
    [InlineData(0xaf7f, 23, 63)]
    [InlineData(0xaf8f, 24, 63)]
    [InlineData(0xaf9f, 25, 63)]
    [InlineData(0xafaf, 26, 63)]
    [InlineData(0xafbf, 27, 63)]
    [InlineData(0xafef, 30, 63)]
    [InlineData(0xafff, 31, 63)]
    #endregion
    public void DecodeInstruction_STD_Y_test(ushort opcode, int r, int q)
    {
        string Mnemonics = $"STD Y+{q}, r{r}";
        ushort address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address + q] = 0;
        _cpu.Y = address;
        _cpu.r[r] = val;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _ram.RAM[address + q]);
        Assert.Equal(101, _cpu.PC);
    }

    [Theory]
#region test data
    [InlineData(0x8201, 00, 01)]
    [InlineData(0x8211, 01, 01)]
    [InlineData(0x8221, 02, 01)]
    [InlineData(0x8231, 03, 01)]
    [InlineData(0x8241, 04, 01)]
    [InlineData(0x8251, 05, 01)]
    [InlineData(0x8261, 06, 01)]
    [InlineData(0x8271, 07, 01)]
    [InlineData(0x8281, 08, 01)]
    [InlineData(0x8291, 09, 01)]
    [InlineData(0x82a1, 10, 01)]
    [InlineData(0x82b1, 11, 01)]
    [InlineData(0x82c1, 12, 01)]
    [InlineData(0x82d1, 13, 01)]
    [InlineData(0x82e1, 14, 01)]
    [InlineData(0x82f1, 15, 01)]
    [InlineData(0x8301, 16, 01)]
    [InlineData(0x8311, 17, 01)]
    [InlineData(0x8321, 18, 01)]
    [InlineData(0x8331, 19, 01)]
    [InlineData(0x8341, 20, 01)]
    [InlineData(0x8351, 21, 01)]
    [InlineData(0x8361, 22, 01)]
    [InlineData(0x8371, 23, 01)]
    [InlineData(0x8381, 24, 01)]
    [InlineData(0x8391, 25, 01)]
    [InlineData(0x83a1, 26, 01)]
    [InlineData(0x83b1, 27, 01)]
    [InlineData(0x83c1, 28, 01)]
    [InlineData(0x83d1, 29, 01)]
#if RequiredHavyTest
    [InlineData(0x8202, 00, 02)]
    [InlineData(0x8212, 01, 02)]
    [InlineData(0x8222, 02, 02)]
    [InlineData(0x8232, 03, 02)]
    [InlineData(0x8242, 04, 02)]
    [InlineData(0x8252, 05, 02)]
    [InlineData(0x8262, 06, 02)]
    [InlineData(0x8272, 07, 02)]
    [InlineData(0x8282, 08, 02)]
    [InlineData(0x8292, 09, 02)]
    [InlineData(0x82a2, 10, 02)]
    [InlineData(0x82b2, 11, 02)]
    [InlineData(0x82c2, 12, 02)]
    [InlineData(0x82d2, 13, 02)]
    [InlineData(0x82e2, 14, 02)]
    [InlineData(0x82f2, 15, 02)]
    [InlineData(0x8302, 16, 02)]
    [InlineData(0x8312, 17, 02)]
    [InlineData(0x8322, 18, 02)]
    [InlineData(0x8332, 19, 02)]
    [InlineData(0x8342, 20, 02)]
    [InlineData(0x8352, 21, 02)]
    [InlineData(0x8362, 22, 02)]
    [InlineData(0x8372, 23, 02)]
    [InlineData(0x8382, 24, 02)]
    [InlineData(0x8392, 25, 02)]
    [InlineData(0x83a2, 26, 02)]
    [InlineData(0x83b2, 27, 02)]
    [InlineData(0x83c2, 28, 02)]
    [InlineData(0x83d2, 29, 02)]

    [InlineData(0x8203, 00, 03)]
    [InlineData(0x8213, 01, 03)]
    [InlineData(0x8223, 02, 03)]
    [InlineData(0x8233, 03, 03)]
    [InlineData(0x8243, 04, 03)]
    [InlineData(0x8253, 05, 03)]
    [InlineData(0x8263, 06, 03)]
    [InlineData(0x8273, 07, 03)]
    [InlineData(0x8283, 08, 03)]
    [InlineData(0x8293, 09, 03)]
    [InlineData(0x82a3, 10, 03)]
    [InlineData(0x82b3, 11, 03)]
    [InlineData(0x82c3, 12, 03)]
    [InlineData(0x82d3, 13, 03)]
    [InlineData(0x82e3, 14, 03)]
    [InlineData(0x82f3, 15, 03)]
    [InlineData(0x8303, 16, 03)]
    [InlineData(0x8313, 17, 03)]
    [InlineData(0x8323, 18, 03)]
    [InlineData(0x8333, 19, 03)]
    [InlineData(0x8343, 20, 03)]
    [InlineData(0x8353, 21, 03)]
    [InlineData(0x8363, 22, 03)]
    [InlineData(0x8373, 23, 03)]
    [InlineData(0x8383, 24, 03)]
    [InlineData(0x8393, 25, 03)]
    [InlineData(0x83a3, 26, 03)]
    [InlineData(0x83b3, 27, 03)]
    [InlineData(0x83c3, 28, 03)]
    [InlineData(0x83d3, 29, 03)]

    [InlineData(0x8204, 00, 04)]
    [InlineData(0x8214, 01, 04)]
    [InlineData(0x8224, 02, 04)]
    [InlineData(0x8234, 03, 04)]
    [InlineData(0x8244, 04, 04)]
    [InlineData(0x8254, 05, 04)]
    [InlineData(0x8264, 06, 04)]
    [InlineData(0x8274, 07, 04)]
    [InlineData(0x8284, 08, 04)]
    [InlineData(0x8294, 09, 04)]
    [InlineData(0x82a4, 10, 04)]
    [InlineData(0x82b4, 11, 04)]
    [InlineData(0x82c4, 12, 04)]
    [InlineData(0x82d4, 13, 04)]
    [InlineData(0x82e4, 14, 04)]
    [InlineData(0x82f4, 15, 04)]
    [InlineData(0x8304, 16, 04)]
    [InlineData(0x8314, 17, 04)]
    [InlineData(0x8324, 18, 04)]
    [InlineData(0x8334, 19, 04)]
    [InlineData(0x8344, 20, 04)]
    [InlineData(0x8354, 21, 04)]
    [InlineData(0x8364, 22, 04)]
    [InlineData(0x8374, 23, 04)]
    [InlineData(0x8384, 24, 04)]
    [InlineData(0x8394, 25, 04)]
    [InlineData(0x83a4, 26, 04)]
    [InlineData(0x83b4, 27, 04)]
    [InlineData(0x83c4, 28, 04)]
    [InlineData(0x83d4, 29, 04)]

    [InlineData(0x8205, 00, 05)]
    [InlineData(0x8215, 01, 05)]
    [InlineData(0x8225, 02, 05)]
    [InlineData(0x8235, 03, 05)]
    [InlineData(0x8245, 04, 05)]
    [InlineData(0x8255, 05, 05)]
    [InlineData(0x8265, 06, 05)]
    [InlineData(0x8275, 07, 05)]
    [InlineData(0x8285, 08, 05)]
    [InlineData(0x8295, 09, 05)]
    [InlineData(0x82a5, 10, 05)]
    [InlineData(0x82b5, 11, 05)]
    [InlineData(0x82c5, 12, 05)]
    [InlineData(0x82d5, 13, 05)]
    [InlineData(0x82e5, 14, 05)]
    [InlineData(0x82f5, 15, 05)]
    [InlineData(0x8305, 16, 05)]
    [InlineData(0x8315, 17, 05)]
    [InlineData(0x8325, 18, 05)]
    [InlineData(0x8335, 19, 05)]
    [InlineData(0x8345, 20, 05)]
    [InlineData(0x8355, 21, 05)]
    [InlineData(0x8365, 22, 05)]
    [InlineData(0x8375, 23, 05)]
    [InlineData(0x8385, 24, 05)]
    [InlineData(0x8395, 25, 05)]
    [InlineData(0x83a5, 26, 05)]
    [InlineData(0x83b5, 27, 05)]
    [InlineData(0x83c5, 28, 05)]
    [InlineData(0x83d5, 29, 05)]

    [InlineData(0x8206, 00, 06)]
    [InlineData(0x8216, 01, 06)]
    [InlineData(0x8226, 02, 06)]
    [InlineData(0x8236, 03, 06)]
    [InlineData(0x8246, 04, 06)]
    [InlineData(0x8256, 05, 06)]
    [InlineData(0x8266, 06, 06)]
    [InlineData(0x8276, 07, 06)]
    [InlineData(0x8286, 08, 06)]
    [InlineData(0x8296, 09, 06)]
    [InlineData(0x82a6, 10, 06)]
    [InlineData(0x82b6, 11, 06)]
    [InlineData(0x82c6, 12, 06)]
    [InlineData(0x82d6, 13, 06)]
    [InlineData(0x82e6, 14, 06)]
    [InlineData(0x82f6, 15, 06)]
    [InlineData(0x8306, 16, 06)]
    [InlineData(0x8316, 17, 06)]
    [InlineData(0x8326, 18, 06)]
    [InlineData(0x8336, 19, 06)]
    [InlineData(0x8346, 20, 06)]
    [InlineData(0x8356, 21, 06)]
    [InlineData(0x8366, 22, 06)]
    [InlineData(0x8376, 23, 06)]
    [InlineData(0x8386, 24, 06)]
    [InlineData(0x8396, 25, 06)]
    [InlineData(0x83a6, 26, 06)]
    [InlineData(0x83b6, 27, 06)]
    [InlineData(0x83c6, 28, 06)]
    [InlineData(0x83d6, 29, 06)]

    [InlineData(0x8207, 00, 07)]
    [InlineData(0x8217, 01, 07)]
    [InlineData(0x8227, 02, 07)]
    [InlineData(0x8237, 03, 07)]
    [InlineData(0x8247, 04, 07)]
    [InlineData(0x8257, 05, 07)]
    [InlineData(0x8267, 06, 07)]
    [InlineData(0x8277, 07, 07)]
    [InlineData(0x8287, 08, 07)]
    [InlineData(0x8297, 09, 07)]
    [InlineData(0x82a7, 10, 07)]
    [InlineData(0x82b7, 11, 07)]
    [InlineData(0x82c7, 12, 07)]
    [InlineData(0x82d7, 13, 07)]
    [InlineData(0x82e7, 14, 07)]
    [InlineData(0x82f7, 15, 07)]
    [InlineData(0x8307, 16, 07)]
    [InlineData(0x8317, 17, 07)]
    [InlineData(0x8327, 18, 07)]
    [InlineData(0x8337, 19, 07)]
    [InlineData(0x8347, 20, 07)]
    [InlineData(0x8357, 21, 07)]
    [InlineData(0x8367, 22, 07)]
    [InlineData(0x8377, 23, 07)]
    [InlineData(0x8387, 24, 07)]
    [InlineData(0x8397, 25, 07)]
    [InlineData(0x83a7, 26, 07)]
    [InlineData(0x83b7, 27, 07)]
    [InlineData(0x83c7, 28, 07)]
    [InlineData(0x83d7, 29, 07)]

    [InlineData(0x8600, 00, 08)]
    [InlineData(0x8610, 01, 08)]
    [InlineData(0x8620, 02, 08)]
    [InlineData(0x8630, 03, 08)]
    [InlineData(0x8640, 04, 08)]
    [InlineData(0x8650, 05, 08)]
    [InlineData(0x8660, 06, 08)]
    [InlineData(0x8670, 07, 08)]
    [InlineData(0x8680, 08, 08)]
    [InlineData(0x8690, 09, 08)]
    [InlineData(0x86a0, 10, 08)]
    [InlineData(0x86b0, 11, 08)]
    [InlineData(0x86c0, 12, 08)]
    [InlineData(0x86d0, 13, 08)]
    [InlineData(0x86e0, 14, 08)]
    [InlineData(0x86f0, 15, 08)]
    [InlineData(0x8700, 16, 08)]
    [InlineData(0x8710, 17, 08)]
    [InlineData(0x8720, 18, 08)]
    [InlineData(0x8730, 19, 08)]
    [InlineData(0x8740, 20, 08)]
    [InlineData(0x8750, 21, 08)]
    [InlineData(0x8760, 22, 08)]
    [InlineData(0x8770, 23, 08)]
    [InlineData(0x8780, 24, 08)]
    [InlineData(0x8790, 25, 08)]
    [InlineData(0x87a0, 26, 08)]
    [InlineData(0x87b0, 27, 08)]
    [InlineData(0x87c0, 28, 08)]
    [InlineData(0x87d0, 29, 08)]

    [InlineData(0x8601, 00, 09)]
    [InlineData(0x8611, 01, 09)]
    [InlineData(0x8621, 02, 09)]
    [InlineData(0x8631, 03, 09)]
    [InlineData(0x8641, 04, 09)]
    [InlineData(0x8651, 05, 09)]
    [InlineData(0x8661, 06, 09)]
    [InlineData(0x8671, 07, 09)]
    [InlineData(0x8681, 08, 09)]
    [InlineData(0x8691, 09, 09)]
    [InlineData(0x86a1, 10, 09)]
    [InlineData(0x86b1, 11, 09)]
    [InlineData(0x86c1, 12, 09)]
    [InlineData(0x86d1, 13, 09)]
    [InlineData(0x86e1, 14, 09)]
    [InlineData(0x86f1, 15, 09)]
    [InlineData(0x8701, 16, 09)]
    [InlineData(0x8711, 17, 09)]
    [InlineData(0x8721, 18, 09)]
    [InlineData(0x8731, 19, 09)]
    [InlineData(0x8741, 20, 09)]
    [InlineData(0x8751, 21, 09)]
    [InlineData(0x8761, 22, 09)]
    [InlineData(0x8771, 23, 09)]
    [InlineData(0x8781, 24, 09)]
    [InlineData(0x8791, 25, 09)]
    [InlineData(0x87a1, 26, 09)]
    [InlineData(0x87b1, 27, 09)]
    [InlineData(0x87c1, 28, 09)]
    [InlineData(0x87d1, 29, 09)]

    [InlineData(0x8602, 00, 10)]
    [InlineData(0x8612, 01, 10)]
    [InlineData(0x8622, 02, 10)]
    [InlineData(0x8632, 03, 10)]
    [InlineData(0x8642, 04, 10)]
    [InlineData(0x8652, 05, 10)]
    [InlineData(0x8662, 06, 10)]
    [InlineData(0x8672, 07, 10)]
    [InlineData(0x8682, 08, 10)]
    [InlineData(0x8692, 09, 10)]
    [InlineData(0x86a2, 10, 10)]
    [InlineData(0x86b2, 11, 10)]
    [InlineData(0x86c2, 12, 10)]
    [InlineData(0x86d2, 13, 10)]
    [InlineData(0x86e2, 14, 10)]
    [InlineData(0x86f2, 15, 10)]
    [InlineData(0x8702, 16, 10)]
    [InlineData(0x8712, 17, 10)]
    [InlineData(0x8722, 18, 10)]
    [InlineData(0x8732, 19, 10)]
    [InlineData(0x8742, 20, 10)]
    [InlineData(0x8752, 21, 10)]
    [InlineData(0x8762, 22, 10)]
    [InlineData(0x8772, 23, 10)]
    [InlineData(0x8782, 24, 10)]
    [InlineData(0x8792, 25, 10)]
    [InlineData(0x87a2, 26, 10)]
    [InlineData(0x87b2, 27, 10)]
    [InlineData(0x87c2, 28, 10)]
    [InlineData(0x87d2, 29, 10)]

    [InlineData(0x8603, 00, 11)]
    [InlineData(0x8613, 01, 11)]
    [InlineData(0x8623, 02, 11)]
    [InlineData(0x8633, 03, 11)]
    [InlineData(0x8643, 04, 11)]
    [InlineData(0x8653, 05, 11)]
    [InlineData(0x8663, 06, 11)]
    [InlineData(0x8673, 07, 11)]
    [InlineData(0x8683, 08, 11)]
    [InlineData(0x8693, 09, 11)]
    [InlineData(0x86a3, 10, 11)]
    [InlineData(0x86b3, 11, 11)]
    [InlineData(0x86c3, 12, 11)]
    [InlineData(0x86d3, 13, 11)]
    [InlineData(0x86e3, 14, 11)]
    [InlineData(0x86f3, 15, 11)]
    [InlineData(0x8703, 16, 11)]
    [InlineData(0x8713, 17, 11)]
    [InlineData(0x8723, 18, 11)]
    [InlineData(0x8733, 19, 11)]
    [InlineData(0x8743, 20, 11)]
    [InlineData(0x8753, 21, 11)]
    [InlineData(0x8763, 22, 11)]
    [InlineData(0x8773, 23, 11)]
    [InlineData(0x8783, 24, 11)]
    [InlineData(0x8793, 25, 11)]
    [InlineData(0x87a3, 26, 11)]
    [InlineData(0x87b3, 27, 11)]
    [InlineData(0x87c3, 28, 11)]
    [InlineData(0x87d3, 29, 11)]

    [InlineData(0x8604, 00, 12)]
    [InlineData(0x8614, 01, 12)]
    [InlineData(0x8624, 02, 12)]
    [InlineData(0x8634, 03, 12)]
    [InlineData(0x8644, 04, 12)]
    [InlineData(0x8654, 05, 12)]
    [InlineData(0x8664, 06, 12)]
    [InlineData(0x8674, 07, 12)]
    [InlineData(0x8684, 08, 12)]
    [InlineData(0x8694, 09, 12)]
    [InlineData(0x86a4, 10, 12)]
    [InlineData(0x86b4, 11, 12)]
    [InlineData(0x86c4, 12, 12)]
    [InlineData(0x86d4, 13, 12)]
    [InlineData(0x86e4, 14, 12)]
    [InlineData(0x86f4, 15, 12)]
    [InlineData(0x8704, 16, 12)]
    [InlineData(0x8714, 17, 12)]
    [InlineData(0x8724, 18, 12)]
    [InlineData(0x8734, 19, 12)]
    [InlineData(0x8744, 20, 12)]
    [InlineData(0x8754, 21, 12)]
    [InlineData(0x8764, 22, 12)]
    [InlineData(0x8774, 23, 12)]
    [InlineData(0x8784, 24, 12)]
    [InlineData(0x8794, 25, 12)]
    [InlineData(0x87a4, 26, 12)]
    [InlineData(0x87b4, 27, 12)]
    [InlineData(0x87c4, 28, 12)]
    [InlineData(0x87d4, 29, 12)]

    [InlineData(0x8605, 00, 13)]
    [InlineData(0x8615, 01, 13)]
    [InlineData(0x8625, 02, 13)]
    [InlineData(0x8635, 03, 13)]
    [InlineData(0x8645, 04, 13)]
    [InlineData(0x8655, 05, 13)]
    [InlineData(0x8665, 06, 13)]
    [InlineData(0x8675, 07, 13)]
    [InlineData(0x8685, 08, 13)]
    [InlineData(0x8695, 09, 13)]
    [InlineData(0x86a5, 10, 13)]
    [InlineData(0x86b5, 11, 13)]
    [InlineData(0x86c5, 12, 13)]
    [InlineData(0x86d5, 13, 13)]
    [InlineData(0x86e5, 14, 13)]
    [InlineData(0x86f5, 15, 13)]
    [InlineData(0x8705, 16, 13)]
    [InlineData(0x8715, 17, 13)]
    [InlineData(0x8725, 18, 13)]
    [InlineData(0x8735, 19, 13)]
    [InlineData(0x8745, 20, 13)]
    [InlineData(0x8755, 21, 13)]
    [InlineData(0x8765, 22, 13)]
    [InlineData(0x8775, 23, 13)]
    [InlineData(0x8785, 24, 13)]
    [InlineData(0x8795, 25, 13)]
    [InlineData(0x87a5, 26, 13)]
    [InlineData(0x87b5, 27, 13)]
    [InlineData(0x87c5, 28, 13)]
    [InlineData(0x87d5, 29, 13)]

    [InlineData(0x8606, 00, 14)]
    [InlineData(0x8616, 01, 14)]
    [InlineData(0x8626, 02, 14)]
    [InlineData(0x8636, 03, 14)]
    [InlineData(0x8646, 04, 14)]
    [InlineData(0x8656, 05, 14)]
    [InlineData(0x8666, 06, 14)]
    [InlineData(0x8676, 07, 14)]
    [InlineData(0x8686, 08, 14)]
    [InlineData(0x8696, 09, 14)]
    [InlineData(0x86a6, 10, 14)]
    [InlineData(0x86b6, 11, 14)]
    [InlineData(0x86c6, 12, 14)]
    [InlineData(0x86d6, 13, 14)]
    [InlineData(0x86e6, 14, 14)]
    [InlineData(0x86f6, 15, 14)]
    [InlineData(0x8706, 16, 14)]
    [InlineData(0x8716, 17, 14)]
    [InlineData(0x8726, 18, 14)]
    [InlineData(0x8736, 19, 14)]
    [InlineData(0x8746, 20, 14)]
    [InlineData(0x8756, 21, 14)]
    [InlineData(0x8766, 22, 14)]
    [InlineData(0x8776, 23, 14)]
    [InlineData(0x8786, 24, 14)]
    [InlineData(0x8796, 25, 14)]
    [InlineData(0x87a6, 26, 14)]
    [InlineData(0x87b6, 27, 14)]
    [InlineData(0x87c6, 28, 14)]
    [InlineData(0x87d6, 29, 14)]

    [InlineData(0x8607, 00, 15)]
    [InlineData(0x8617, 01, 15)]
    [InlineData(0x8627, 02, 15)]
    [InlineData(0x8637, 03, 15)]
    [InlineData(0x8647, 04, 15)]
    [InlineData(0x8657, 05, 15)]
    [InlineData(0x8667, 06, 15)]
    [InlineData(0x8677, 07, 15)]
    [InlineData(0x8687, 08, 15)]
    [InlineData(0x8697, 09, 15)]
    [InlineData(0x86a7, 10, 15)]
    [InlineData(0x86b7, 11, 15)]
    [InlineData(0x86c7, 12, 15)]
    [InlineData(0x86d7, 13, 15)]
    [InlineData(0x86e7, 14, 15)]
    [InlineData(0x86f7, 15, 15)]
    [InlineData(0x8707, 16, 15)]
    [InlineData(0x8717, 17, 15)]
    [InlineData(0x8727, 18, 15)]
    [InlineData(0x8737, 19, 15)]
    [InlineData(0x8747, 20, 15)]
    [InlineData(0x8757, 21, 15)]
    [InlineData(0x8767, 22, 15)]
    [InlineData(0x8777, 23, 15)]
    [InlineData(0x8787, 24, 15)]
    [InlineData(0x8797, 25, 15)]
    [InlineData(0x87a7, 26, 15)]
    [InlineData(0x87b7, 27, 15)]
    [InlineData(0x87c7, 28, 15)]
    [InlineData(0x87d7, 29, 15)]

    [InlineData(0x8a00, 00, 16)]
    [InlineData(0x8a10, 01, 16)]
    [InlineData(0x8a20, 02, 16)]
    [InlineData(0x8a30, 03, 16)]
    [InlineData(0x8a40, 04, 16)]
    [InlineData(0x8a50, 05, 16)]
    [InlineData(0x8a60, 06, 16)]
    [InlineData(0x8a70, 07, 16)]
    [InlineData(0x8a80, 08, 16)]
    [InlineData(0x8a90, 09, 16)]
    [InlineData(0x8aa0, 10, 16)]
    [InlineData(0x8ab0, 11, 16)]
    [InlineData(0x8ac0, 12, 16)]
    [InlineData(0x8ad0, 13, 16)]
    [InlineData(0x8ae0, 14, 16)]
    [InlineData(0x8af0, 15, 16)]
    [InlineData(0x8b00, 16, 16)]
    [InlineData(0x8b10, 17, 16)]
    [InlineData(0x8b20, 18, 16)]
    [InlineData(0x8b30, 19, 16)]
    [InlineData(0x8b40, 20, 16)]
    [InlineData(0x8b50, 21, 16)]
    [InlineData(0x8b60, 22, 16)]
    [InlineData(0x8b70, 23, 16)]
    [InlineData(0x8b80, 24, 16)]
    [InlineData(0x8b90, 25, 16)]
    [InlineData(0x8ba0, 26, 16)]
    [InlineData(0x8bb0, 27, 16)]
    [InlineData(0x8bc0, 28, 16)]
    [InlineData(0x8bd0, 29, 16)]

    [InlineData(0x8a01, 00, 17)]
    [InlineData(0x8a11, 01, 17)]
    [InlineData(0x8a21, 02, 17)]
    [InlineData(0x8a31, 03, 17)]
    [InlineData(0x8a41, 04, 17)]
    [InlineData(0x8a51, 05, 17)]
    [InlineData(0x8a61, 06, 17)]
    [InlineData(0x8a71, 07, 17)]
    [InlineData(0x8a81, 08, 17)]
    [InlineData(0x8a91, 09, 17)]
    [InlineData(0x8aa1, 10, 17)]
    [InlineData(0x8ab1, 11, 17)]
    [InlineData(0x8ac1, 12, 17)]
    [InlineData(0x8ad1, 13, 17)]
    [InlineData(0x8ae1, 14, 17)]
    [InlineData(0x8af1, 15, 17)]
    [InlineData(0x8b01, 16, 17)]
    [InlineData(0x8b11, 17, 17)]
    [InlineData(0x8b21, 18, 17)]
    [InlineData(0x8b31, 19, 17)]
    [InlineData(0x8b41, 20, 17)]
    [InlineData(0x8b51, 21, 17)]
    [InlineData(0x8b61, 22, 17)]
    [InlineData(0x8b71, 23, 17)]
    [InlineData(0x8b81, 24, 17)]
    [InlineData(0x8b91, 25, 17)]
    [InlineData(0x8ba1, 26, 17)]
    [InlineData(0x8bb1, 27, 17)]
    [InlineData(0x8bc1, 28, 17)]
    [InlineData(0x8bd1, 29, 17)]

    [InlineData(0x8a02, 00, 18)]
    [InlineData(0x8a12, 01, 18)]
    [InlineData(0x8a22, 02, 18)]
    [InlineData(0x8a32, 03, 18)]
    [InlineData(0x8a42, 04, 18)]
    [InlineData(0x8a52, 05, 18)]
    [InlineData(0x8a62, 06, 18)]
    [InlineData(0x8a72, 07, 18)]
    [InlineData(0x8a82, 08, 18)]
    [InlineData(0x8a92, 09, 18)]
    [InlineData(0x8aa2, 10, 18)]
    [InlineData(0x8ab2, 11, 18)]
    [InlineData(0x8ac2, 12, 18)]
    [InlineData(0x8ad2, 13, 18)]
    [InlineData(0x8ae2, 14, 18)]
    [InlineData(0x8af2, 15, 18)]
    [InlineData(0x8b02, 16, 18)]
    [InlineData(0x8b12, 17, 18)]
    [InlineData(0x8b22, 18, 18)]
    [InlineData(0x8b32, 19, 18)]
    [InlineData(0x8b42, 20, 18)]
    [InlineData(0x8b52, 21, 18)]
    [InlineData(0x8b62, 22, 18)]
    [InlineData(0x8b72, 23, 18)]
    [InlineData(0x8b82, 24, 18)]
    [InlineData(0x8b92, 25, 18)]
    [InlineData(0x8ba2, 26, 18)]
    [InlineData(0x8bb2, 27, 18)]
    [InlineData(0x8bc2, 28, 18)]
    [InlineData(0x8bd2, 29, 18)]

    [InlineData(0x8a03, 00, 19)]
    [InlineData(0x8a13, 01, 19)]
    [InlineData(0x8a23, 02, 19)]
    [InlineData(0x8a33, 03, 19)]
    [InlineData(0x8a43, 04, 19)]
    [InlineData(0x8a53, 05, 19)]
    [InlineData(0x8a63, 06, 19)]
    [InlineData(0x8a73, 07, 19)]
    [InlineData(0x8a83, 08, 19)]
    [InlineData(0x8a93, 09, 19)]
    [InlineData(0x8aa3, 10, 19)]
    [InlineData(0x8ab3, 11, 19)]
    [InlineData(0x8ac3, 12, 19)]
    [InlineData(0x8ad3, 13, 19)]
    [InlineData(0x8ae3, 14, 19)]
    [InlineData(0x8af3, 15, 19)]
    [InlineData(0x8b03, 16, 19)]
    [InlineData(0x8b13, 17, 19)]
    [InlineData(0x8b23, 18, 19)]
    [InlineData(0x8b33, 19, 19)]
    [InlineData(0x8b43, 20, 19)]
    [InlineData(0x8b53, 21, 19)]
    [InlineData(0x8b63, 22, 19)]
    [InlineData(0x8b73, 23, 19)]
    [InlineData(0x8b83, 24, 19)]
    [InlineData(0x8b93, 25, 19)]
    [InlineData(0x8ba3, 26, 19)]
    [InlineData(0x8bb3, 27, 19)]
    [InlineData(0x8bc3, 28, 19)]
    [InlineData(0x8bd3, 29, 19)]

    [InlineData(0x8a04, 00, 20)]
    [InlineData(0x8a14, 01, 20)]
    [InlineData(0x8a24, 02, 20)]
    [InlineData(0x8a34, 03, 20)]
    [InlineData(0x8a44, 04, 20)]
    [InlineData(0x8a54, 05, 20)]
    [InlineData(0x8a64, 06, 20)]
    [InlineData(0x8a74, 07, 20)]
    [InlineData(0x8a84, 08, 20)]
    [InlineData(0x8a94, 09, 20)]
    [InlineData(0x8aa4, 10, 20)]
    [InlineData(0x8ab4, 11, 20)]
    [InlineData(0x8ac4, 12, 20)]
    [InlineData(0x8ad4, 13, 20)]
    [InlineData(0x8ae4, 14, 20)]
    [InlineData(0x8af4, 15, 20)]
    [InlineData(0x8b04, 16, 20)]
    [InlineData(0x8b14, 17, 20)]
    [InlineData(0x8b24, 18, 20)]
    [InlineData(0x8b34, 19, 20)]
    [InlineData(0x8b44, 20, 20)]
    [InlineData(0x8b54, 21, 20)]
    [InlineData(0x8b64, 22, 20)]
    [InlineData(0x8b74, 23, 20)]
    [InlineData(0x8b84, 24, 20)]
    [InlineData(0x8b94, 25, 20)]
    [InlineData(0x8ba4, 26, 20)]
    [InlineData(0x8bb4, 27, 20)]
    [InlineData(0x8bc4, 28, 20)]
    [InlineData(0x8bd4, 29, 20)]

    [InlineData(0x8a05, 00, 21)]
    [InlineData(0x8a15, 01, 21)]
    [InlineData(0x8a25, 02, 21)]
    [InlineData(0x8a35, 03, 21)]
    [InlineData(0x8a45, 04, 21)]
    [InlineData(0x8a55, 05, 21)]
    [InlineData(0x8a65, 06, 21)]
    [InlineData(0x8a75, 07, 21)]
    [InlineData(0x8a85, 08, 21)]
    [InlineData(0x8a95, 09, 21)]
    [InlineData(0x8aa5, 10, 21)]
    [InlineData(0x8ab5, 11, 21)]
    [InlineData(0x8ac5, 12, 21)]
    [InlineData(0x8ad5, 13, 21)]
    [InlineData(0x8ae5, 14, 21)]
    [InlineData(0x8af5, 15, 21)]
    [InlineData(0x8b05, 16, 21)]
    [InlineData(0x8b15, 17, 21)]
    [InlineData(0x8b25, 18, 21)]
    [InlineData(0x8b35, 19, 21)]
    [InlineData(0x8b45, 20, 21)]
    [InlineData(0x8b55, 21, 21)]
    [InlineData(0x8b65, 22, 21)]
    [InlineData(0x8b75, 23, 21)]
    [InlineData(0x8b85, 24, 21)]
    [InlineData(0x8b95, 25, 21)]
    [InlineData(0x8ba5, 26, 21)]
    [InlineData(0x8bb5, 27, 21)]
    [InlineData(0x8bc5, 28, 21)]
    [InlineData(0x8bd5, 29, 21)]

    [InlineData(0x8a06, 00, 22)]
    [InlineData(0x8a16, 01, 22)]
    [InlineData(0x8a26, 02, 22)]
    [InlineData(0x8a36, 03, 22)]
    [InlineData(0x8a46, 04, 22)]
    [InlineData(0x8a56, 05, 22)]
    [InlineData(0x8a66, 06, 22)]
    [InlineData(0x8a76, 07, 22)]
    [InlineData(0x8a86, 08, 22)]
    [InlineData(0x8a96, 09, 22)]
    [InlineData(0x8aa6, 10, 22)]
    [InlineData(0x8ab6, 11, 22)]
    [InlineData(0x8ac6, 12, 22)]
    [InlineData(0x8ad6, 13, 22)]
    [InlineData(0x8ae6, 14, 22)]
    [InlineData(0x8af6, 15, 22)]
    [InlineData(0x8b06, 16, 22)]
    [InlineData(0x8b16, 17, 22)]
    [InlineData(0x8b26, 18, 22)]
    [InlineData(0x8b36, 19, 22)]
    [InlineData(0x8b46, 20, 22)]
    [InlineData(0x8b56, 21, 22)]
    [InlineData(0x8b66, 22, 22)]
    [InlineData(0x8b76, 23, 22)]
    [InlineData(0x8b86, 24, 22)]
    [InlineData(0x8b96, 25, 22)]
    [InlineData(0x8ba6, 26, 22)]
    [InlineData(0x8bb6, 27, 22)]
    [InlineData(0x8bc6, 28, 22)]
    [InlineData(0x8bd6, 29, 22)]

    [InlineData(0x8a07, 00, 23)]
    [InlineData(0x8a17, 01, 23)]
    [InlineData(0x8a27, 02, 23)]
    [InlineData(0x8a37, 03, 23)]
    [InlineData(0x8a47, 04, 23)]
    [InlineData(0x8a57, 05, 23)]
    [InlineData(0x8a67, 06, 23)]
    [InlineData(0x8a77, 07, 23)]
    [InlineData(0x8a87, 08, 23)]
    [InlineData(0x8a97, 09, 23)]
    [InlineData(0x8aa7, 10, 23)]
    [InlineData(0x8ab7, 11, 23)]
    [InlineData(0x8ac7, 12, 23)]
    [InlineData(0x8ad7, 13, 23)]
    [InlineData(0x8ae7, 14, 23)]
    [InlineData(0x8af7, 15, 23)]
    [InlineData(0x8b07, 16, 23)]
    [InlineData(0x8b17, 17, 23)]
    [InlineData(0x8b27, 18, 23)]
    [InlineData(0x8b37, 19, 23)]
    [InlineData(0x8b47, 20, 23)]
    [InlineData(0x8b57, 21, 23)]
    [InlineData(0x8b67, 22, 23)]
    [InlineData(0x8b77, 23, 23)]
    [InlineData(0x8b87, 24, 23)]
    [InlineData(0x8b97, 25, 23)]
    [InlineData(0x8ba7, 26, 23)]
    [InlineData(0x8bb7, 27, 23)]
    [InlineData(0x8bc7, 28, 23)]
    [InlineData(0x8bd7, 29, 23)]

    [InlineData(0x8e00, 00, 24)]
    [InlineData(0x8e10, 01, 24)]
    [InlineData(0x8e20, 02, 24)]
    [InlineData(0x8e30, 03, 24)]
    [InlineData(0x8e40, 04, 24)]
    [InlineData(0x8e50, 05, 24)]
    [InlineData(0x8e60, 06, 24)]
    [InlineData(0x8e70, 07, 24)]
    [InlineData(0x8e80, 08, 24)]
    [InlineData(0x8e90, 09, 24)]
    [InlineData(0x8ea0, 10, 24)]
    [InlineData(0x8eb0, 11, 24)]
    [InlineData(0x8ec0, 12, 24)]
    [InlineData(0x8ed0, 13, 24)]
    [InlineData(0x8ee0, 14, 24)]
    [InlineData(0x8ef0, 15, 24)]
    [InlineData(0x8f00, 16, 24)]
    [InlineData(0x8f10, 17, 24)]
    [InlineData(0x8f20, 18, 24)]
    [InlineData(0x8f30, 19, 24)]
    [InlineData(0x8f40, 20, 24)]
    [InlineData(0x8f50, 21, 24)]
    [InlineData(0x8f60, 22, 24)]
    [InlineData(0x8f70, 23, 24)]
    [InlineData(0x8f80, 24, 24)]
    [InlineData(0x8f90, 25, 24)]
    [InlineData(0x8fa0, 26, 24)]
    [InlineData(0x8fb0, 27, 24)]
    [InlineData(0x8fc0, 28, 24)]
    [InlineData(0x8fd0, 29, 24)]

    [InlineData(0x8e01, 00, 25)]
    [InlineData(0x8e11, 01, 25)]
    [InlineData(0x8e21, 02, 25)]
    [InlineData(0x8e31, 03, 25)]
    [InlineData(0x8e41, 04, 25)]
    [InlineData(0x8e51, 05, 25)]
    [InlineData(0x8e61, 06, 25)]
    [InlineData(0x8e71, 07, 25)]
    [InlineData(0x8e81, 08, 25)]
    [InlineData(0x8e91, 09, 25)]
    [InlineData(0x8ea1, 10, 25)]
    [InlineData(0x8eb1, 11, 25)]
    [InlineData(0x8ec1, 12, 25)]
    [InlineData(0x8ed1, 13, 25)]
    [InlineData(0x8ee1, 14, 25)]
    [InlineData(0x8ef1, 15, 25)]
    [InlineData(0x8f01, 16, 25)]
    [InlineData(0x8f11, 17, 25)]
    [InlineData(0x8f21, 18, 25)]
    [InlineData(0x8f31, 19, 25)]
    [InlineData(0x8f41, 20, 25)]
    [InlineData(0x8f51, 21, 25)]
    [InlineData(0x8f61, 22, 25)]
    [InlineData(0x8f71, 23, 25)]
    [InlineData(0x8f81, 24, 25)]
    [InlineData(0x8f91, 25, 25)]
    [InlineData(0x8fa1, 26, 25)]
    [InlineData(0x8fb1, 27, 25)]
    [InlineData(0x8fc1, 28, 25)]
    [InlineData(0x8fd1, 29, 25)]

    [InlineData(0x8e02, 00, 26)]
    [InlineData(0x8e12, 01, 26)]
    [InlineData(0x8e22, 02, 26)]
    [InlineData(0x8e32, 03, 26)]
    [InlineData(0x8e42, 04, 26)]
    [InlineData(0x8e52, 05, 26)]
    [InlineData(0x8e62, 06, 26)]
    [InlineData(0x8e72, 07, 26)]
    [InlineData(0x8e82, 08, 26)]
    [InlineData(0x8e92, 09, 26)]
    [InlineData(0x8ea2, 10, 26)]
    [InlineData(0x8eb2, 11, 26)]
    [InlineData(0x8ec2, 12, 26)]
    [InlineData(0x8ed2, 13, 26)]
    [InlineData(0x8ee2, 14, 26)]
    [InlineData(0x8ef2, 15, 26)]
    [InlineData(0x8f02, 16, 26)]
    [InlineData(0x8f12, 17, 26)]
    [InlineData(0x8f22, 18, 26)]
    [InlineData(0x8f32, 19, 26)]
    [InlineData(0x8f42, 20, 26)]
    [InlineData(0x8f52, 21, 26)]
    [InlineData(0x8f62, 22, 26)]
    [InlineData(0x8f72, 23, 26)]
    [InlineData(0x8f82, 24, 26)]
    [InlineData(0x8f92, 25, 26)]
    [InlineData(0x8fa2, 26, 26)]
    [InlineData(0x8fb2, 27, 26)]
    [InlineData(0x8fc2, 28, 26)]
    [InlineData(0x8fd2, 29, 26)]

    [InlineData(0x8e03, 00, 27)]
    [InlineData(0x8e13, 01, 27)]
    [InlineData(0x8e23, 02, 27)]
    [InlineData(0x8e33, 03, 27)]
    [InlineData(0x8e43, 04, 27)]
    [InlineData(0x8e53, 05, 27)]
    [InlineData(0x8e63, 06, 27)]
    [InlineData(0x8e73, 07, 27)]
    [InlineData(0x8e83, 08, 27)]
    [InlineData(0x8e93, 09, 27)]
    [InlineData(0x8ea3, 10, 27)]
    [InlineData(0x8eb3, 11, 27)]
    [InlineData(0x8ec3, 12, 27)]
    [InlineData(0x8ed3, 13, 27)]
    [InlineData(0x8ee3, 14, 27)]
    [InlineData(0x8ef3, 15, 27)]
    [InlineData(0x8f03, 16, 27)]
    [InlineData(0x8f13, 17, 27)]
    [InlineData(0x8f23, 18, 27)]
    [InlineData(0x8f33, 19, 27)]
    [InlineData(0x8f43, 20, 27)]
    [InlineData(0x8f53, 21, 27)]
    [InlineData(0x8f63, 22, 27)]
    [InlineData(0x8f73, 23, 27)]
    [InlineData(0x8f83, 24, 27)]
    [InlineData(0x8f93, 25, 27)]
    [InlineData(0x8fa3, 26, 27)]
    [InlineData(0x8fb3, 27, 27)]
    [InlineData(0x8fc3, 28, 27)]
    [InlineData(0x8fd3, 29, 27)]

    [InlineData(0x8e04, 00, 28)]
    [InlineData(0x8e14, 01, 28)]
    [InlineData(0x8e24, 02, 28)]
    [InlineData(0x8e34, 03, 28)]
    [InlineData(0x8e44, 04, 28)]
    [InlineData(0x8e54, 05, 28)]
    [InlineData(0x8e64, 06, 28)]
    [InlineData(0x8e74, 07, 28)]
    [InlineData(0x8e84, 08, 28)]
    [InlineData(0x8e94, 09, 28)]
    [InlineData(0x8ea4, 10, 28)]
    [InlineData(0x8eb4, 11, 28)]
    [InlineData(0x8ec4, 12, 28)]
    [InlineData(0x8ed4, 13, 28)]
    [InlineData(0x8ee4, 14, 28)]
    [InlineData(0x8ef4, 15, 28)]
    [InlineData(0x8f04, 16, 28)]
    [InlineData(0x8f14, 17, 28)]
    [InlineData(0x8f24, 18, 28)]
    [InlineData(0x8f34, 19, 28)]
    [InlineData(0x8f44, 20, 28)]
    [InlineData(0x8f54, 21, 28)]
    [InlineData(0x8f64, 22, 28)]
    [InlineData(0x8f74, 23, 28)]
    [InlineData(0x8f84, 24, 28)]
    [InlineData(0x8f94, 25, 28)]
    [InlineData(0x8fa4, 26, 28)]
    [InlineData(0x8fb4, 27, 28)]
    [InlineData(0x8fc4, 28, 28)]
    [InlineData(0x8fd4, 29, 28)]

    [InlineData(0x8e05, 00, 29)]
    [InlineData(0x8e15, 01, 29)]
    [InlineData(0x8e25, 02, 29)]
    [InlineData(0x8e35, 03, 29)]
    [InlineData(0x8e45, 04, 29)]
    [InlineData(0x8e55, 05, 29)]
    [InlineData(0x8e65, 06, 29)]
    [InlineData(0x8e75, 07, 29)]
    [InlineData(0x8e85, 08, 29)]
    [InlineData(0x8e95, 09, 29)]
    [InlineData(0x8ea5, 10, 29)]
    [InlineData(0x8eb5, 11, 29)]
    [InlineData(0x8ec5, 12, 29)]
    [InlineData(0x8ed5, 13, 29)]
    [InlineData(0x8ee5, 14, 29)]
    [InlineData(0x8ef5, 15, 29)]
    [InlineData(0x8f05, 16, 29)]
    [InlineData(0x8f15, 17, 29)]
    [InlineData(0x8f25, 18, 29)]
    [InlineData(0x8f35, 19, 29)]
    [InlineData(0x8f45, 20, 29)]
    [InlineData(0x8f55, 21, 29)]
    [InlineData(0x8f65, 22, 29)]
    [InlineData(0x8f75, 23, 29)]
    [InlineData(0x8f85, 24, 29)]
    [InlineData(0x8f95, 25, 29)]
    [InlineData(0x8fa5, 26, 29)]
    [InlineData(0x8fb5, 27, 29)]
    [InlineData(0x8fc5, 28, 29)]
    [InlineData(0x8fd5, 29, 29)]

    [InlineData(0x8e06, 00, 30)]
    [InlineData(0x8e16, 01, 30)]
    [InlineData(0x8e26, 02, 30)]
    [InlineData(0x8e36, 03, 30)]
    [InlineData(0x8e46, 04, 30)]
    [InlineData(0x8e56, 05, 30)]
    [InlineData(0x8e66, 06, 30)]
    [InlineData(0x8e76, 07, 30)]
    [InlineData(0x8e86, 08, 30)]
    [InlineData(0x8e96, 09, 30)]
    [InlineData(0x8ea6, 10, 30)]
    [InlineData(0x8eb6, 11, 30)]
    [InlineData(0x8ec6, 12, 30)]
    [InlineData(0x8ed6, 13, 30)]
    [InlineData(0x8ee6, 14, 30)]
    [InlineData(0x8ef6, 15, 30)]
    [InlineData(0x8f06, 16, 30)]
    [InlineData(0x8f16, 17, 30)]
    [InlineData(0x8f26, 18, 30)]
    [InlineData(0x8f36, 19, 30)]
    [InlineData(0x8f46, 20, 30)]
    [InlineData(0x8f56, 21, 30)]
    [InlineData(0x8f66, 22, 30)]
    [InlineData(0x8f76, 23, 30)]
    [InlineData(0x8f86, 24, 30)]
    [InlineData(0x8f96, 25, 30)]
    [InlineData(0x8fa6, 26, 30)]
    [InlineData(0x8fb6, 27, 30)]
    [InlineData(0x8fc6, 28, 30)]
    [InlineData(0x8fd6, 29, 30)]

    [InlineData(0x8e07, 00, 31)]
    [InlineData(0x8e17, 01, 31)]
    [InlineData(0x8e27, 02, 31)]
    [InlineData(0x8e37, 03, 31)]
    [InlineData(0x8e47, 04, 31)]
    [InlineData(0x8e57, 05, 31)]
    [InlineData(0x8e67, 06, 31)]
    [InlineData(0x8e77, 07, 31)]
    [InlineData(0x8e87, 08, 31)]
    [InlineData(0x8e97, 09, 31)]
    [InlineData(0x8ea7, 10, 31)]
    [InlineData(0x8eb7, 11, 31)]
    [InlineData(0x8ec7, 12, 31)]
    [InlineData(0x8ed7, 13, 31)]
    [InlineData(0x8ee7, 14, 31)]
    [InlineData(0x8ef7, 15, 31)]
    [InlineData(0x8f07, 16, 31)]
    [InlineData(0x8f17, 17, 31)]
    [InlineData(0x8f27, 18, 31)]
    [InlineData(0x8f37, 19, 31)]
    [InlineData(0x8f47, 20, 31)]
    [InlineData(0x8f57, 21, 31)]
    [InlineData(0x8f67, 22, 31)]
    [InlineData(0x8f77, 23, 31)]
    [InlineData(0x8f87, 24, 31)]
    [InlineData(0x8f97, 25, 31)]
    [InlineData(0x8fa7, 26, 31)]
    [InlineData(0x8fb7, 27, 31)]
    [InlineData(0x8fc7, 28, 31)]
    [InlineData(0x8fd7, 29, 31)]

    [InlineData(0xa200, 00, 32)]
    [InlineData(0xa210, 01, 32)]
    [InlineData(0xa220, 02, 32)]
    [InlineData(0xa230, 03, 32)]
    [InlineData(0xa240, 04, 32)]
    [InlineData(0xa250, 05, 32)]
    [InlineData(0xa260, 06, 32)]
    [InlineData(0xa270, 07, 32)]
    [InlineData(0xa280, 08, 32)]
    [InlineData(0xa290, 09, 32)]
    [InlineData(0xa2a0, 10, 32)]
    [InlineData(0xa2b0, 11, 32)]
    [InlineData(0xa2c0, 12, 32)]
    [InlineData(0xa2d0, 13, 32)]
    [InlineData(0xa2e0, 14, 32)]
    [InlineData(0xa2f0, 15, 32)]
    [InlineData(0xa300, 16, 32)]
    [InlineData(0xa310, 17, 32)]
    [InlineData(0xa320, 18, 32)]
    [InlineData(0xa330, 19, 32)]
    [InlineData(0xa340, 20, 32)]
    [InlineData(0xa350, 21, 32)]
    [InlineData(0xa360, 22, 32)]
    [InlineData(0xa370, 23, 32)]
    [InlineData(0xa380, 24, 32)]
    [InlineData(0xa390, 25, 32)]
    [InlineData(0xa3a0, 26, 32)]
    [InlineData(0xa3b0, 27, 32)]
    [InlineData(0xa3c0, 28, 32)]
    [InlineData(0xa3d0, 29, 32)]

    [InlineData(0xa201, 00, 33)]
    [InlineData(0xa211, 01, 33)]
    [InlineData(0xa221, 02, 33)]
    [InlineData(0xa231, 03, 33)]
    [InlineData(0xa241, 04, 33)]
    [InlineData(0xa251, 05, 33)]
    [InlineData(0xa261, 06, 33)]
    [InlineData(0xa271, 07, 33)]
    [InlineData(0xa281, 08, 33)]
    [InlineData(0xa291, 09, 33)]
    [InlineData(0xa2a1, 10, 33)]
    [InlineData(0xa2b1, 11, 33)]
    [InlineData(0xa2c1, 12, 33)]
    [InlineData(0xa2d1, 13, 33)]
    [InlineData(0xa2e1, 14, 33)]
    [InlineData(0xa2f1, 15, 33)]
    [InlineData(0xa301, 16, 33)]
    [InlineData(0xa311, 17, 33)]
    [InlineData(0xa321, 18, 33)]
    [InlineData(0xa331, 19, 33)]
    [InlineData(0xa341, 20, 33)]
    [InlineData(0xa351, 21, 33)]
    [InlineData(0xa361, 22, 33)]
    [InlineData(0xa371, 23, 33)]
    [InlineData(0xa381, 24, 33)]
    [InlineData(0xa391, 25, 33)]
    [InlineData(0xa3a1, 26, 33)]
    [InlineData(0xa3b1, 27, 33)]
    [InlineData(0xa3c1, 28, 33)]
    [InlineData(0xa3d1, 29, 33)]

    [InlineData(0xa202, 00, 34)]
    [InlineData(0xa212, 01, 34)]
    [InlineData(0xa222, 02, 34)]
    [InlineData(0xa232, 03, 34)]
    [InlineData(0xa242, 04, 34)]
    [InlineData(0xa252, 05, 34)]
    [InlineData(0xa262, 06, 34)]
    [InlineData(0xa272, 07, 34)]
    [InlineData(0xa282, 08, 34)]
    [InlineData(0xa292, 09, 34)]
    [InlineData(0xa2a2, 10, 34)]
    [InlineData(0xa2b2, 11, 34)]
    [InlineData(0xa2c2, 12, 34)]
    [InlineData(0xa2d2, 13, 34)]
    [InlineData(0xa2e2, 14, 34)]
    [InlineData(0xa2f2, 15, 34)]
    [InlineData(0xa302, 16, 34)]
    [InlineData(0xa312, 17, 34)]
    [InlineData(0xa322, 18, 34)]
    [InlineData(0xa332, 19, 34)]
    [InlineData(0xa342, 20, 34)]
    [InlineData(0xa352, 21, 34)]
    [InlineData(0xa362, 22, 34)]
    [InlineData(0xa372, 23, 34)]
    [InlineData(0xa382, 24, 34)]
    [InlineData(0xa392, 25, 34)]
    [InlineData(0xa3a2, 26, 34)]
    [InlineData(0xa3b2, 27, 34)]
    [InlineData(0xa3c2, 28, 34)]
    [InlineData(0xa3d2, 29, 34)]

    [InlineData(0xa203, 00, 35)]
    [InlineData(0xa213, 01, 35)]
    [InlineData(0xa223, 02, 35)]
    [InlineData(0xa233, 03, 35)]
    [InlineData(0xa243, 04, 35)]
    [InlineData(0xa253, 05, 35)]
    [InlineData(0xa263, 06, 35)]
    [InlineData(0xa273, 07, 35)]
    [InlineData(0xa283, 08, 35)]
    [InlineData(0xa293, 09, 35)]
    [InlineData(0xa2a3, 10, 35)]
    [InlineData(0xa2b3, 11, 35)]
    [InlineData(0xa2c3, 12, 35)]
    [InlineData(0xa2d3, 13, 35)]
    [InlineData(0xa2e3, 14, 35)]
    [InlineData(0xa2f3, 15, 35)]
    [InlineData(0xa303, 16, 35)]
    [InlineData(0xa313, 17, 35)]
    [InlineData(0xa323, 18, 35)]
    [InlineData(0xa333, 19, 35)]
    [InlineData(0xa343, 20, 35)]
    [InlineData(0xa353, 21, 35)]
    [InlineData(0xa363, 22, 35)]
    [InlineData(0xa373, 23, 35)]
    [InlineData(0xa383, 24, 35)]
    [InlineData(0xa393, 25, 35)]
    [InlineData(0xa3a3, 26, 35)]
    [InlineData(0xa3b3, 27, 35)]
    [InlineData(0xa3c3, 28, 35)]
    [InlineData(0xa3d3, 29, 35)]

    [InlineData(0xa204, 00, 36)]
    [InlineData(0xa214, 01, 36)]
    [InlineData(0xa224, 02, 36)]
    [InlineData(0xa234, 03, 36)]
    [InlineData(0xa244, 04, 36)]
    [InlineData(0xa254, 05, 36)]
    [InlineData(0xa264, 06, 36)]
    [InlineData(0xa274, 07, 36)]
    [InlineData(0xa284, 08, 36)]
    [InlineData(0xa294, 09, 36)]
    [InlineData(0xa2a4, 10, 36)]
    [InlineData(0xa2b4, 11, 36)]
    [InlineData(0xa2c4, 12, 36)]
    [InlineData(0xa2d4, 13, 36)]
    [InlineData(0xa2e4, 14, 36)]
    [InlineData(0xa2f4, 15, 36)]
    [InlineData(0xa304, 16, 36)]
    [InlineData(0xa314, 17, 36)]
    [InlineData(0xa324, 18, 36)]
    [InlineData(0xa334, 19, 36)]
    [InlineData(0xa344, 20, 36)]
    [InlineData(0xa354, 21, 36)]
    [InlineData(0xa364, 22, 36)]
    [InlineData(0xa374, 23, 36)]
    [InlineData(0xa384, 24, 36)]
    [InlineData(0xa394, 25, 36)]
    [InlineData(0xa3a4, 26, 36)]
    [InlineData(0xa3b4, 27, 36)]
    [InlineData(0xa3c4, 28, 36)]
    [InlineData(0xa3d4, 29, 36)]

    [InlineData(0xa205, 00, 37)]
    [InlineData(0xa215, 01, 37)]
    [InlineData(0xa225, 02, 37)]
    [InlineData(0xa235, 03, 37)]
    [InlineData(0xa245, 04, 37)]
    [InlineData(0xa255, 05, 37)]
    [InlineData(0xa265, 06, 37)]
    [InlineData(0xa275, 07, 37)]
    [InlineData(0xa285, 08, 37)]
    [InlineData(0xa295, 09, 37)]
    [InlineData(0xa2a5, 10, 37)]
    [InlineData(0xa2b5, 11, 37)]
    [InlineData(0xa2c5, 12, 37)]
    [InlineData(0xa2d5, 13, 37)]
    [InlineData(0xa2e5, 14, 37)]
    [InlineData(0xa2f5, 15, 37)]
    [InlineData(0xa305, 16, 37)]
    [InlineData(0xa315, 17, 37)]
    [InlineData(0xa325, 18, 37)]
    [InlineData(0xa335, 19, 37)]
    [InlineData(0xa345, 20, 37)]
    [InlineData(0xa355, 21, 37)]
    [InlineData(0xa365, 22, 37)]
    [InlineData(0xa375, 23, 37)]
    [InlineData(0xa385, 24, 37)]
    [InlineData(0xa395, 25, 37)]
    [InlineData(0xa3a5, 26, 37)]
    [InlineData(0xa3b5, 27, 37)]
    [InlineData(0xa3c5, 28, 37)]
    [InlineData(0xa3d5, 29, 37)]

    [InlineData(0xa206, 00, 38)]
    [InlineData(0xa216, 01, 38)]
    [InlineData(0xa226, 02, 38)]
    [InlineData(0xa236, 03, 38)]
    [InlineData(0xa246, 04, 38)]
    [InlineData(0xa256, 05, 38)]
    [InlineData(0xa266, 06, 38)]
    [InlineData(0xa276, 07, 38)]
    [InlineData(0xa286, 08, 38)]
    [InlineData(0xa296, 09, 38)]
    [InlineData(0xa2a6, 10, 38)]
    [InlineData(0xa2b6, 11, 38)]
    [InlineData(0xa2c6, 12, 38)]
    [InlineData(0xa2d6, 13, 38)]
    [InlineData(0xa2e6, 14, 38)]
    [InlineData(0xa2f6, 15, 38)]
    [InlineData(0xa306, 16, 38)]
    [InlineData(0xa316, 17, 38)]
    [InlineData(0xa326, 18, 38)]
    [InlineData(0xa336, 19, 38)]
    [InlineData(0xa346, 20, 38)]
    [InlineData(0xa356, 21, 38)]
    [InlineData(0xa366, 22, 38)]
    [InlineData(0xa376, 23, 38)]
    [InlineData(0xa386, 24, 38)]
    [InlineData(0xa396, 25, 38)]
    [InlineData(0xa3a6, 26, 38)]
    [InlineData(0xa3b6, 27, 38)]
    [InlineData(0xa3c6, 28, 38)]
    [InlineData(0xa3d6, 29, 38)]

    [InlineData(0xa207, 00, 39)]
    [InlineData(0xa217, 01, 39)]
    [InlineData(0xa227, 02, 39)]
    [InlineData(0xa237, 03, 39)]
    [InlineData(0xa247, 04, 39)]
    [InlineData(0xa257, 05, 39)]
    [InlineData(0xa267, 06, 39)]
    [InlineData(0xa277, 07, 39)]
    [InlineData(0xa287, 08, 39)]
    [InlineData(0xa297, 09, 39)]
    [InlineData(0xa2a7, 10, 39)]
    [InlineData(0xa2b7, 11, 39)]
    [InlineData(0xa2c7, 12, 39)]
    [InlineData(0xa2d7, 13, 39)]
    [InlineData(0xa2e7, 14, 39)]
    [InlineData(0xa2f7, 15, 39)]
    [InlineData(0xa307, 16, 39)]
    [InlineData(0xa317, 17, 39)]
    [InlineData(0xa327, 18, 39)]
    [InlineData(0xa337, 19, 39)]
    [InlineData(0xa347, 20, 39)]
    [InlineData(0xa357, 21, 39)]
    [InlineData(0xa367, 22, 39)]
    [InlineData(0xa377, 23, 39)]
    [InlineData(0xa387, 24, 39)]
    [InlineData(0xa397, 25, 39)]
    [InlineData(0xa3a7, 26, 39)]
    [InlineData(0xa3b7, 27, 39)]
    [InlineData(0xa3c7, 28, 39)]
    [InlineData(0xa3d7, 29, 39)]

    [InlineData(0xa600, 00, 40)]
    [InlineData(0xa610, 01, 40)]
    [InlineData(0xa620, 02, 40)]
    [InlineData(0xa630, 03, 40)]
    [InlineData(0xa640, 04, 40)]
    [InlineData(0xa650, 05, 40)]
    [InlineData(0xa660, 06, 40)]
    [InlineData(0xa670, 07, 40)]
    [InlineData(0xa680, 08, 40)]
    [InlineData(0xa690, 09, 40)]
    [InlineData(0xa6a0, 10, 40)]
    [InlineData(0xa6b0, 11, 40)]
    [InlineData(0xa6c0, 12, 40)]
    [InlineData(0xa6d0, 13, 40)]
    [InlineData(0xa6e0, 14, 40)]
    [InlineData(0xa6f0, 15, 40)]
    [InlineData(0xa700, 16, 40)]
    [InlineData(0xa710, 17, 40)]
    [InlineData(0xa720, 18, 40)]
    [InlineData(0xa730, 19, 40)]
    [InlineData(0xa740, 20, 40)]
    [InlineData(0xa750, 21, 40)]
    [InlineData(0xa760, 22, 40)]
    [InlineData(0xa770, 23, 40)]
    [InlineData(0xa780, 24, 40)]
    [InlineData(0xa790, 25, 40)]
    [InlineData(0xa7a0, 26, 40)]
    [InlineData(0xa7b0, 27, 40)]
    [InlineData(0xa7c0, 28, 40)]
    [InlineData(0xa7d0, 29, 40)]

    [InlineData(0xa601, 00, 41)]
    [InlineData(0xa611, 01, 41)]
    [InlineData(0xa621, 02, 41)]
    [InlineData(0xa631, 03, 41)]
    [InlineData(0xa641, 04, 41)]
    [InlineData(0xa651, 05, 41)]
    [InlineData(0xa661, 06, 41)]
    [InlineData(0xa671, 07, 41)]
    [InlineData(0xa681, 08, 41)]
    [InlineData(0xa691, 09, 41)]
    [InlineData(0xa6a1, 10, 41)]
    [InlineData(0xa6b1, 11, 41)]
    [InlineData(0xa6c1, 12, 41)]
    [InlineData(0xa6d1, 13, 41)]
    [InlineData(0xa6e1, 14, 41)]
    [InlineData(0xa6f1, 15, 41)]
    [InlineData(0xa701, 16, 41)]
    [InlineData(0xa711, 17, 41)]
    [InlineData(0xa721, 18, 41)]
    [InlineData(0xa731, 19, 41)]
    [InlineData(0xa741, 20, 41)]
    [InlineData(0xa751, 21, 41)]
    [InlineData(0xa761, 22, 41)]
    [InlineData(0xa771, 23, 41)]
    [InlineData(0xa781, 24, 41)]
    [InlineData(0xa791, 25, 41)]
    [InlineData(0xa7a1, 26, 41)]
    [InlineData(0xa7b1, 27, 41)]
    [InlineData(0xa7c1, 28, 41)]
    [InlineData(0xa7d1, 29, 41)]

    [InlineData(0xa602, 00, 42)]
    [InlineData(0xa612, 01, 42)]
    [InlineData(0xa622, 02, 42)]
    [InlineData(0xa632, 03, 42)]
    [InlineData(0xa642, 04, 42)]
    [InlineData(0xa652, 05, 42)]
    [InlineData(0xa662, 06, 42)]
    [InlineData(0xa672, 07, 42)]
    [InlineData(0xa682, 08, 42)]
    [InlineData(0xa692, 09, 42)]
    [InlineData(0xa6a2, 10, 42)]
    [InlineData(0xa6b2, 11, 42)]
    [InlineData(0xa6c2, 12, 42)]
    [InlineData(0xa6d2, 13, 42)]
    [InlineData(0xa6e2, 14, 42)]
    [InlineData(0xa6f2, 15, 42)]
    [InlineData(0xa702, 16, 42)]
    [InlineData(0xa712, 17, 42)]
    [InlineData(0xa722, 18, 42)]
    [InlineData(0xa732, 19, 42)]
    [InlineData(0xa742, 20, 42)]
    [InlineData(0xa752, 21, 42)]
    [InlineData(0xa762, 22, 42)]
    [InlineData(0xa772, 23, 42)]
    [InlineData(0xa782, 24, 42)]
    [InlineData(0xa792, 25, 42)]
    [InlineData(0xa7a2, 26, 42)]
    [InlineData(0xa7b2, 27, 42)]
    [InlineData(0xa7c2, 28, 42)]
    [InlineData(0xa7d2, 29, 42)]

    [InlineData(0xa603, 00, 43)]
    [InlineData(0xa613, 01, 43)]
    [InlineData(0xa623, 02, 43)]
    [InlineData(0xa633, 03, 43)]
    [InlineData(0xa643, 04, 43)]
    [InlineData(0xa653, 05, 43)]
    [InlineData(0xa663, 06, 43)]
    [InlineData(0xa673, 07, 43)]
    [InlineData(0xa683, 08, 43)]
    [InlineData(0xa693, 09, 43)]
    [InlineData(0xa6a3, 10, 43)]
    [InlineData(0xa6b3, 11, 43)]
    [InlineData(0xa6c3, 12, 43)]
    [InlineData(0xa6d3, 13, 43)]
    [InlineData(0xa6e3, 14, 43)]
    [InlineData(0xa6f3, 15, 43)]
    [InlineData(0xa703, 16, 43)]
    [InlineData(0xa713, 17, 43)]
    [InlineData(0xa723, 18, 43)]
    [InlineData(0xa733, 19, 43)]
    [InlineData(0xa743, 20, 43)]
    [InlineData(0xa753, 21, 43)]
    [InlineData(0xa763, 22, 43)]
    [InlineData(0xa773, 23, 43)]
    [InlineData(0xa783, 24, 43)]
    [InlineData(0xa793, 25, 43)]
    [InlineData(0xa7a3, 26, 43)]
    [InlineData(0xa7b3, 27, 43)]
    [InlineData(0xa7c3, 28, 43)]
    [InlineData(0xa7d3, 29, 43)]

    [InlineData(0xa604, 00, 44)]
    [InlineData(0xa614, 01, 44)]
    [InlineData(0xa624, 02, 44)]
    [InlineData(0xa634, 03, 44)]
    [InlineData(0xa644, 04, 44)]
    [InlineData(0xa654, 05, 44)]
    [InlineData(0xa664, 06, 44)]
    [InlineData(0xa674, 07, 44)]
    [InlineData(0xa684, 08, 44)]
    [InlineData(0xa694, 09, 44)]
    [InlineData(0xa6a4, 10, 44)]
    [InlineData(0xa6b4, 11, 44)]
    [InlineData(0xa6c4, 12, 44)]
    [InlineData(0xa6d4, 13, 44)]
    [InlineData(0xa6e4, 14, 44)]
    [InlineData(0xa6f4, 15, 44)]
    [InlineData(0xa704, 16, 44)]
    [InlineData(0xa714, 17, 44)]
    [InlineData(0xa724, 18, 44)]
    [InlineData(0xa734, 19, 44)]
    [InlineData(0xa744, 20, 44)]
    [InlineData(0xa754, 21, 44)]
    [InlineData(0xa764, 22, 44)]
    [InlineData(0xa774, 23, 44)]
    [InlineData(0xa784, 24, 44)]
    [InlineData(0xa794, 25, 44)]
    [InlineData(0xa7a4, 26, 44)]
    [InlineData(0xa7b4, 27, 44)]
    [InlineData(0xa7c4, 28, 44)]
    [InlineData(0xa7d4, 29, 44)]

    [InlineData(0xa605, 00, 45)]
    [InlineData(0xa615, 01, 45)]
    [InlineData(0xa625, 02, 45)]
    [InlineData(0xa635, 03, 45)]
    [InlineData(0xa645, 04, 45)]
    [InlineData(0xa655, 05, 45)]
    [InlineData(0xa665, 06, 45)]
    [InlineData(0xa675, 07, 45)]
    [InlineData(0xa685, 08, 45)]
    [InlineData(0xa695, 09, 45)]
    [InlineData(0xa6a5, 10, 45)]
    [InlineData(0xa6b5, 11, 45)]
    [InlineData(0xa6c5, 12, 45)]
    [InlineData(0xa6d5, 13, 45)]
    [InlineData(0xa6e5, 14, 45)]
    [InlineData(0xa6f5, 15, 45)]
    [InlineData(0xa705, 16, 45)]
    [InlineData(0xa715, 17, 45)]
    [InlineData(0xa725, 18, 45)]
    [InlineData(0xa735, 19, 45)]
    [InlineData(0xa745, 20, 45)]
    [InlineData(0xa755, 21, 45)]
    [InlineData(0xa765, 22, 45)]
    [InlineData(0xa775, 23, 45)]
    [InlineData(0xa785, 24, 45)]
    [InlineData(0xa795, 25, 45)]
    [InlineData(0xa7a5, 26, 45)]
    [InlineData(0xa7b5, 27, 45)]
    [InlineData(0xa7c5, 28, 45)]
    [InlineData(0xa7d5, 29, 45)]

    [InlineData(0xa606, 00, 46)]
    [InlineData(0xa616, 01, 46)]
    [InlineData(0xa626, 02, 46)]
    [InlineData(0xa636, 03, 46)]
    [InlineData(0xa646, 04, 46)]
    [InlineData(0xa656, 05, 46)]
    [InlineData(0xa666, 06, 46)]
    [InlineData(0xa676, 07, 46)]
    [InlineData(0xa686, 08, 46)]
    [InlineData(0xa696, 09, 46)]
    [InlineData(0xa6a6, 10, 46)]
    [InlineData(0xa6b6, 11, 46)]
    [InlineData(0xa6c6, 12, 46)]
    [InlineData(0xa6d6, 13, 46)]
    [InlineData(0xa6e6, 14, 46)]
    [InlineData(0xa6f6, 15, 46)]
    [InlineData(0xa706, 16, 46)]
    [InlineData(0xa716, 17, 46)]
    [InlineData(0xa726, 18, 46)]
    [InlineData(0xa736, 19, 46)]
    [InlineData(0xa746, 20, 46)]
    [InlineData(0xa756, 21, 46)]
    [InlineData(0xa766, 22, 46)]
    [InlineData(0xa776, 23, 46)]
    [InlineData(0xa786, 24, 46)]
    [InlineData(0xa796, 25, 46)]
    [InlineData(0xa7a6, 26, 46)]
    [InlineData(0xa7b6, 27, 46)]
    [InlineData(0xa7c6, 28, 46)]
    [InlineData(0xa7d6, 29, 46)]

    [InlineData(0xa607, 00, 47)]
    [InlineData(0xa617, 01, 47)]
    [InlineData(0xa627, 02, 47)]
    [InlineData(0xa637, 03, 47)]
    [InlineData(0xa647, 04, 47)]
    [InlineData(0xa657, 05, 47)]
    [InlineData(0xa667, 06, 47)]
    [InlineData(0xa677, 07, 47)]
    [InlineData(0xa687, 08, 47)]
    [InlineData(0xa697, 09, 47)]
    [InlineData(0xa6a7, 10, 47)]
    [InlineData(0xa6b7, 11, 47)]
    [InlineData(0xa6c7, 12, 47)]
    [InlineData(0xa6d7, 13, 47)]
    [InlineData(0xa6e7, 14, 47)]
    [InlineData(0xa6f7, 15, 47)]
    [InlineData(0xa707, 16, 47)]
    [InlineData(0xa717, 17, 47)]
    [InlineData(0xa727, 18, 47)]
    [InlineData(0xa737, 19, 47)]
    [InlineData(0xa747, 20, 47)]
    [InlineData(0xa757, 21, 47)]
    [InlineData(0xa767, 22, 47)]
    [InlineData(0xa777, 23, 47)]
    [InlineData(0xa787, 24, 47)]
    [InlineData(0xa797, 25, 47)]
    [InlineData(0xa7a7, 26, 47)]
    [InlineData(0xa7b7, 27, 47)]
    [InlineData(0xa7c7, 28, 47)]
    [InlineData(0xa7d7, 29, 47)]

    [InlineData(0xaa00, 00, 48)]
    [InlineData(0xaa10, 01, 48)]
    [InlineData(0xaa20, 02, 48)]
    [InlineData(0xaa30, 03, 48)]
    [InlineData(0xaa40, 04, 48)]
    [InlineData(0xaa50, 05, 48)]
    [InlineData(0xaa60, 06, 48)]
    [InlineData(0xaa70, 07, 48)]
    [InlineData(0xaa80, 08, 48)]
    [InlineData(0xaa90, 09, 48)]
    [InlineData(0xaaa0, 10, 48)]
    [InlineData(0xaab0, 11, 48)]
    [InlineData(0xaac0, 12, 48)]
    [InlineData(0xaad0, 13, 48)]
    [InlineData(0xaae0, 14, 48)]
    [InlineData(0xaaf0, 15, 48)]
    [InlineData(0xab00, 16, 48)]
    [InlineData(0xab10, 17, 48)]
    [InlineData(0xab20, 18, 48)]
    [InlineData(0xab30, 19, 48)]
    [InlineData(0xab40, 20, 48)]
    [InlineData(0xab50, 21, 48)]
    [InlineData(0xab60, 22, 48)]
    [InlineData(0xab70, 23, 48)]
    [InlineData(0xab80, 24, 48)]
    [InlineData(0xab90, 25, 48)]
    [InlineData(0xaba0, 26, 48)]
    [InlineData(0xabb0, 27, 48)]
    [InlineData(0xabc0, 28, 48)]
    [InlineData(0xabd0, 29, 48)]

    [InlineData(0xaa01, 00, 49)]
    [InlineData(0xaa11, 01, 49)]
    [InlineData(0xaa21, 02, 49)]
    [InlineData(0xaa31, 03, 49)]
    [InlineData(0xaa41, 04, 49)]
    [InlineData(0xaa51, 05, 49)]
    [InlineData(0xaa61, 06, 49)]
    [InlineData(0xaa71, 07, 49)]
    [InlineData(0xaa81, 08, 49)]
    [InlineData(0xaa91, 09, 49)]
    [InlineData(0xaaa1, 10, 49)]
    [InlineData(0xaab1, 11, 49)]
    [InlineData(0xaac1, 12, 49)]
    [InlineData(0xaad1, 13, 49)]
    [InlineData(0xaae1, 14, 49)]
    [InlineData(0xaaf1, 15, 49)]
    [InlineData(0xab01, 16, 49)]
    [InlineData(0xab11, 17, 49)]
    [InlineData(0xab21, 18, 49)]
    [InlineData(0xab31, 19, 49)]
    [InlineData(0xab41, 20, 49)]
    [InlineData(0xab51, 21, 49)]
    [InlineData(0xab61, 22, 49)]
    [InlineData(0xab71, 23, 49)]
    [InlineData(0xab81, 24, 49)]
    [InlineData(0xab91, 25, 49)]
    [InlineData(0xaba1, 26, 49)]
    [InlineData(0xabb1, 27, 49)]
    [InlineData(0xabc1, 28, 49)]
    [InlineData(0xabd1, 29, 49)]

    [InlineData(0xaa02, 00, 50)]
    [InlineData(0xaa12, 01, 50)]
    [InlineData(0xaa22, 02, 50)]
    [InlineData(0xaa32, 03, 50)]
    [InlineData(0xaa42, 04, 50)]
    [InlineData(0xaa52, 05, 50)]
    [InlineData(0xaa62, 06, 50)]
    [InlineData(0xaa72, 07, 50)]
    [InlineData(0xaa82, 08, 50)]
    [InlineData(0xaa92, 09, 50)]
    [InlineData(0xaaa2, 10, 50)]
    [InlineData(0xaab2, 11, 50)]
    [InlineData(0xaac2, 12, 50)]
    [InlineData(0xaad2, 13, 50)]
    [InlineData(0xaae2, 14, 50)]
    [InlineData(0xaaf2, 15, 50)]
    [InlineData(0xab02, 16, 50)]
    [InlineData(0xab12, 17, 50)]
    [InlineData(0xab22, 18, 50)]
    [InlineData(0xab32, 19, 50)]
    [InlineData(0xab42, 20, 50)]
    [InlineData(0xab52, 21, 50)]
    [InlineData(0xab62, 22, 50)]
    [InlineData(0xab72, 23, 50)]
    [InlineData(0xab82, 24, 50)]
    [InlineData(0xab92, 25, 50)]
    [InlineData(0xaba2, 26, 50)]
    [InlineData(0xabb2, 27, 50)]
    [InlineData(0xabc2, 28, 50)]
    [InlineData(0xabd2, 29, 50)]

    [InlineData(0xaa03, 00, 51)]
    [InlineData(0xaa13, 01, 51)]
    [InlineData(0xaa23, 02, 51)]
    [InlineData(0xaa33, 03, 51)]
    [InlineData(0xaa43, 04, 51)]
    [InlineData(0xaa53, 05, 51)]
    [InlineData(0xaa63, 06, 51)]
    [InlineData(0xaa73, 07, 51)]
    [InlineData(0xaa83, 08, 51)]
    [InlineData(0xaa93, 09, 51)]
    [InlineData(0xaaa3, 10, 51)]
    [InlineData(0xaab3, 11, 51)]
    [InlineData(0xaac3, 12, 51)]
    [InlineData(0xaad3, 13, 51)]
    [InlineData(0xaae3, 14, 51)]
    [InlineData(0xaaf3, 15, 51)]
    [InlineData(0xab03, 16, 51)]
    [InlineData(0xab13, 17, 51)]
    [InlineData(0xab23, 18, 51)]
    [InlineData(0xab33, 19, 51)]
    [InlineData(0xab43, 20, 51)]
    [InlineData(0xab53, 21, 51)]
    [InlineData(0xab63, 22, 51)]
    [InlineData(0xab73, 23, 51)]
    [InlineData(0xab83, 24, 51)]
    [InlineData(0xab93, 25, 51)]
    [InlineData(0xaba3, 26, 51)]
    [InlineData(0xabb3, 27, 51)]
    [InlineData(0xabc3, 28, 51)]
    [InlineData(0xabd3, 29, 51)]

    [InlineData(0xaa04, 00, 52)]
    [InlineData(0xaa14, 01, 52)]
    [InlineData(0xaa24, 02, 52)]
    [InlineData(0xaa34, 03, 52)]
    [InlineData(0xaa44, 04, 52)]
    [InlineData(0xaa54, 05, 52)]
    [InlineData(0xaa64, 06, 52)]
    [InlineData(0xaa74, 07, 52)]
    [InlineData(0xaa84, 08, 52)]
    [InlineData(0xaa94, 09, 52)]
    [InlineData(0xaaa4, 10, 52)]
    [InlineData(0xaab4, 11, 52)]
    [InlineData(0xaac4, 12, 52)]
    [InlineData(0xaad4, 13, 52)]
    [InlineData(0xaae4, 14, 52)]
    [InlineData(0xaaf4, 15, 52)]
    [InlineData(0xab04, 16, 52)]
    [InlineData(0xab14, 17, 52)]
    [InlineData(0xab24, 18, 52)]
    [InlineData(0xab34, 19, 52)]
    [InlineData(0xab44, 20, 52)]
    [InlineData(0xab54, 21, 52)]
    [InlineData(0xab64, 22, 52)]
    [InlineData(0xab74, 23, 52)]
    [InlineData(0xab84, 24, 52)]
    [InlineData(0xab94, 25, 52)]
    [InlineData(0xaba4, 26, 52)]
    [InlineData(0xabb4, 27, 52)]
    [InlineData(0xabc4, 28, 52)]
    [InlineData(0xabd4, 29, 52)]

    [InlineData(0xaa05, 00, 53)]
    [InlineData(0xaa15, 01, 53)]
    [InlineData(0xaa25, 02, 53)]
    [InlineData(0xaa35, 03, 53)]
    [InlineData(0xaa45, 04, 53)]
    [InlineData(0xaa55, 05, 53)]
    [InlineData(0xaa65, 06, 53)]
    [InlineData(0xaa75, 07, 53)]
    [InlineData(0xaa85, 08, 53)]
    [InlineData(0xaa95, 09, 53)]
    [InlineData(0xaaa5, 10, 53)]
    [InlineData(0xaab5, 11, 53)]
    [InlineData(0xaac5, 12, 53)]
    [InlineData(0xaad5, 13, 53)]
    [InlineData(0xaae5, 14, 53)]
    [InlineData(0xaaf5, 15, 53)]
    [InlineData(0xab05, 16, 53)]
    [InlineData(0xab15, 17, 53)]
    [InlineData(0xab25, 18, 53)]
    [InlineData(0xab35, 19, 53)]
    [InlineData(0xab45, 20, 53)]
    [InlineData(0xab55, 21, 53)]
    [InlineData(0xab65, 22, 53)]
    [InlineData(0xab75, 23, 53)]
    [InlineData(0xab85, 24, 53)]
    [InlineData(0xab95, 25, 53)]
    [InlineData(0xaba5, 26, 53)]
    [InlineData(0xabb5, 27, 53)]
    [InlineData(0xabc5, 28, 53)]
    [InlineData(0xabd5, 29, 53)]

    [InlineData(0xaa06, 00, 54)]
    [InlineData(0xaa16, 01, 54)]
    [InlineData(0xaa26, 02, 54)]
    [InlineData(0xaa36, 03, 54)]
    [InlineData(0xaa46, 04, 54)]
    [InlineData(0xaa56, 05, 54)]
    [InlineData(0xaa66, 06, 54)]
    [InlineData(0xaa76, 07, 54)]
    [InlineData(0xaa86, 08, 54)]
    [InlineData(0xaa96, 09, 54)]
    [InlineData(0xaaa6, 10, 54)]
    [InlineData(0xaab6, 11, 54)]
    [InlineData(0xaac6, 12, 54)]
    [InlineData(0xaad6, 13, 54)]
    [InlineData(0xaae6, 14, 54)]
    [InlineData(0xaaf6, 15, 54)]
    [InlineData(0xab06, 16, 54)]
    [InlineData(0xab16, 17, 54)]
    [InlineData(0xab26, 18, 54)]
    [InlineData(0xab36, 19, 54)]
    [InlineData(0xab46, 20, 54)]
    [InlineData(0xab56, 21, 54)]
    [InlineData(0xab66, 22, 54)]
    [InlineData(0xab76, 23, 54)]
    [InlineData(0xab86, 24, 54)]
    [InlineData(0xab96, 25, 54)]
    [InlineData(0xaba6, 26, 54)]
    [InlineData(0xabb6, 27, 54)]
    [InlineData(0xabc6, 28, 54)]
    [InlineData(0xabd6, 29, 54)]

    [InlineData(0xaa07, 00, 55)]
    [InlineData(0xaa17, 01, 55)]
    [InlineData(0xaa27, 02, 55)]
    [InlineData(0xaa37, 03, 55)]
    [InlineData(0xaa47, 04, 55)]
    [InlineData(0xaa57, 05, 55)]
    [InlineData(0xaa67, 06, 55)]
    [InlineData(0xaa77, 07, 55)]
    [InlineData(0xaa87, 08, 55)]
    [InlineData(0xaa97, 09, 55)]
    [InlineData(0xaaa7, 10, 55)]
    [InlineData(0xaab7, 11, 55)]
    [InlineData(0xaac7, 12, 55)]
    [InlineData(0xaad7, 13, 55)]
    [InlineData(0xaae7, 14, 55)]
    [InlineData(0xaaf7, 15, 55)]
    [InlineData(0xab07, 16, 55)]
    [InlineData(0xab17, 17, 55)]
    [InlineData(0xab27, 18, 55)]
    [InlineData(0xab37, 19, 55)]
    [InlineData(0xab47, 20, 55)]
    [InlineData(0xab57, 21, 55)]
    [InlineData(0xab67, 22, 55)]
    [InlineData(0xab77, 23, 55)]
    [InlineData(0xab87, 24, 55)]
    [InlineData(0xab97, 25, 55)]
    [InlineData(0xaba7, 26, 55)]
    [InlineData(0xabb7, 27, 55)]
    [InlineData(0xabc7, 28, 55)]
    [InlineData(0xabd7, 29, 55)]

    [InlineData(0xae00, 00, 56)]
    [InlineData(0xae10, 01, 56)]
    [InlineData(0xae20, 02, 56)]
    [InlineData(0xae30, 03, 56)]
    [InlineData(0xae40, 04, 56)]
    [InlineData(0xae50, 05, 56)]
    [InlineData(0xae60, 06, 56)]
    [InlineData(0xae70, 07, 56)]
    [InlineData(0xae80, 08, 56)]
    [InlineData(0xae90, 09, 56)]
    [InlineData(0xaea0, 10, 56)]
    [InlineData(0xaeb0, 11, 56)]
    [InlineData(0xaec0, 12, 56)]
    [InlineData(0xaed0, 13, 56)]
    [InlineData(0xaee0, 14, 56)]
    [InlineData(0xaef0, 15, 56)]
    [InlineData(0xaf00, 16, 56)]
    [InlineData(0xaf10, 17, 56)]
    [InlineData(0xaf20, 18, 56)]
    [InlineData(0xaf30, 19, 56)]
    [InlineData(0xaf40, 20, 56)]
    [InlineData(0xaf50, 21, 56)]
    [InlineData(0xaf60, 22, 56)]
    [InlineData(0xaf70, 23, 56)]
    [InlineData(0xaf80, 24, 56)]
    [InlineData(0xaf90, 25, 56)]
    [InlineData(0xafa0, 26, 56)]
    [InlineData(0xafb0, 27, 56)]
    [InlineData(0xafc0, 28, 56)]
    [InlineData(0xafd0, 29, 56)]

    [InlineData(0xae01, 00, 57)]
    [InlineData(0xae11, 01, 57)]
    [InlineData(0xae21, 02, 57)]
    [InlineData(0xae31, 03, 57)]
    [InlineData(0xae41, 04, 57)]
    [InlineData(0xae51, 05, 57)]
    [InlineData(0xae61, 06, 57)]
    [InlineData(0xae71, 07, 57)]
    [InlineData(0xae81, 08, 57)]
    [InlineData(0xae91, 09, 57)]
    [InlineData(0xaea1, 10, 57)]
    [InlineData(0xaeb1, 11, 57)]
    [InlineData(0xaec1, 12, 57)]
    [InlineData(0xaed1, 13, 57)]
    [InlineData(0xaee1, 14, 57)]
    [InlineData(0xaef1, 15, 57)]
    [InlineData(0xaf01, 16, 57)]
    [InlineData(0xaf11, 17, 57)]
    [InlineData(0xaf21, 18, 57)]
    [InlineData(0xaf31, 19, 57)]
    [InlineData(0xaf41, 20, 57)]
    [InlineData(0xaf51, 21, 57)]
    [InlineData(0xaf61, 22, 57)]
    [InlineData(0xaf71, 23, 57)]
    [InlineData(0xaf81, 24, 57)]
    [InlineData(0xaf91, 25, 57)]
    [InlineData(0xafa1, 26, 57)]
    [InlineData(0xafb1, 27, 57)]
    [InlineData(0xafc1, 28, 57)]
    [InlineData(0xafd1, 29, 57)]

    [InlineData(0xae02, 00, 58)]
    [InlineData(0xae12, 01, 58)]
    [InlineData(0xae22, 02, 58)]
    [InlineData(0xae32, 03, 58)]
    [InlineData(0xae42, 04, 58)]
    [InlineData(0xae52, 05, 58)]
    [InlineData(0xae62, 06, 58)]
    [InlineData(0xae72, 07, 58)]
    [InlineData(0xae82, 08, 58)]
    [InlineData(0xae92, 09, 58)]
    [InlineData(0xaea2, 10, 58)]
    [InlineData(0xaeb2, 11, 58)]
    [InlineData(0xaec2, 12, 58)]
    [InlineData(0xaed2, 13, 58)]
    [InlineData(0xaee2, 14, 58)]
    [InlineData(0xaef2, 15, 58)]
    [InlineData(0xaf02, 16, 58)]
    [InlineData(0xaf12, 17, 58)]
    [InlineData(0xaf22, 18, 58)]
    [InlineData(0xaf32, 19, 58)]
    [InlineData(0xaf42, 20, 58)]
    [InlineData(0xaf52, 21, 58)]
    [InlineData(0xaf62, 22, 58)]
    [InlineData(0xaf72, 23, 58)]
    [InlineData(0xaf82, 24, 58)]
    [InlineData(0xaf92, 25, 58)]
    [InlineData(0xafa2, 26, 58)]
    [InlineData(0xafb2, 27, 58)]
    [InlineData(0xafc2, 28, 58)]
    [InlineData(0xafd2, 29, 58)]

    [InlineData(0xae03, 00, 59)]
    [InlineData(0xae13, 01, 59)]
    [InlineData(0xae23, 02, 59)]
    [InlineData(0xae33, 03, 59)]
    [InlineData(0xae43, 04, 59)]
    [InlineData(0xae53, 05, 59)]
    [InlineData(0xae63, 06, 59)]
    [InlineData(0xae73, 07, 59)]
    [InlineData(0xae83, 08, 59)]
    [InlineData(0xae93, 09, 59)]
    [InlineData(0xaea3, 10, 59)]
    [InlineData(0xaeb3, 11, 59)]
    [InlineData(0xaec3, 12, 59)]
    [InlineData(0xaed3, 13, 59)]
    [InlineData(0xaee3, 14, 59)]
    [InlineData(0xaef3, 15, 59)]
    [InlineData(0xaf03, 16, 59)]
    [InlineData(0xaf13, 17, 59)]
    [InlineData(0xaf23, 18, 59)]
    [InlineData(0xaf33, 19, 59)]
    [InlineData(0xaf43, 20, 59)]
    [InlineData(0xaf53, 21, 59)]
    [InlineData(0xaf63, 22, 59)]
    [InlineData(0xaf73, 23, 59)]
    [InlineData(0xaf83, 24, 59)]
    [InlineData(0xaf93, 25, 59)]
    [InlineData(0xafa3, 26, 59)]
    [InlineData(0xafb3, 27, 59)]
    [InlineData(0xafc3, 28, 59)]
    [InlineData(0xafd3, 29, 59)]

    [InlineData(0xae04, 00, 60)]
    [InlineData(0xae14, 01, 60)]
    [InlineData(0xae24, 02, 60)]
    [InlineData(0xae34, 03, 60)]
    [InlineData(0xae44, 04, 60)]
    [InlineData(0xae54, 05, 60)]
    [InlineData(0xae64, 06, 60)]
    [InlineData(0xae74, 07, 60)]
    [InlineData(0xae84, 08, 60)]
    [InlineData(0xae94, 09, 60)]
    [InlineData(0xaea4, 10, 60)]
    [InlineData(0xaeb4, 11, 60)]
    [InlineData(0xaec4, 12, 60)]
    [InlineData(0xaed4, 13, 60)]
    [InlineData(0xaee4, 14, 60)]
    [InlineData(0xaef4, 15, 60)]
    [InlineData(0xaf04, 16, 60)]
    [InlineData(0xaf14, 17, 60)]
    [InlineData(0xaf24, 18, 60)]
    [InlineData(0xaf34, 19, 60)]
    [InlineData(0xaf44, 20, 60)]
    [InlineData(0xaf54, 21, 60)]
    [InlineData(0xaf64, 22, 60)]
    [InlineData(0xaf74, 23, 60)]
    [InlineData(0xaf84, 24, 60)]
    [InlineData(0xaf94, 25, 60)]
    [InlineData(0xafa4, 26, 60)]
    [InlineData(0xafb4, 27, 60)]
    [InlineData(0xafc4, 28, 60)]
    [InlineData(0xafd4, 29, 60)]

    [InlineData(0xae05, 00, 61)]
    [InlineData(0xae15, 01, 61)]
    [InlineData(0xae25, 02, 61)]
    [InlineData(0xae35, 03, 61)]
    [InlineData(0xae45, 04, 61)]
    [InlineData(0xae55, 05, 61)]
    [InlineData(0xae65, 06, 61)]
    [InlineData(0xae75, 07, 61)]
    [InlineData(0xae85, 08, 61)]
    [InlineData(0xae95, 09, 61)]
    [InlineData(0xaea5, 10, 61)]
    [InlineData(0xaeb5, 11, 61)]
    [InlineData(0xaec5, 12, 61)]
    [InlineData(0xaed5, 13, 61)]
    [InlineData(0xaee5, 14, 61)]
    [InlineData(0xaef5, 15, 61)]
    [InlineData(0xaf05, 16, 61)]
    [InlineData(0xaf15, 17, 61)]
    [InlineData(0xaf25, 18, 61)]
    [InlineData(0xaf35, 19, 61)]
    [InlineData(0xaf45, 20, 61)]
    [InlineData(0xaf55, 21, 61)]
    [InlineData(0xaf65, 22, 61)]
    [InlineData(0xaf75, 23, 61)]
    [InlineData(0xaf85, 24, 61)]
    [InlineData(0xaf95, 25, 61)]
    [InlineData(0xafa5, 26, 61)]
    [InlineData(0xafb5, 27, 61)]
    [InlineData(0xafc5, 28, 61)]
    [InlineData(0xafd5, 29, 61)]

    [InlineData(0xae06, 00, 62)]
    [InlineData(0xae16, 01, 62)]
    [InlineData(0xae26, 02, 62)]
    [InlineData(0xae36, 03, 62)]
    [InlineData(0xae46, 04, 62)]
    [InlineData(0xae56, 05, 62)]
    [InlineData(0xae66, 06, 62)]
    [InlineData(0xae76, 07, 62)]
    [InlineData(0xae86, 08, 62)]
    [InlineData(0xae96, 09, 62)]
    [InlineData(0xaea6, 10, 62)]
    [InlineData(0xaeb6, 11, 62)]
    [InlineData(0xaec6, 12, 62)]
    [InlineData(0xaed6, 13, 62)]
    [InlineData(0xaee6, 14, 62)]
    [InlineData(0xaef6, 15, 62)]
    [InlineData(0xaf06, 16, 62)]
    [InlineData(0xaf16, 17, 62)]
    [InlineData(0xaf26, 18, 62)]
    [InlineData(0xaf36, 19, 62)]
    [InlineData(0xaf46, 20, 62)]
    [InlineData(0xaf56, 21, 62)]
    [InlineData(0xaf66, 22, 62)]
    [InlineData(0xaf76, 23, 62)]
    [InlineData(0xaf86, 24, 62)]
    [InlineData(0xaf96, 25, 62)]
    [InlineData(0xafa6, 26, 62)]
    [InlineData(0xafb6, 27, 62)]
    [InlineData(0xafc6, 28, 62)]
    [InlineData(0xafd6, 29, 62)]
#endif
    [InlineData(0xae07, 00, 63)]
    [InlineData(0xae17, 01, 63)]
    [InlineData(0xae27, 02, 63)]
    [InlineData(0xae37, 03, 63)]
    [InlineData(0xae47, 04, 63)]
    [InlineData(0xae57, 05, 63)]
    [InlineData(0xae67, 06, 63)]
    [InlineData(0xae77, 07, 63)]
    [InlineData(0xae87, 08, 63)]
    [InlineData(0xae97, 09, 63)]
    [InlineData(0xaea7, 10, 63)]
    [InlineData(0xaeb7, 11, 63)]
    [InlineData(0xaec7, 12, 63)]
    [InlineData(0xaed7, 13, 63)]
    [InlineData(0xaee7, 14, 63)]
    [InlineData(0xaef7, 15, 63)]
    [InlineData(0xaf07, 16, 63)]
    [InlineData(0xaf17, 17, 63)]
    [InlineData(0xaf27, 18, 63)]
    [InlineData(0xaf37, 19, 63)]
    [InlineData(0xaf47, 20, 63)]
    [InlineData(0xaf57, 21, 63)]
    [InlineData(0xaf67, 22, 63)]
    [InlineData(0xaf77, 23, 63)]
    [InlineData(0xaf87, 24, 63)]
    [InlineData(0xaf97, 25, 63)]
    [InlineData(0xafa7, 26, 63)]
    [InlineData(0xafb7, 27, 63)]
    [InlineData(0xafc7, 28, 63)]
    [InlineData(0xafd7, 29, 63)]
#endregion
    public void DecodeInstruction_STD_Z_test(ushort opcode, int r, int q)
    {
        string Mnemonics = $"STD Z+{q}, r{r}";
        ushort address = 0xff;
        byte val = 0xcc;

        _ram.RAM[address + q] = 0;
        _cpu.Z = address;
        _cpu.r[r] = val;
        _cpu.PC = 100;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);

        instruction.Executable.Invoke();

        Assert.Equal(val, _ram.RAM[address + q]);
        Assert.Equal(101, _cpu.PC);
    }

    [Theory]
    #region test data
    [InlineData(0x9200, 00)]
    [InlineData(0x9210, 01)]
    [InlineData(0x9220, 02)]
    [InlineData(0x9230, 03)]
    [InlineData(0x9240, 04)]
    [InlineData(0x9250, 05)]
    [InlineData(0x9260, 06)]
    [InlineData(0x9270, 07)]
    [InlineData(0x9280, 08)]
    [InlineData(0x9290, 09)]
    [InlineData(0x92a0, 10)]
    [InlineData(0x92b0, 11)]
    [InlineData(0x92c0, 12)]
    [InlineData(0x92d0, 13)]
    [InlineData(0x92e0, 14)]
    [InlineData(0x92f0, 15)]
    [InlineData(0x9300, 16)]
    [InlineData(0x9310, 17)]
    [InlineData(0x9320, 18)]
    [InlineData(0x9330, 19)]
    [InlineData(0x9340, 20)]
    [InlineData(0x9350, 21)]
    [InlineData(0x9360, 22)]
    [InlineData(0x9370, 23)]
    [InlineData(0x9380, 24)]
    [InlineData(0x9390, 25)]
    [InlineData(0x93a0, 26)]
    [InlineData(0x93b0, 27)]
    [InlineData(0x93c0, 28)]
    [InlineData(0x93d0, 29)]
    [InlineData(0x93e0, 30)]
    [InlineData(0x93f0, 31)]
    #endregion
    public void DecodeInstruction_STS_test(ushort opcode, int r)
    {
        ushort k = 0x052d;
        string Mnemonics = $"STS 0x{k:x4}, r{r}";
        byte val = 0xce;
        int flashAddress = 100;
        _flashMemory.Write(flashAddress, opcode);
        _flashMemory.Write(flashAddress + 1, k);
        _ram.RAM[k] = 0;
        _cpu.r[r] = val;
        _cpu.PC = flashAddress;

        var instruction = _cpu.DecodeInstruction(opcode);

        Assert.Equal(Mnemonics, instruction.Mnemonics);
        Assert.Equal(2, instruction.WestedCycle);
        Assert.Equal(2, instruction.Size);
        instruction.Executable.Invoke();

        Assert.Equal(val, _ram.RAM[k]);
        Assert.Equal(flashAddress + 2, _cpu.PC);
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
