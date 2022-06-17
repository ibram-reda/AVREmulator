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
    public Action Executable { get; set; }
    public int  WestedCycle { get; set; }
}

/// <summary>
/// a class to Emulate the real AVR CPU,  it contains all register insed the avr cpu
/// and also contains all the logic suported by the instruction of avr cpu
/// <br/>
/// CPU is comunicate and controll ram and other peripherals through <see cref="DataBus"/> and <see cref="ProgramBus"/> to get Data and code respectivley
/// so we should pass them as a constractor dependancy
/// </summary>
public class CPU
{
    #region registers
    /// <summary>
    /// stack pinter
    /// </summary>
    public UInt16 SP { get; set; } = UInt16.MaxValue;
    /// <summary>
    /// Program counter
    /// </summary>
    public int PC { get; set; } = 0;
    /// <summary>
    /// Status Register
    /// </summary>
    public byte SReg { get; set; } = 0;

    /// <summary>
    /// array of general perpose registers r0-r31
    /// </summary>
    public readonly byte[] r = new byte[32];
    private readonly DataBus _dataBus;
    private readonly ProgramBus _programBus;

    public byte r0 { get => r[0]; set => r[0] = value; }
    public byte r1 { get => r[1]; set => r[1] = value; }
    public byte r2 { get => r[2]; set => r[2] = value; }
    public byte r3 { get => r[3]; set => r[3] = value; }
    public byte r4 { get => r[4]; set => r[4] = value; }
    public byte r5 { get => r[5]; set => r[5] = value; }
    public byte r6 { get => r[6]; set => r[6] = value; }
    public byte r7 { get => r[7]; set => r[7] = value; }
    public byte r8 { get => r[8]; set => r[8] = value; }
    public byte r9 { get => r[9]; set => r[9] = value; }
    public byte r10 { get => r[10]; set => r[10] = value; }
    public byte r11 { get => r[11]; set => r[11] = value; }
    public byte r12 { get => r[12]; set => r[12] = value; }
    public byte r13 { get => r[13]; set => r[13] = value; }
    public byte r14 { get => r[14]; set => r[14] = value; }
    public byte r15 { get => r[15]; set => r[15] = value; }
    public byte r16 { get => r[16]; set => r[16] = value; }
    public byte r17 { get => r[17]; set => r[17] = value; }
    public byte r18 { get => r[18]; set => r[18] = value; }
    public byte r19 { get => r[19]; set => r[19] = value; }
    public byte r20 { get => r[20]; set => r[20] = value; }
    public byte r21 { get => r[21]; set => r[21] = value; }
    public byte r22 { get => r[22]; set => r[22] = value; }
    public byte r23 { get => r[23]; set => r[23] = value; }
    public byte r24 { get => r[24]; set => r[24] = value; }
    public byte r25 { get => r[25]; set => r[25] = value; }
    public byte r26 { get => r[26]; set => r[26] = value; }
    public byte r27 { get => r[27]; set => r[27] = value; }
    public byte r28 { get => r[28]; set => r[28] = value; }
    public byte r29 { get => r[29]; set => r[29] = value; }
    public byte r30 { get => r[30]; set => r[30] = value; }
    public byte r31 { get => r[31]; set => r[31] = value; }

    public UInt16 X { get => BitConverter.ToUInt16(r, 26); }
    public UInt16 Y { get => BitConverter.ToUInt16(r, 28); }
    public UInt16 Z { get => BitConverter.ToUInt16(r, 30); }
    #endregion

    #region ctor
    /// <summary>
    /// Emulate the real AVR CPU,  it contains all register and instruction set behaviors
    /// </summary>
    /// <param name="dataBus">data bus which allow cpu to comunicate with ram and other peripherals sauch as Timer,ADC,URT..etc</param>
    /// <param name="programBus">program /or code bus is used to fetch next Instruction opcode from Flash memory </param>
    public CPU(DataBus dataBus, ProgramBus programBus)
    {
        Reset();
        _dataBus = dataBus;
        _programBus = programBus;
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
        return _programBus.flashMemory.Read(PC);
    }

    /// <summary>
    /// undarstnd the opcode and translate it to the corsponding instruction
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
                throw new Exception("something went wrong,should not reach that line ever");
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
    public int RunNextInstuction()
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
    /// <exception cref="ArgumentException">if the opcode desnote start with 0</exception>
    private CPUInstruction Group0(UInt16 opcode)
    {
        if (opcode.GetNipple(3) != 0)
            throw new ArgumentException("Wrong Decoding handller");
                
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
    /// <exception cref="ArgumentException">if the opcode desnote start with 1</exception>   
    private CPUInstruction Group1(ushort opcode)
    {
        throw new NotImplementedException();
    }
    /// <summary>
    /// decode any opcode start with 2
    /// </summary>
    /// <param name="opcode">opcode look like 0x2kkk</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">if the opcode desnote start with 2</exception>
    private CPUInstruction Group2(ushort opcode)
    {
        if (opcode.GetNipple(3) != 2)
            throw new ArgumentException("Wrong Decoding handller");
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
    /// <exception cref="ArgumentException">if the opcode desnote start with 8</exception>
    private CPUInstruction Group8(ushort opcode)
    {
        throw new NotImplementedException();
    }
    /// <summary>
    /// decode any opcode start with 9
    /// </summary>
    /// <param name="opcode">opcode look like 0x9kkk</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">if the opcode desnote start with 9</exception>
    private CPUInstruction Group9(ushort opcode)
    {
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
        throw new NotImplementedException();
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
            throw new ArgumentException("Wrong Decoding handller");
        return new CPUInstruction
        {
            Mnemonics = "NOP",
            Verb = "NOP",
            Executable = () => PC++,
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
                PC++;
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

                PC++;
            },
            WestedCycle = 1,

        };

    }

    private CPUInstruction MOV(UInt16 opcode)
    {
        // 0010 11rd dddd rrrr
        if ((opcode & 0x2c00) != 0x2c00)
            throw new ArgumentException("wrong Decoder handler!");
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
                PC++;
            }
        };

    }

    private CPUInstruction LDi(UInt16 opcode)
    {
        // ldi Rd,k
        // 1110 kkkk dddd kkkk
        // opcode should be cxxx
        if ((opcode & 0xF000) != 0xE000)
            throw new ArgumentException("wrong opcode handler!", nameof(opcode));

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
                PC++;
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
            throw new ArgumentException("wrong opcode handler!",nameof(opcode));

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
            Executable = () => PC += k + 1,
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
