namespace AVREmulator;

/// <summary>
/// CPU Executable instraction 
/// it contains a meta data about the instraction 
/// as well as The executable instruction itself
/// </summary>
public class CPUInstruction
{
    public string Mnemonics { get; set; } = string.Empty;
    public string Verb { get; set; } = string.Empty;
    public string? Operand1 { get; set; }
    public string? Operand2 { get; set; }
    public Action Executable { get; set; } = () => { };
    public int  WestedCycle { get; set; }
    /// <summary>
    /// size of instruction in Word
    /// </summary>
    public int Size { get; set; } = 1;
}

/// <summary>
/// a class to Emulate the real AVR CPU, it contains all register in the avr cpu
/// and also contains all the logic for avr cpu instructions
/// <br/>
/// CPU is communicate and control the ram and other peripherals through <see cref="DataBus"/> and <see cref="ProgramBus"/> to get Data and code respectively
/// so we should pass them as a constructor dependency
/// </summary>
public partial class CPU
{
    #region registers
    /// <summary>
    /// stack pointer
    /// </summary>
    public UInt16 SP { get; internal set; } = UInt16.MaxValue;
    /// <summary>
    /// Program counter
    /// </summary>
    public int PC { get; internal set; } = 0;
    /// <summary>
    /// Status Register
    /// </summary>
    public byte SReg { get; internal set; } = 0;

	/// <summary>
	/// array of general purpose registers r0-r31
	/// </summary>
	public readonly PartialObservableCollection<byte> r ;
    private readonly Ram _ram;
    private readonly FlashMemory _flashMemory;

    public byte r0 { get => r[0];   internal set => r[0] = value; }
    public byte r1 { get => r[1];   internal set => r[1] = value; }
    public byte r2 { get => r[2];   internal set => r[2] = value; }
    public byte r3 { get => r[3];   internal set => r[3] = value; }
    public byte r4 { get => r[4];   internal set => r[4] = value; }
    public byte r5 { get => r[5];   internal set => r[5] = value; }
    public byte r6 { get => r[6];   internal set => r[6] = value; }
    public byte r7 { get => r[7];   internal set => r[7] = value; }
    public byte r8 { get => r[8];   internal set => r[8] = value; }
    public byte r9 { get => r[9];   internal set => r[9] = value; }
    public byte r10 { get => r[10]; internal set => r[10] = value; }
    public byte r11 { get => r[11]; internal set => r[11] = value; }
    public byte r12 { get => r[12]; internal set => r[12] = value; }
    public byte r13 { get => r[13]; internal set => r[13] = value; }
    public byte r14 { get => r[14]; internal set => r[14] = value; }
    public byte r15 { get => r[15]; internal set => r[15] = value; }
    public byte r16 { get => r[16]; internal set => r[16] = value; }
    public byte r17 { get => r[17]; internal set => r[17] = value; }
    public byte r18 { get => r[18]; internal set => r[18] = value; }
    public byte r19 { get => r[19]; internal set => r[19] = value; }
    public byte r20 { get => r[20]; internal set => r[20] = value; }
    public byte r21 { get => r[21]; internal set => r[21] = value; }
    public byte r22 { get => r[22]; internal set => r[22] = value; }
    public byte r23 { get => r[23]; internal set => r[23] = value; }
    public byte r24 { get => r[24]; internal set => r[24] = value; }
    public byte r25 { get => r[25]; internal set => r[25] = value; }
    public byte r26 { get => r[26]; internal set => r[26] = value; }
    public byte r27 { get => r[27]; internal set => r[27] = value; }
    public byte r28 { get => r[28]; internal set => r[28] = value; }
    public byte r29 { get => r[29]; internal set => r[29] = value; }
    public byte r30 { get => r[30]; internal set => r[30] = value; }
    public byte r31 { get => r[31]; internal set => r[31] = value; }

    public UInt16 X
    {
        get => (UInt16)((r27<<8)+r26);
		internal set
        {
            r26 = (byte)(value & 0xff);
            r27 = (byte)(value>>8 & 0xff);
		}
    }
    public UInt16 Y 
    {
		get => (UInt16)((r29 << 8) + r28);
		internal set
		{
			r28 = (byte)(value & 0xff);
			r29 = (byte)(value >> 8 & 0xff);
		}
	}
    public UInt16 Z 
    {
		get => (UInt16)((r31 << 8) + r30);
		internal set
		{
			r30 = (byte)(value & 0xff);
			r31 = (byte)(value >> 8 & 0xff);
		}
	}
	#endregion

	#region ctor
	/// <summary>
	/// Emulate the real AVR CPU,  it contains all register and instruction set behaviors
	/// </summary>
	/// <param name="dataBus">data bus which allow cpu to communicate with ram and other peripherals such as Timer,ADC,URT..etc</param>
	/// <param name="programBus">program /or code bus is used to fetch next Instruction opcode from Flash memory </param>
	public CPU(Ram ram, FlashMemory flashMemory)
    {
		_ram = ram;
		_flashMemory = flashMemory;
        r = _ram.GetPortion(0, 32);
	}
    #endregion

