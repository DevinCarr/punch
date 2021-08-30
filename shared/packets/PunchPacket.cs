using MessagePack;

namespace Punch.Shared.Packets;

[MessagePackObject]
public abstract record PunchPacket
{
    private const int MAGIC = 0x44cf;

    [Key(0)]
    public int Magic { get; set; } = MAGIC;

    public virtual bool IsValid() => Magic == MAGIC;
}