
using ConsoleApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(
                    (context, configurationBuilder) =>
                    {
                        configurationBuilder.SetBasePath(args[0])
                             .AddJsonFile("appsettings.json", false);
                    })
    .ConfigureServices((context, services) =>
    {
        
        services.AddSingleton<ServiceA>();
    })
    .Build();


var serviceA = host.Services.GetRequiredService<ServiceA>();
serviceA.DoSomething();