    #region Main cpu functions
    /// <summary>
    /// Reset the cpu 
    /// make all register take the default values
    /// </summary>
    public void Reset()
    {
        SP = UInt16.MaxValue;
        PC = 0;
        SReg = 0;
        for (int i = 0; i < 32; i++)
        {
            r[i] = 0;
        }
    }
  
    /// <summary>
    /// Fetch Opcode from Flash Memory
    /// </summary>
    /// <returns>the opcode in location of program counter register</returns>
    public UInt16 FetchInstruction()
    {
        return _flashMemory.Read(PC++);
    }

	/// <summary>
	/// understand the opcode and translate it to the corresponding instruction
	/// </summary>
	/// <param name="opcode"></param>
	/// <returns>CPU executable instruction</returns>
	/// <exception cref="NotImplementedException"></exception>
	public CPUInstruction DecodeInstruction(UInt16 opcode)
    {
        var lastNipple = opcode.GetNipple(3);

        switch (lastNipple)
        {
            case 0x0: return Group0(opcode);
            case 0x1: return Group1(opcode);
            case 0x2: return Group2(opcode);
            case 0x3: return CPi(opcode);
            case 0x4: return SBCi(opcode);
            case 0x5: return SUBi(opcode);
            case 0x6: return ORi(opcode);
            case 0x7: return ANDi(opcode);
            case 0x8: return Group8(opcode);
            case 0x9: return Group9(opcode);
            case 0xA: return GroupA(opcode);
            case 0xB: return GroupB(opcode);
            case 0xC: return RJMP(opcode);
            case 0xD: return RCALL(opcode);
            case 0xE: return LDi(opcode);
            case 0xF: return GroupF(opcode);

            default:
                throw new Exception("something went wrong, should not reach that line ever");
        }
    }
        
    /// <summary>
    /// execute an cpu executable Instruction
    /// </summary>
    /// <param name="action"></param>
    /// <returns>number of consumed cycles</returns>
    public int ExecuteInstruction(CPUInstruction instruction)
    {
        instruction.Executable.Invoke();
        return instruction.WestedCycle;
    }
    public int RunNextInstruction()
    {
        var opcode = FetchInstruction();
        var instruction = DecodeInstruction(opcode);
        return ExecuteInstruction(instruction);
    }
	#endregion

	#region cpu Instruction factories

