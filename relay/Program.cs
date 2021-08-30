using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Punch;

IHost host = Host.CreateDefaultBuilder(args)
    .UseSystemd()
    .ConfigureServices(services =>
    {
        services.AddHostedService<Relay>();
    })
    .Build();

await host.RunAsync();
