using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MessagePack;
using Punch.Shared.Packets;

if (args.Length <= 0)
{
    Console.WriteLine("No relay host provided");
    return;
}

string relayHost = args.First();
if (relayHost is null)
{
    Console.WriteLine("Invalid relay host provided");
    return;
}

var relay = new IPEndPoint(IPAddress.Parse(relayHost), 11008);
var options = MessagePackSerializerOptions.Standard
    .WithSecurity(MessagePackSecurity.UntrustedData);
var helloBytes = MessagePackSerializer.Serialize(new Hello("PING"));

using (var client = new UdpClient())
{
    await client.SendAsync(helloBytes, helloBytes.Length, relay);

    var receivePayload = client.Receive(ref relay);
    var helloResponse = MessagePackSerializer.Deserialize<Hello>(receivePayload, options);
    if (!helloResponse.IsValid())
    {
        Console.WriteLine("Invalid Hello response");
        return;
    }
    Console.WriteLine(helloResponse.Message);

    receivePayload = client.Receive(ref relay);
    var punchPacket = MessagePackSerializer.Deserialize<Client>(receivePayload, options);
    if (punchPacket is null || punchPacket.Address is null)
    {
        Console.WriteLine("Invalid Client packet from relay");
        return;
    }
    Console.WriteLine($"[{new IPAddress(punchPacket.Address)}]:{punchPacket.Port}");
    
    var other = new IPEndPoint(new IPAddress(punchPacket.Address), punchPacket.Port);
    client.Connect(other);
    while (true)
    {
        Console.Write("> ");
        var message = Console.ReadLine();
        var messageBytes = Encoding.ASCII.GetBytes(message);
        await client.SendAsync(messageBytes, messageBytes.Length);
        var recv = await client.ReceiveAsync();
        Console.WriteLine($"< {Encoding.ASCII.GetString(recv.Buffer)}");
    }
}