	/// <summary>
	/// decode any opcode start with 0
	/// </summary>
	/// <param name="opcode">opcode look like 0x0kkk</param>
	/// <returns></returns>
	/// <exception cref="WrongDecoderHandlerException">if the opcode doesn't start with 0</exception>
	private CPUInstruction Group0(UInt16 opcode)
    {
        if (opcode.GetNipple(3) != 0)
            throw new WrongDecoderHandlerException(); 
                
        switch (opcode.GetNipple(2))
        {
            case 0x0: return NOP(opcode);    
            case 0x1:return MOVW(opcode);
            default:
                throw new ArgumentException("Opcode is Reserved or not supported by Emulator");
        }

    }
	/// <summary>
	/// decode any opcode start with 1
	/// </summary>
	/// <param name="opcode">opcode look like 0x1kkk</param>
	/// <returns></returns>
	/// <exception cref="WrongDecoderHandlerException">if the opcode doesn't start with 1</exception>   
	private CPUInstruction Group1(ushort opcode)
    {
        throw new NotImplementedException();
    }
	/// <summary>
	/// decode any opcode start with 2
	/// </summary>
	/// <param name="opcode">opcode look like 0x2kkk</param>
	/// <returns></returns>
	/// <exception cref="WrongDecoderHandlerException">if the opcode doesn't start with 2</exception>
	private CPUInstruction Group2(ushort opcode)
    {
        if (opcode.GetNipple(3) != 2)
            throw new WrongDecoderHandlerException();

        switch (opcode.GetNipple(2))
        {
            case 0XC: return MOV(opcode);
            case 0XD: return MOV(opcode);
            case 0XE: return MOV(opcode);
            case 0XF: return MOV(opcode);
        }
        throw new NotImplementedException();
    }
	/// <summary>
	/// decode any opcode start with 8
	/// </summary>
	/// <param name="opcode">opcode look like 0x8kkk</param>
	/// <returns></returns>
	/// <exception cref="WrongDecoderHandlerException">if the opcode doesn't start with 8</exception>
	private CPUInstruction Group8(ushort opcode)
    {
        if (opcode.GetNipple(3) != 8)
            throw new WrongDecoderHandlerException();

        int lowNipple = opcode.GetNipple(0);
        switch (opcode.GetNipple(2))
        {
            case 0:
            case 1:
                switch (lowNipple)
                {
                    case 0: return LD_Z(opcode);
                    case int n when (n <= 7):return LDD_Z(opcode);
                    case 8: return LD_Y(opcode);
                    case int n when (n <= 0xf):return LDD_Y(opcode);
                    default: throw new UnRichabelLocationExaption();
                }
            case 2:
            case 3:
                switch (lowNipple)
                {
                    case 0: return ST_Z(opcode);
                    case int n when (n <= 7): return STD_Z(opcode);
                    case 8: return ST_Y(opcode);
                    case int n when (n <= 0xf): return STD_Y(opcode);
                    default: throw new UnRichabelLocationExaption();
                }
            case 0X4:
            case 0X5:
            case 0X8:
            case 0X9:
            case 0XC:
            case 0XD:
                switch (lowNipple)
                {
                    case int n when (n <= 7): return LDD_Z(opcode);
                    case int n when (n <= 0xf): return LDD_Y(opcode);
                    default: throw new UnRichabelLocationExaption();
                }
            case 0X6:
            case 0X7:
            case 0XA:
            case 0XB:
            case 0XE:
            case 0XF:
                switch (lowNipple)
                {
                    case int n when (n <= 7): return STD_Z(opcode);
                    case int n when (n <= 0xf): return STD_Y(opcode);
                    default: throw new UnRichabelLocationExaption();
                }

        }
        throw new UnRichabelLocationExaption();
    }
    /// <summary>
    /// decode any opcode start with 9
    /// </summary>
    /// <param name="opcode">opcode look like 0x9kkk</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">if the opcode desnote start with 9</exception>
    private CPUInstruction Group9(ushort opcode)
    {
        if (opcode.GetNipple(3) != 9)
            throw new WrongDecoderHandlerException();

        if (opcode == 0x95C8) return LPM(opcode);
        var lastnipple = opcode.GetNipple(0);
        switch (opcode.GetNipple(2))
        {
            case 0:
            case 1:                
                switch (lastnipple)
                {
                    case 0x0: return LDS(opcode);
                    case 0x1: return LD_Z(opcode);
                    case 0x2: return LD_Z(opcode);
                    case 0x3: throw new ReservedInstructionExaption();
                    case 0x4: return LPM(opcode);
                    case 0x5: return LPM(opcode);
                    case 0x6: return ELPM(opcode);
                    case 0x7: return ELPM(opcode);
                    case 0x8: throw new ReservedInstructionExaption();
                    case 0x9: return LD_Y(opcode);
                    case 0xA: return LD_Y(opcode);
                    case 0xB: throw new ReservedInstructionExaption();
                    case 0xC: return LD_X(opcode);
                    case 0xD: return LD_X(opcode);
                    case 0xE: return LD_X(opcode);
                    case 0xf: return POP(opcode);
                }
                break;
            case 2:
            case 3:
                switch (lastnipple)
                {
                    case 0x0:
                        return STS(opcode);
                    case 0x1:
                    case 0x2:
                        return ST_Z(opcode);
                    case var n when (n <= 0x8):
                        throw new ReservedInstructionExaption();
                    case 0x9:
                    case 0xA:
                        return ST_Y(opcode);
                    case 0xB:
                    case 0xC:
                    case 0xD:
                    case 0xE:
                        return ST_X(opcode);
                    case 0xF:
                        return PUSH(opcode);
                }
                break;
        }
        throw new NotImplementedException();
    }
    /// <summary>
    /// decode any opcode start with A
    /// </summary>
    /// <param name="opcode">opcode look like 0xAkkk</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">if the opcode desnote start with A</exception>
    private CPUInstruction GroupA(ushort opcode)
    {
        if (opcode.GetNipple(3) != 0XA)
            throw new WrongDecoderHandlerException();
        int lowNipple = opcode.GetNipple(0);
        switch (opcode.GetNipple(2))
        {
            case 0X0:
            case 0X1:
            case 0X4:
            case 0X5:
            case 0X8:
            case 0X9:
            case 0XC:
            case 0XD:
                switch (lowNipple)
                {
                    case int n when (n <= 7): return LDD_Z(opcode);
                    case int n when (n <= 0xf): return LDD_Y(opcode);
                    default: throw new UnRichabelLocationExaption();
                }
            case 0X2:
            case 0X3:
            case 0X6:
            case 0X7:
            case 0XA:
            case 0XB:
            case 0XE:
            case 0XF:
                switch (lowNipple)
                {
                    case int n when (n <= 7): return STD_Z(opcode);
                    case int n when (n <= 0xf): return STD_Y(opcode);
                    default: throw new UnRichabelLocationExaption();
                }
        }
        throw new UnRichabelLocationExaption();
    }
    /// <summary>
    /// decode any opcode start with B
    /// </summary>
    /// <param name="opcode">opcode look like 0xBkkk</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">if the opcode desnote start with B</exception>
    private CPUInstruction GroupB(ushort opcode)
    {
        throw new NotImplementedException();
    }
    /// <summary>
    /// decode any opcode start with F
    /// </summary>
    /// <param name="opcode">opcode look like 0xFkkk</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">if the opcode desnote start with F</exception>
    private CPUInstruction GroupF(ushort opcode)
    {
        throw new NotImplementedException();
    }

    private CPUInstruction NOP(UInt16 opcode)
    {
        if(opcode != 0)
            throw new WrongDecoderHandlerException();
        return new CPUInstruction
        {
            Mnemonics = "NOP",
            Verb = "NOP",
            Executable = () => { },
            WestedCycle = 1,
        };
    }
    /// <summary>
    /// Factory method which response for Decoding any LDI (load immadiate) instruction 
    /// <br/>
    /// LDI Rd,k 
    /// </summary>
    /// <param name="opcode"></param>
    /// <returns>Executable instraction represent the opcode</returns>
    /// <exception cref="ArgumentException"> if the opcode is not for LDI instruction </exception>  
    private CPUInstruction MOVW(UInt16 opcode)
    {
        int sourceReg= opcode.GetNipple(0)*2;
        int distReg = opcode.GetNipple(1)*2;
        return new CPUInstruction
        {
            Mnemonics = $"MOVW r{distReg}, r{sourceReg}",
            Verb = "MOVW",
            Operand1 = $"r{distReg}",
            Operand2 = $"r{sourceReg}",
            Executable = () =>
            {
                r[distReg] = r[sourceReg];
                r[distReg+1] = r[sourceReg+1];
            },
            WestedCycle = 1
        };


    }

