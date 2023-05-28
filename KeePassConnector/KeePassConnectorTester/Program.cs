using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace KeePassConnectorTester
{
    class Program
    {
        private static IConfiguration? configuration;
        private static ILogger<Program>? logger;
        private static ILoggerFactory? loggerFactory;
        private static KeePassConnector.KeePassConnector? keePassConnector;

        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                    logging.AddNLog("nlog.config");
                })
                .AddSingleton((serviceProvider) =>
                {
                    IConfiguration configuration = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json")
                            .AddEnvironmentVariables()
                            .Build();

                    return configuration;
                })
                .BuildServiceProvider();

            //configure console logging
            logger = serviceProvider!.GetRequiredService<ILogger<Program>>();

            configuration = serviceProvider!.GetRequiredService<IConfiguration>();

            loggerFactory = serviceProvider!.GetRequiredService<ILoggerFactory>();

            keePassConnector = new(loggerFactory.CreateLogger<KeePassConnector.KeePassConnector>(), configuration["KeePassConnector:AESkeyName"], configuration["KeePassConnector:AESkeyValue"]);

            logger.LogInformation($"Call KeePassConnector - GetKeePassEntry method.");

            if (keePassConnector != null)
            {
                //GetMultiple Entries
                var entries = keePassConnector.GetKeePassEntries("AIMS SQL Production");
                if (entries != null)
                {
                    foreach (var entry in entries)
                    {
                        Console.WriteLine(entry.Name);
                        Console.WriteLine(entry.Login);
                        Console.WriteLine(entry.Password);
                    }
                }

                //Get Single Entry
                var keePassEntry = keePassConnector.GetKeePassEntry("AIMS SQL Production", "AimsApp");
                if (keePassEntry != null)
                {
                    Console.WriteLine(keePassEntry.Name);
                    Console.WriteLine(keePassEntry.Login);
                    Console.WriteLine(keePassEntry.Password);
                }

                //Get Single Entry
                Console.WriteLine(keePassConnector.GetKeePassEntryPassword("AIMS SQL Production", "AimsApp"));
            }
            Console.ReadKey();

        }

    }

}