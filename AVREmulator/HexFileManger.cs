namespace AVREmulator;

public class HexFileManger
{
    public static void Load(string hexFilePath, FlashMemory flashMemory)
    {
        using StreamReader streamReader = new StreamReader(hexFilePath);
        while (!streamReader.EndOfStream)
        {
            string line = streamReader.ReadLine() ?? "";
            var ParsedLine = new LineDiscription(line);
            switch (ParsedLine.LineType)
            {
                case 00:
                    flashMemory.Load(ParsedLine.Address / 2, ParsedLine.Data);
                    break;
                case 01:
                    // end of transment
                    return;
                case 02:
                    // segment swtichng
                    throw new NotImplementedException("Extended Intel Hex not suported yet");
                default: throw new ArgumentException("not known line type!", nameof(ParsedLine.LineType));
            }


        }
    }
}


internal class LineDiscription
{
    public LineDiscription(string line)
    {
        if (!line.StartsWith(':'))
            throw new ArgumentException("in intel hexfile line should start with ':'");

        string size = line.Substring(1, 2);
        Size = Convert.ToUInt16(size, 16);

        string address = line.Substring(3, 4);
        Address = Convert.ToUInt16(address, 16);

        string lineType = line.Substring(7, 2);
        LineType = Convert.ToUInt16(lineType, 16);

        Data = new UInt16[Size / 2];
        for (int i = 0; i < Size / 2; i++)
        {
            string Low = line.Substring(9 + i * 4, 2);
            string High = line.Substring(11 + i * 4, 2);
            string val = High + Low;
            Data[i] = Convert.ToUInt16(val, 16);
        }

        Print();
    }

    public uint Size { get; set; }
    public UInt16 Address { get; set; }
    public uint LineType { get; set; }
    public UInt16[] Data;

    public byte GetCheckSum()
    {
        throw new NotImplementedException();
    }
    public void Print()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(Address.ToString("x4") + "\t");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(LineType.ToString("X2") + "\t");
        Console.ForegroundColor = ConsoleColor.White;
        foreach (var val in Data) Console.Write(val.ToString("x4") + "\t");
        Console.WriteLine();
    }
}