    private CPUInstruction MULS(UInt16 opcode)
    {
        //Multiply Signed
        // muls Rd,Rr
        int d = opcode.GetNipple(1) + 0x10;
        int source = opcode.GetNipple(0) + 0x10;
        return new CPUInstruction
        {
            Mnemonics = $"MULS r{d}, r{source}",
            Verb = "MULS",
            Operand1 = $"r{d}",
            Operand2 = $"r{source}",
            Executable = () =>
            {
                int result = (int)((sbyte)r[d] * (sbyte)r[source]);
                r[0] = (byte)result;
                r[1] = (byte)(result >> 8);
                if (r0 == 0 && r1==0)
                    SetFlag(Flag.Z);
                else ClearFlag(Flag.Z);

                if (result > short.MaxValue || result < short.MinValue )
                    SetFlag(Flag.C);
                else ClearFlag(Flag.C);
            },
            WestedCycle = 1,

        };

    }

    private CPUInstruction MOV(UInt16 opcode)
    {
        // 0010 11rd dddd rrrr
        if ((opcode & 0x2c00) != 0x2c00)
            throw new WrongDecoderHandlerException();
        var d = opcode.GetNipple(1) + (opcode.GetBit(8)? 0x10:0);
        var source = opcode.GetNipple(0) + (opcode.GetBit(9) ? 0x10 : 0);

        return new CPUInstruction
        {
            Mnemonics = $"MOV r{d}, r{source}",
            Verb = "MOV",
            WestedCycle = 1,
            Executable = () =>
            {
                r[d] = r[source];
            }
        };

    }

    /// <summary>
    /// LD – Load Indirect From Data Space to Register using Index X
    /// </summary>
    /// <param name="opcode"></param>
    /// <returns></returns>
    /// <exception cref="WrongDecoderHandlerException"></exception>
    private CPUInstruction LD_X(UInt16 opcode)
    {
        // 1001_000d__dddd_1100 LD rd, X
        // 1001_000d__dddd_1101 LD rd, X+
        // 1001_000d__dddd_1110 LD rd, -X
        if ((opcode & 0xfe0c) != 0x900c)
            throw new WrongDecoderHandlerException();

        var d = (opcode >> 4) & 0x1f;

        switch (opcode.GetNipple(0))
        {
            case 0xC:
                return new CPUInstruction
                {
                    Mnemonics = $"LD r{d}, X",
                    Verb = "LD",
                    WestedCycle = 1,
                    Executable = () =>
                    {
                        r[d] = _ram.Read(X);
                    }
                };
            case 0xD:
                return new CPUInstruction
                {
                    Mnemonics = $"LD r{d}, X+",
                    Verb = "LD",
                    WestedCycle = 1,
                    Executable = () =>
                    {
                        if (d == 26 || d == 27)
                            throw new UndefinedBehaviorException(opcode, $"LD r{d}, X+");
                        r[d] = _ram.Read(X++);
                    }
                };
            case 0xE:
                return new CPUInstruction
                {
                    Mnemonics = $"LD r{d}, -X",
                    Verb = "LD",
                    WestedCycle = 2,
                    Executable = () =>
                    {
                        if (d == 26 || d == 27)
                            throw new UndefinedBehaviorException(opcode, $"LD r{d}, -X");
                        r[d] = _ram.Read(--X);
                    }
                };
        }
        throw new UnRichabelLocationExaption();
    }
    /// <summary>
    /// LD – Load Indirect From Data Space to Register using Index Y
    /// </summary>
    /// <param name="opcode"></param>
    /// <returns></returns>
    /// <exception cref="WrongDecoderHandlerException"></exception>
    /// <exception cref="UndefinedBehaviorException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    private CPUInstruction LD_Y(UInt16 opcode)
    {
        // page 71 in manuale
        //1000_000d_dddd_1000  LD rd, Y
        //1001_000d_dddd_1001  LD rd, Y+
        //1001_000d_dddd_1010  LD rd, -Y
        var mask = opcode & 0xfe0f;
        if (! ( mask == 0x8008 ||
             mask == 0x9009 ||
             mask == 0x900A) )
        {
            throw new WrongDecoderHandlerException();
        }

        var d = (opcode >> 4) & 0x1f;
        switch (mask)
        {
            case 0x8008:
                return new CPUInstruction
                {
                    Mnemonics = $"LD r{d}, Y",
                    Verb = "LD",
                    WestedCycle = 1,
                    Executable = () =>
                    {
                        r[d] = _ram.Read(Y);
                    }
                };
            case 0x9009:
                return new CPUInstruction
                {
                    Mnemonics = $"LD r{d}, Y+",
                    Verb = "LD",
                    WestedCycle = 1,
                    Executable = () =>
                    {
                        if (d == 28 || d == 29)
                            throw new UndefinedBehaviorException(opcode);
                        r[d] = _ram.Read(Y++);
                    }
                };
            case 0x900A:
                return new CPUInstruction
                {
                    Mnemonics = $"LD r{d}, -Y",
                    Verb = "LD",
                    WestedCycle = 1,
                    Executable = () =>
                    {
                        if (d == 28 || d == 29)
                            throw new UndefinedBehaviorException(opcode);
                        r[d] = _ram.Read(--Y);
                    }
                };
        }

        throw new UnRichabelLocationExaption();
    }

