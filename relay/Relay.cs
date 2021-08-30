using MessagePack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Punch.Shared.Packets;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Punch;

public class Relay : BackgroundService
{
    private readonly ILogger<Relay> _logger;

    private readonly int _port;
    private readonly Dictionary<string, IPEndPoint> _clients;
    private readonly MessagePackSerializerOptions _msgpackOptions;
    private readonly byte[] _helloMsg;

    public Relay(IConfiguration configuration, ILogger<Relay> logger)
    {
        _logger = logger;
        _port = configuration.GetValue<int>("Punch:Port");
        _clients = new Dictionary<string, IPEndPoint>();
        _msgpackOptions = MessagePackSerializerOptions.Standard
            .WithSecurity(MessagePackSecurity.UntrustedData);
        _helloMsg = MessagePackSerializer.Serialize(new Hello("PONG"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Punch running on: {_port}");
        using (var relay = new UdpClient(_port))
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var receivePayload = await relay.ReceiveAsync();
                var client = receivePayload.RemoteEndPoint;
                var logLine = $"[{client.Address}]:{client.Port}";
                // Current Hello size shouldn't exceed 500 bytes + msgpack serialzation size
                if (receivePayload.Buffer.Length > 510)
                {
                    _logger.LogWarning(logLine + " [SYN] Message size too large");
                    continue;
                }
                Hello helloResponse;
                try
                {
                    helloResponse = MessagePackSerializer.Deserialize<Hello>(receivePayload.Buffer, _msgpackOptions);
                }
                catch (MessagePackSerializationException)
                {
                    _logger.LogWarning(logLine + " [SYN] Unable to deserialize");
                    continue;
                }
                if (!helloResponse.IsValid())
                {
                    _logger.LogWarning(logLine + " [SYN] Invalid client Hello");
                    continue;
                }
                var key = helloResponse.Message;
                _logger.LogInformation($"[HELLO] {key} {logLine}");
                await relay.SendAsync(_helloMsg, _helloMsg.Length, client);

                // Check if a client exists for the request
                if (_clients.TryGetValue(key, out var remoteClient))
                {
                    var clientResponse = MessagePackSerializer.Serialize(new Client(remoteClient));
                    await relay.SendAsync(clientResponse, clientResponse.Length, client);
                    clientResponse = MessagePackSerializer.Serialize(new Client(client));
                    await relay.SendAsync(clientResponse, clientResponse.Length, remoteClient);
                    _clients.Remove(key);
                    _logger.LogInformation($"[RELAY] [{client.Address}]:{client.Port} [{remoteClient.Address}]:{remoteClient.Port}");
                    continue;
                }

                _clients.Add(key, client);
            }
        }
    }
}
