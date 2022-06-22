using System.Runtime.Serialization;

namespace AVREmulator;

public class UndifiendBehaviorException : Exception
{
    public UndifiendBehaviorException(ushort opcode) : base($"opcode = 0x{opcode:x4}, has no behavior defiend by atmel")
    {
    }
    public UndifiendBehaviorException(ushort opcode,string instruction) :base($"opcode = 0x{opcode:x4} decoded to: `{instruction}`, has no behavior defiend by atmel")
    {
    }
}


public class WrongDecoderHandlerException : ArgumentException
{
    public WrongDecoderHandlerException() : base("wrong Decoder handler! hav been called! ")
    {
    }

    public WrongDecoderHandlerException(string? message) : base(message)
    {
    }

    public WrongDecoderHandlerException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected WrongDecoderHandlerException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

public class ReservedInstructionExaption : Exception
{
    public ReservedInstructionExaption()
    {
    }

    public ReservedInstructionExaption(string? message) : base(message)
    {
    }

    public ReservedInstructionExaption(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public class UnRichabelLocationExaption : Exception
{
    public UnRichabelLocationExaption() : base("the code should not rich this location ever")
    {
    }
}