    /// <summary>
    /// LDD – Load Indirect with displasment From Data Space to Register using Index Y
    /// </summary>
    /// <param name="opcode"></param>
    /// <returns></returns>
    private CPUInstruction LDD_Y(UInt16 opcode)
    {
        // page 71
        //10q0_qq0d_dddd_1qqq  LDD rd, Y+q ;
        if ((opcode & 0b1101_0010_0000_1000) != 0x8008)
            throw new WrongDecoderHandlerException();

        int d = (opcode >> 4) & 0x1f;
        int q = ( (opcode & 0b0000_0111) | ((opcode >> 7) & 0b0001_1000) | ((opcode >> 8) & 0b0010_0000) ) & 0b0011_1111;
        return new CPUInstruction
        {
            Mnemonics = $"LDD r{d}, Y+{q}",
            Verb = "LDD",
            WestedCycle = 2,
            Executable = () =>
            {
                r[d] = _ram.Read(Y + q);
            }
        };
    }
    
    /// <summary>
    /// LD (LDD) – Load Indirect From Data Space to Register using Index Z
    /// </summary>
    /// <param name="opcode"></param>
    /// <returns></returns>
    /// <exception cref="WrongDecoderHandlerException"></exception>
    /// <exception cref="UndefinedBehaviorException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    private CPUInstruction LD_Z(UInt16 opcode)
    {
        //1000_000d_dddd_0000  LD rd, Z
        //1001_000d_dddd_0001  LD rd, Z+
        //1001_000d_dddd_0010  LD rd, -Z        
        var mask = opcode & 0xfe0f;
        if (!(mask == 0x8000 ||
             mask == 0x9001 ||
             mask == 0x9002))
        {
            throw new WrongDecoderHandlerException();
        }

        var d = (opcode >> 4) & 0x1f;
        switch (mask)
        {
            case 0x8000:
                return new CPUInstruction
                {
                    Mnemonics = $"LD r{d}, Z",
                    Verb = "LD",
                    WestedCycle = 1,
                    Executable = () =>
                    {
                        r[d] = _ram.Read(Z);
                    }
                };
            case 0x9001:
                return new CPUInstruction
                {
                    Mnemonics = $"LD r{d}, Z+",
                    Verb = "LD",
                    WestedCycle = 1,
                    Executable = () =>
                    {
                        if (d == 30 || d == 31)
                            throw new UndefinedBehaviorException(opcode);
                        r[d] = _ram.Read(Z++);
                    }
                };
            case 0x9002:
                return new CPUInstruction
                {
                    Mnemonics = $"LD r{d}, -Z",
                    Verb = "LD",
                    WestedCycle = 2,
                    Executable = () =>
                    {
                        if (d == 30 || d == 31)
                            throw new UndefinedBehaviorException(opcode);
                        r[d] = _ram.Read(--Z);
                    }
                };
        }

        throw new UnRichabelLocationExaption();
    }

    /// <summary>
    /// LDD – Load Indirect with displasment From Data Space to Register using Index Z
    /// </summary>
    /// <param name="opcode"></param>
    /// <returns></returns>
    /// <exception cref="WrongDecoderHandlerException"></exception>
    private CPUInstruction LDD_Z(UInt16 opcode)
    {
        //10q0_qq0d_dddd_0qqq  LDD rd, Z+q ;
        if ((opcode & 0b1101_0010_0000_1000) != 0x8000)
            throw new WrongDecoderHandlerException();

        int d = (opcode >> 4) & 0x1f;
        int q = ((opcode & 0x7) | ((opcode >> 7) & 0x18) | ((opcode >> 8) & 0x20)) & 0x3f;
        return new CPUInstruction
        {
            Mnemonics = $"LDD r{d}, Z+{q}",
            Verb = "LDD",
            WestedCycle = 2,
            Executable = () =>
            {
                r[d] = _ram.Read(Z + q);
            }
        };
    }
    /// <summary>
    /// LDS – Load Direct from Data Space
    /// </summary>
    private CPUInstruction LDS(UInt16 opcode)
    {
        // page 74 in manual
        // 1001_000d_dddd_0000  <<< 2word 
        // page 75
        // 1001_0kkk_dddd_kkkk   << 1 word & 1 cycle not implemented yet
        if ((opcode & 0xfe0f) != 0x9000)
            throw new WrongDecoderHandlerException();

        int d = (opcode >> 4) & 0x1f;
        UInt16 k = FetchInstruction(); // load the parameter
        return new CPUInstruction
        {
            Mnemonics = $"LDS r{d}, 0x{k:x4}",
            Verb = "LDS",
            WestedCycle = 2,
            Size = 2,
            Executable = () =>
            {
                r[d] = _ram.Read(k);
            }
        };
    }

