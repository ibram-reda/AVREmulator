namespace AVREmulator;

public class CPU
{
    /// <summary>
    /// stack pinter
    /// </summary>
    public UInt16 SP { get; set; } = UInt16.MaxValue;
    /// <summary>
    /// Program counter
    /// </summary>
    public UInt16 PC { get; set; } = 0;
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

    #region registers
    public byte r0  { get => r[0];  set => r[0] = value; }
    public byte r1  { get => r[1];  set => r[1] = value; }
    public byte r2  { get => r[2];  set => r[2] = value; }
    public byte r3  { get => r[3];  set => r[3] = value; }
    public byte r4  { get => r[4];  set => r[4] = value; }
    public byte r5  { get => r[5];  set => r[5] = value; }
    public byte r6  { get => r[6];  set => r[6] = value; }
    public byte r7  { get => r[7];  set => r[7] = value; }
    public byte r8  { get => r[8];  set => r[8] = value; }
    public byte r9  { get => r[9];  set => r[9] = value; }
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

    public UInt16 X { get => BitConverter.ToUInt16(r,26); }
    public UInt16 Y { get => BitConverter.ToUInt16(r,28); }
    public UInt16 Z { get => BitConverter.ToUInt16(r,30); }
    #endregion


    public CPU(DataBus dataBus,ProgramBus programBus)
    {
        Reset();
        _dataBus = dataBus;
        _programBus = programBus;
    }

    /// <summary>
    /// Reset the cpu 
    /// make all register take the default values
    /// </summary>
    public void Reset()
    {
        SP = UInt16.MaxValue;
        PC = 0;
        SReg = 0;
        for(int i=0;i<32;i++)
        {
            r[i] = 0;
        }
    }


    public int ExecuteInstruction()
    {
        PC++;
        throw new NotImplementedException();
        return 0;
    }

}
