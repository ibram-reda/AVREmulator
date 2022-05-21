﻿namespace AVREmulator;

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
    public UInt16 FetchInstruction()
    {
        return _programBus.flashMemory.Read(PC);
    }

    public Func<int> DecodeInstruction(UInt16 opcode)
    {
        var lastNipple = (opcode & 0xf000);

        switch (lastNipple)
        {

            case 0x3000: return CPi(opcode);
            case 0x4000: return SBCi(opcode);
            case 0x5000: return SUBi(opcode);
            case 0x6000: return ORi(opcode);
            case 0x7000: return ANDi(opcode);
            case 0xC000: return RJMP(opcode);
            case 0xD000: return RCALL(opcode);
            case 0xE000: return LDi(opcode);
            default:
                throw new NotImplementedException("this instruction not implemented yet");
        }
    }
    public int ExecuteInstruction(Func<int> action)
    {
        return action.Invoke();
    }
    public int RunNextInstuction()
    {
        var code = FetchInstruction();
        var action = DecodeInstruction(code);
        return ExecuteInstruction(action);
    }
    #endregion

    #region cpu Instruction factories
    public Func<int> LDi(UInt16 opcode)
    {
        // ldi Rd,k
        // 1110 kkkk dddd kkkk

        var d = opcode.GetNipple(1) | 0x10;
        if (!(16 <= d & d <= 31))
            throw new ArgumentException($"[d={d}]the distenation register should be in range r16 to r31");
        byte kh = opcode.GetNipple(2);
        byte kl = opcode.GetNipple(0);
        byte k = (byte)((kh << 4) | (kl));

        return () =>
        {
            r[d] = k;
            PC++;
            return 1;
        };

    }

    public Func<int> CPi(UInt16 opcode)
    {
        var d = opcode.GetNipple(1) | 0x10;
        if (!(16 <= d & d <= 31))
            throw new ArgumentException($"[d={d}]the distenation register should be in range r16 to r31");
        byte kh = opcode.GetNipple(2);
        byte kl = opcode.GetNipple(0);
        byte k = (byte)((kh << 4) | (kl));

        return () =>
        {
            // test for z flag
            if (k.Equals(r[d]))
                SetFlag(Flag.Z);
            else
                ClearFlag(Flag.Z);
            // test for h flag
            var rl = r[d] & 0x0f;
            var result = rl + kl;
            if ((result & 1 << 4) !=0)
                SetFlag(Flag.H);
            else
                ClearFlag(Flag.H);

            // test for v flage
            // test for n flage
            //
            throw new NotImplementedException("cpi instruction not implemented yet");
        };
    }

    public Func<int> SBCi(UInt16 opcode)
    {
        throw new NotImplementedException();
    }

    public Func<int> SUBi(UInt16 ocode)
    {
        throw new NotImplementedException();
    }

    public Func<int> ORi(UInt16 ocode)
    {
        throw new NotImplementedException();
    }

    public Func<int> ANDi(UInt16 ocode)
    {
        throw new NotImplementedException();
    }

    public Func<int> RJMP(UInt16 opcode)
    {
        int k = opcode & 0x0fff;
        // check if it negative value
        if ( (k & 0x0800) != 0)
        {
            var onceComplement = 0x0fff - k+1;
            k = onceComplement & (0x0fff);
            k = -k;
        }
        return () =>
        {
            PC = PC + k + 1;
            return 2; // west two cycle
        };
    }

    public Func<int> RCALL(UInt16 ocode)
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
    #endregion

}