    /// <summary>
    /// ST – Store Indirect From Register to Data Space using Index X
    /// </summary>
    /// <param name="opcode"></param>
    /// <returns></returns>
    /// <exception cref="WrongDecoderHandlerException"></exception>
    private CPUInstruction ST_X(UInt16 opcode)
    {
        // 1001_001r_rrrr_1100 LD X , Rr
        // 1001_001r_rrrr_1101 LD X+, Rr
        // 1001_001r_rrrr_1110 LD -X, Rr
        if ((opcode & 0xfe0c) != 0x920c)
            throw new WrongDecoderHandlerException();

        var r = (opcode >> 4) & 0x1f;

        switch (opcode.GetNipple(0))
        {
            case 0xC:
                return new CPUInstruction
                {
                    Mnemonics = $"ST X, r{r}",
                    Verb = "ST",
                    WestedCycle = 1,
                    Size = 1,
                    Executable = () =>
                    {
                        _ram.Write(X, this.r[r]);
                    }
                };
            case 0xD:
                return new CPUInstruction
                {
                    Mnemonics = $"ST X+, r{r}",
                    Verb = "ST",
                    WestedCycle = 1,
                    Executable = () =>
                    {
                        if (r == 26 || r == 27)
                            throw new UndefinedBehaviorException(opcode);
                        _ram.Write(X++, this.r[r]);
                    }
                };
            case 0xE:
                return new CPUInstruction
                {
                    Mnemonics = $"ST -X, r{r}",
                    Verb = "ST",
                    WestedCycle = 2,
                    Executable = () =>
                    {
                        if (r == 26 || r == 27)
                            throw new UndefinedBehaviorException(opcode);
                        _ram.Write(--X, this.r[r]);
                    }
                };
        }
        throw new UnRichabelLocationExaption();
    }

    private CPUInstruction ST_Y(UInt16 opcode)
    {
        // page 119 in manual
        //1000_001d_dddd_1000  ST  Y, Rr
        //1001_001d_dddd_1001  ST Y+, Rr
        //1001_001d_dddd_1010  ST -Y, Rr
        var mask = opcode & 0xfe0f;
        if (!(mask == 0x8208 ||
             mask == 0x9209 ||
             mask == 0x920A))
        {
            throw new WrongDecoderHandlerException();
        }

        var r = (opcode >> 4) & 0x1f;
        switch (mask)
        {
            case 0x8208:
                return new CPUInstruction
                {
                    Mnemonics = $"ST Y, r{r}",
                    Verb = "ST",
                    WestedCycle = 1,
                    Executable = () =>
                    {
                        _ram.Write(Y, this.r[r]);
                    }
                };
            case 0x9209:
                return new CPUInstruction
                {
                    Mnemonics = $"ST Y+, r{r}",
                    Verb = "ST",
                    WestedCycle = 1,
                    Executable = () =>
                    {
                        if (r == 28 || r == 29)
                            throw new UndefinedBehaviorException(opcode);
                        _ram.Write(Y++, this.r[r]);
                    }
                };
            case 0x920A:
                return new CPUInstruction
                {
                    Mnemonics = $"ST -Y, r{r}",
                    Verb = "ST",
                    WestedCycle = 1,
                    Executable = () =>
                    {
                        if (r == 28 || r == 29)
                            throw new UndefinedBehaviorException(opcode);
                        _ram.Write(--Y, this.r[r]);
                    }
                };
        }

        throw new UnRichabelLocationExaption();
    }

    /// <summary>
    /// ST – Store Indirect From Register to Data Space using Index Z
    /// </summary>
    /// <param name="opcode"></param>
    /// <returns></returns>
    /// <exception cref="WrongDecoderHandlerException"></exception>
    /// <exception cref="UndefinedBehaviorException"></exception>
    /// <exception cref="UnRichabelLocationExaption"></exception>
    private CPUInstruction ST_Z(UInt16 opcode)
    {
        //1000_001r_rrrr_0000  ST  Z, Rr 
        //1001_001r_rrrr_0001  ST Z+, Rr
        //1001_001r_rrrr_0010  ST -Z, Rr         
        var mask = opcode & 0xfe0f;
        if (!(mask == 0x8200 ||
             mask == 0x9201 ||
             mask == 0x9202))
        {
            throw new WrongDecoderHandlerException();
        }

        var r = (opcode >> 4) & 0x1f;
        switch (mask)
        {
            case 0x8200:
                return new CPUInstruction
                {
                    Mnemonics = $"ST Z, r{r}",
                    Verb = "ST",
                    WestedCycle = 1,
                    Executable = () =>
                    {
                        _ram.Write(Z, this.r[r]);
                    }
                };
            case 0x9201:
                return new CPUInstruction
                {
                    Mnemonics = $"ST Z+, r{r}",
                    Verb = "ST",
                    WestedCycle = 1,
                    Executable = () =>
                    {
                        if (r == 30 || r == 31)
                            throw new UndefinedBehaviorException(opcode);
                        _ram.Write(Z++, this.r[r]);
                    }
                };
            case 0x9202:
                return new CPUInstruction
                {
                    Mnemonics = $"ST -Z, r{r}",
                    Verb = "ST",
                    WestedCycle = 2,
                    Executable = () =>
                    {
                        if (r == 30 || r == 31)
                            throw new UndefinedBehaviorException(opcode);
                        _ram.Write(--Z, this.r[r]);
                    }
                };
        }

        throw new UnRichabelLocationExaption();
    }


