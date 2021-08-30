using MessagePack;
using System.Net;

namespace Punch.Shared.Packets;

[MessagePackObject]
public record Client
{
    [Key(0)]
    public int Magic { get; } = 0x44cf;

    [Key(1)]
    public byte[]? Address { get; set; }

    [Key(2)]
    public int Port { get; set; }

    public Client()
    {
    }

    public Client(IPEndPoint endpoint)
    {
        Address = endpoint.Address.GetAddressBytes();
        Port = endpoint.Port;
    }
}
