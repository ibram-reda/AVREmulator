using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVREmulator;

public static class Extentions
{

    /// <summary>
    /// Get the corresponding nipple value 
    /// n3_n2_n1_n0
    /// </summary>
    /// <param name="opcode"></param>
    /// <param name="nippleNumper">number between 0 and 3</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static byte GetNipple(this UInt16 opcode,int nippleNumper)
    {
        if(nippleNumper > 3 | nippleNumper<0)
            throw new ArgumentException("nipple number shod be equal one of [0,1,2,3]");
        
        return (byte)(opcode >> (4 * nippleNumper) & 0xf);
    }

    /// <summary>
    /// Get the corresponding Bit value 
    /// zero based index
    /// </summary>
    /// <param name="opcode"></param>
    /// <param name="bitNumper">number between 0 and 15</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static bool GetBit(this UInt16 opcode, int bitNumper)
    {
        return (opcode & (1 << bitNumper)) != 0;
    }
    public static UInt16[] TOFlashWordArray(this byte[] bytes)
    {
        int bytesCount = bytes.Length;
        UInt16[] result = new UInt16[bytesCount/2];
        for (int i = 0; i < bytesCount;i+=2)
        {
            bytes.AsEnumerable().GetEnumerator().MoveNext();
            int low = bytes[i];
            int high = bytes[i+1];
            int compunation = high << 8 | low;
            result[i/2] = (UInt16)compunation;

        }
        
        return result;
    }

    public static UInt16 Combine(byte HighByte, byte LowByte)
    {
        int combined = HighByte << 8 | LowByte;
        return (UInt16)combined;
    }
}
