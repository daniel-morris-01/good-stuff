using NLog;
using NLog.Extensions.Logging;
using SyncDBTables;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options=>
    {
        options.ServiceName = "Asp Identity DB Syncronizer";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddSingleton<DbSyncronizer>();
        services.AddLogging(loggingBuilder =>
        {
            // configure Logging with NLog
            loggingBuilder.ClearProviders();
            loggingBuilder.AddNLog();
            loggingBuilder.AddConsole();
        });
    })
    .Build();

await host.RunAsync();