    /// <summary>
    /// STD – Store (with displasment) Indirect  From Register to Data Space using Index Y
    /// </summary>
    /// <param name="opcode"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    /// <exception cref="WrongDecoderHandlerException"></exception>
    private CPUInstruction STD_Y(UInt16 opcode)
    {
        // page 119
        //10q0_qq1r_rrrr_1qqq  STD Y+q, Rr;
        if ((opcode & 0b1101_0010_0000_1000) != 0x8208)
            throw new WrongDecoderHandlerException();

        int r = (opcode >> 4) & 0x1f;
        int q = ((opcode & 0b0000_0111) | ((opcode >> 7) & 0b0001_1000) | ((opcode >> 8) & 0b0010_0000)) & 0b0011_1111;
        if (q == 0) return ST_Y(opcode);
        return new CPUInstruction
        {
            Mnemonics = $"STD Y+{q}, r{r}",
            Verb = "STD",
            WestedCycle = 2,
            Executable = () =>
            {
                _ram.Write(Y + q, this.r[r]);
            }
        };
    }

    private CPUInstruction STD_Z(UInt16 opcode)
    {
        //10q0_qq1r_rrrr_0qqq  STD Z+q, Rr ;
        if ((opcode & 0b1101_0010_0000_1000) != 0x8200)
            throw new WrongDecoderHandlerException();

        int r = (opcode >> 4) & 0x1f;
        int q = ((opcode & 0x7) | ((opcode >> 7) & 0x18) | ((opcode >> 8) & 0x20)) & 0x3f;
        return new CPUInstruction
        {
            Mnemonics = $"STD Z+{q}, r{r}",
            Verb = "STD",
            WestedCycle = 2,
            Executable = () =>
            {
                _ram.Write(Z + q, this.r[r]);
            }
        };
    }

    private CPUInstruction STS(UInt16 opcode)
    {
        // page 121 in manual
        // 1001_001d_dddd_0000  ; STS K, Rr <<< 2word  
        // page 122
        // 1001_1kkk_dddd_kkkk   << 1 word & 1 cycle not implemented yet
        if ((opcode & 0xfe0f) != 0x9200)
            throw new WrongDecoderHandlerException();

        int d = (opcode >> 4) & 0x1f;
        UInt16 k = FetchInstruction(); // load the parameter k
        return new CPUInstruction
        {
            Mnemonics = $"STS 0x{k:x4}, r{d}",
            Verb = "STS",
            WestedCycle = 2,
            Size = 2,
            Executable = () =>
            {
                _ram.Write(k, this.r[d]);
            }
        };
    }

    /// <summary>
    /// LPM – Load Program Memory
    /// </summary>
    /// <param name="opcode"></param>
    /// <returns></returns>
    /// <exception cref="WrongDecoderHandlerException"></exception>
    /// <exception cref="UndefinedBehaviorException"></exception>
    /// <exception cref="UnRichabelLocationExaption"></exception>
    private CPUInstruction LPM(UInt16 opcode)
    {
        // instruction set manual page 76
        // 1001_0101_1100_1000   LPM ; R0 implied
        // 1001_000d_dddd_0100   LPM rd, Z
        // 1001_000d_dddd_0101   LPM rd, Z+
        var mask = opcode & 0xfe0f;
        if (!(opcode == 0x95C8 ||
              mask == 0x9004 ||
              mask == 0x9005))
            throw new WrongDecoderHandlerException();

        if (opcode == 0x95C8)
            return new CPUInstruction
            {
                Mnemonics = "LPM",
                Verb = "LPM",
                WestedCycle = 3,
                Executable = () =>
                {
                    var val = _flashMemory.Read(Z >> 1);
                    r0 = Z.GetBit(0) ? (byte)(val >> 8):(byte)val;
                }
            };
        
        var d = (opcode >> 4) & 0x1f;
        switch (mask)
        {
            case 0x9004:
                return new CPUInstruction
                {
                    Mnemonics = $"LPM r{d}, Z",
                    Verb = "LPM",
                    WestedCycle = 3,
                    Executable = () =>
                    {
                        var val = _flashMemory.Read(Z >> 1);
                        r[d] = Z.GetBit(0) ? (byte)(val >> 8) : (byte)val;
                    }

                };
            case 0x9005:
                return new CPUInstruction
                {
                    Mnemonics = $"LPM r{d}, Z+",
                    Verb = "LPM",
                    WestedCycle = 3,
                    Executable = () =>
                    {
                        if (d == 30 || d == 31)
                            throw new UndefinedBehaviorException(opcode);

                        var val = _flashMemory.Read(Z >> 1);
                        r[d] = Z.GetBit(0) ? (byte)(val >> 8) : (byte)val;
                        Z++;
                    }

                };

        }
        throw new UnRichabelLocationExaption();
    }

