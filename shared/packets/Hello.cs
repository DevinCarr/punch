using System;
using System.Text;
using MessagePack;

namespace Punch.Shared.Packets;

[MessagePackObject]
public record Hello : PunchPacket
{
    [Key(1)]
    public string Message { get; set; }

    public Hello()
    {
    }

    public Hello(string message)
    {
        if (Encoding.UTF8.GetByteCount(message) > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(message), "Message should not be larger than 500 bytes");
        }
        Message = message;
    }
}
