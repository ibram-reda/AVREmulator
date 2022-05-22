namespace AVREmulator;

public class CodeBurnerEmulator
{
    public static void LoadFromObjFile(string objFilePath,FlashMemory flashMemory)
    {
        throw new NotImplementedException();
    }
    public static void LoadFromHexFile(string hexFilePath, FlashMemory flashMemory)
    {
        using StreamReader streamReader = new StreamReader(hexFilePath);
        int segAddres = 0;
        while (!streamReader.EndOfStream)
        {
            string line = streamReader.ReadLine() ?? "";
            var ParsedLine = new LineDiscription(line);
            switch (ParsedLine.LineType)
            {
                case 00:
                    int absoluteAddress = (segAddres << 4) + ParsedLine.Address;
                    flashMemory.Load(absoluteAddress / 2, ParsedLine.Data.TOFlashWordArray());
                    break;
                case 01:
                    // end of transment
                    return;
                case 02:
                    // segment swtichng
                    
                    segAddres = Extentions.Combine(ParsedLine.Data[0], ParsedLine.Data[1]);
                    break;
                default: throw new ArgumentException("not suported line type!", nameof(ParsedLine.LineType));
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

        Data = new byte[Size];
        for (int i = 0; i < Size; i++)
        {
            string d = line.Substring(9 + i * 2, 2);
            Data[i] = Convert.ToByte(d, 16);
        }

        Print();
    }

    public uint Size { get; set; }
    public UInt16 Address { get; set; }
    public uint LineType { get; set; }
    public byte[] Data;

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