    private CPUInstruction ELPM(UInt16 opcode)
    {
        throw new NotImplementedException();
    }

    private CPUInstruction POP(UInt16 opcode)
    {
        //1001_000d_dddd_1111
        if ((opcode & 0xfe0f) != 0x900f)
            throw new WrongDecoderHandlerException();
        int d = (opcode >> 4) & 0x1f;
        return new CPUInstruction
        {
            Mnemonics = $"POP r{d}",
            Verb = "POP",
            WestedCycle = 2,
            Executable = () =>
            {
                r[d] = _ram.Read(++SP);
            }
        };
    }

    private CPUInstruction PUSH(UInt16 opcode)
    {
        // page 90
        //1001_001d_dddd_1111
        if ((opcode & 0xfe0f) != 0x920f)
            throw new WrongDecoderHandlerException();
        int d = (opcode >> 4) & 0x1f;
        return new CPUInstruction
        {
            Mnemonics = $"PUSH r{d}",
            Verb = "PUSH",
            WestedCycle = 2,
            Executable = () =>
            {
                _ram.Write(SP--, this.r[d]);
            }
        };
    }
    private CPUInstruction LDi(UInt16 opcode)
    {
        // ldi Rd,k
        // 1110 kkkk dddd kkkk
        // opcode should be cxxx
        if ((opcode & 0xF000) != 0xE000)
            throw new WrongDecoderHandlerException();

        var d = opcode.GetNipple(1) | 0x10;
        if (!(16 <= d & d <= 31))
            throw new ArgumentException($"[d={d}]the distenation register should be in range r16 to r31");
        byte kh = opcode.GetNipple(2);
        byte kl = opcode.GetNipple(0);
        byte k = (byte)((kh << 4) | (kl));

        return new CPUInstruction
        {
            Mnemonics = $"LDI r{d}, 0x{k:X2}",
            Verb ="LDI",
            Operand1=$"r{d}",
            Operand2=$"0x{k:x2}",
            Executable = () =>
            {
                r[d] = k;
            },
            WestedCycle = 1
        };               

    }

    private CPUInstruction CPi(UInt16 opcode)
    {
        var d = opcode.GetNipple(1) | 0x10;
        if (!(16 <= d & d <= 31))
            throw new ArgumentException($"[d={d}]the distenation register should be in range r16 to r31");
        byte kh = opcode.GetNipple(2);
        byte kl = opcode.GetNipple(0);
        byte k = (byte)((kh << 4) | (kl));

        throw new NotImplementedException("cpi instruction not implemented yet");
        //return () =>
        //{
        //    // test for z flag
        //    if (k.Equals(r[d]))
        //        SetFlag(Flag.Z);
        //    else
        //        ClearFlag(Flag.Z);
        //    // test for h flag
        //    var rl = r[d] & 0x0f;
        //    var result = rl + kl;
        //    if ((result & 1 << 4) !=0)
        //        SetFlag(Flag.H);
        //    else
        //        ClearFlag(Flag.H);
        //
        //    // test for v flage
        //    // test for n flage
        //    //
        //};
    }

    private CPUInstruction SBCi(UInt16 opcode)
    {
        throw new NotImplementedException();
    }

    private CPUInstruction SUBi(UInt16 ocode)
    {
        throw new NotImplementedException();
    }

    private CPUInstruction ORi(UInt16 ocode)
    {
        throw new NotImplementedException();
    }

    private CPUInstruction ANDi(UInt16 ocode)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Factory method which response for Decoding any RJMP (relative jump) instruction 
    /// <br/>
    /// RJMP z
    /// </summary>
    /// <param name="opcode"></param>
    /// <returns>Executable instraction represent the opcode</returns>
    /// <exception cref="ArgumentException"></exception>
    private CPUInstruction RJMP(UInt16 opcode)
    {
        // opcode should be cxxx
        if ((opcode & 0xf000) != 0xc000)
            throw new WrongDecoderHandlerException();

        int k = opcode & 0x0fff;
        // check if it negative value
        if ( (k & 0x0800) != 0)
        {
            var onceComplement = 0x0fff - k+1;
            k = onceComplement & (0x0fff);
            k = -k;
        }

        return new CPUInstruction
        {
            Mnemonics = $"RJMP 0X{k:x3}",
            Verb = "RJMP",
            Operand1 = $"0x{k:x3}",
            Executable = () => PC += k ,
            WestedCycle = 2
        };
    }

    private CPUInstruction RCALL(UInt16 ocode)
    {
        throw new NotImplementedException();
    }    
    #endregion

    #region status Register helper
    public enum Flag
    {
        C = 0,
        Z = 1,
        N = 2,
        V = 3,
        S = 4,
        H = 5,
        T = 6,
        I = 7,
    };
    /// status regesters
    public void SetFlag(Flag flag) => SReg = (byte)(SReg | 1<<(int)flag);
    public void ClearFlag(Flag flag) => SReg = (byte)(SReg & ~(1<<(int)flag));

    public bool GetFlag(Flag flag) => (SReg & (1 << (int)flag)) == 1;
    #endregion

}
