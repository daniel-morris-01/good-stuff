using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Spectre.Console;
using SyncAvatar;
using SyncAvatar.HttpClients;
using SyncDBTables;
using System.Data.SqlClient;
using System.Diagnostics;

using IHost host = Host.CreateDefaultBuilder(args)
     .ConfigureServices(services =>
     {
         services.AddSingleton<ConnectionManager>();
         services.AddSingleton<DbSyncronizer>();
         services.AddSingleton<OriginProfileActivityHttpClient>();
         services.AddSingleton<TargetProfileActivityHttpClient>();
         services.AddSingleton<SyncAvatarManager>();
         services.AddSingleton<KeePassConnector.KeePassConnector>();
         services.AddLogging(loggingBuilder =>
         {
             // configure Logging with NLog
             loggingBuilder.ClearProviders();
             loggingBuilder.AddNLog();
             loggingBuilder.AddConsole();
         });
     })
    .Build();
ILogger logger = host.Services!.GetRequiredService<ILogger<Program>>();
ILoggerFactory loggerFactory = host.Services!.GetRequiredService<ILoggerFactory>();

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

ConnectionManager connectionManager = host.Services!.GetRequiredService<ConnectionManager>();
DbSyncronizer dbSyncronizer = host.Services!.GetRequiredService<DbSyncronizer>();
SyncAvatarManager syncAvatarManager = host.Services!.GetRequiredService<SyncAvatarManager>();
KeePassConnector.KeePassConnector? keePassConnector = null;

try
{
    keePassConnector = new(loggerFactory.CreateLogger<KeePassConnector.KeePassConnector>(),
                                                        configuration["KeePassConnector:AESkeyName"],
                                                        configuration["KeePassConnector:AESkeyValue"]);
}
catch
{
    logger.LogError($"Failed to create KeePassConnector instance.");
}

//Ask for the origin environment to copy Avtar from
var originEnvironment = AnsiConsole.Prompt(
   new SelectionPrompt<string>()
       .Title("Where do you want to copy Avatar [green]from[/]?")
       .AddChoices(new[] {
            "Production", "Staging", "Test", "Local",
       }));

string originPassword = "";
if (originEnvironment != "Local")
{
    try
    {
        string keePassUrl = "AIMS SQL " + originEnvironment;
        string login = connectionManager.GetSqlConnectionUserIdByEnvironment(originEnvironment);
        if(keePassConnector != null)
        {
            originPassword = keePassConnector.GetKeePassEntryPassword(keePassUrl, login)!;
        }
    }
    catch (Exception)
    {
        logger.LogError("Failed to query KeePass via KeePassConnector.");
    }
    if (string.IsNullOrEmpty(originPassword))
    {
        originPassword = AnsiConsole.Prompt(
            new TextPrompt<string>($"Failed to query KeePass. Please provide password for [green]{originEnvironment}[/] database connection:")
            .Secret());
    }
}

using var originConn = new SqlConnection(connectionManager.GetSqlConnectionByEnvironment(originEnvironment, originPassword));

try
{
    originConn.Open();
}
catch (Exception e)
{
    logger.LogError($"Failed to connect to {originEnvironment} Database. The following exception occurred. Exception : {e}");
    AnsiConsole.MarkupLine($"Failed to connect to [green]{originEnvironment}[/] Database.");
    Console.Read();
    return;
}
// Echo the selected origin environment back to the terminal
AnsiConsole.MarkupLine("Copy Avatar from: [yellow]{0}[/]", originEnvironment);

//Ask for the origin environment to copy Avtar to
var targetEnvironment = AnsiConsole.Prompt(
   new SelectionPrompt<string>()
       .Title("Where do you want to copy Avatar [green]to[/]?")
       .AddChoices(new[] {
            "Production", "Staging", "Test", "Local",
       }));

string targetPassword = "";
if (targetEnvironment != "Local")
{
    try
    {
        string targetKeePassUrl = "AIMS SQL " + targetEnvironment;
        string targetLogin = connectionManager.GetSqlConnectionUserIdByEnvironment(targetEnvironment);
        if (keePassConnector != null)
        {
            targetPassword = keePassConnector.GetKeePassEntryPassword(targetKeePassUrl, targetLogin)!;
        }
    }
    catch (Exception)
    {
        logger.LogError("Failed to query KeePass via KeePassConnector.");
    }
    if (string.IsNullOrEmpty(targetPassword))
    {
        targetPassword = AnsiConsole.Prompt(
            new TextPrompt<string>($"Failed to query KeePass. Please provide password for [green]{targetEnvironment}[/] database connection:")
            .Secret());
    }
}

using var targetConn = new SqlConnection(connectionManager.GetSqlConnectionByEnvironment(targetEnvironment, targetPassword));

try
{
    targetConn.Open();
}
catch (Exception e)
{
    logger.LogError($"Failed to connect to {targetEnvironment} Database. The following exception occurred. Exception : {e}");
    AnsiConsole.MarkupLine($"Failed to connect to [green]{targetEnvironment}[/] Database.");
    Console.Read();
    return;
}
// Echo the selected target environment back to the terminal
AnsiConsole.MarkupLine("Copy Avatar to: [yellow]{0}[/]", targetEnvironment);

if ((originEnvironment == "Local" || targetEnvironment == "Local") && String.IsNullOrEmpty(configuration[$"ConnectionStrings:AimsConnStr"]))
    AnsiConsole.MarkupLine("[red]Can't copy from/to Local Database. ConnectionStrings__AimsConnStr entry is missing in your environment variables.[/]");
else
{
    Console.WriteLine("Please enter AvatarId to copy:");
    int avatarIdToCopy;
    if (int.TryParse(Console.ReadLine(), out avatarIdToCopy))
    {
        try
        {
            if (dbSyncronizer.RecordExistsInDB(targetConn, "avatars", "avatarid", avatarIdToCopy))
            {
                targetConn.Close();
                AnsiConsole.MarkupLine($"[red]This Avatar can't be copied, it already exist in target DB : [/]{targetConn.ConnectionString}");
            }
            else
            {
                //originConn.Open();
                if (dbSyncronizer.RecordExistsInDB(originConn, "avatars", "avatarid", avatarIdToCopy))
                {
                    AnsiConsole.MarkupLine("AvatarId to copy: [yellow]{0}[/]", avatarIdToCopy);
                    Console.WriteLine($"Please enter ProjectId for copied AvatarId: {avatarIdToCopy}.");
                    int targetProjectId;
                    if (int.TryParse(Console.ReadLine(), out targetProjectId))
                    {
                        if (dbSyncronizer.RecordExistsInDB(targetConn, "projects", "projectId", targetProjectId))
                        {
                            AnsiConsole.MarkupLine("ProjectId for Avatar to copy: [yellow]{0}[/]", targetProjectId);
                            
                            AnsiConsole.MarkupLine($"Start copying AvatarId: {avatarIdToCopy}. From : [green]{originEnvironment}[/] to : [green]{targetEnvironment}[/].");
                            logger.LogInformation($"Start copying AvatarId: {avatarIdToCopy}. From : {originEnvironment} to : {targetEnvironment}");
                            
                            var sw = Stopwatch.StartNew();

                            await syncAvatarManager.SyncAvatarTablesAsync(originConn, targetConn, avatarIdToCopy, targetProjectId);

                            originConn.Close();
                            targetConn.Close();

                            AnsiConsole.MarkupLine($"Start copying Session Storage files for AvatarId: {avatarIdToCopy}. From : [green]{originEnvironment}[/] to : [green]{targetEnvironment}[/].");
                            logger.LogInformation($"Start copying Session Storage files for AvatarId: {avatarIdToCopy}. From : {originEnvironment} to : {targetEnvironment}");

                            try
                            {
                                if(syncAvatarManager.CopySessionStorageFiles(avatarIdToCopy, originEnvironment, targetEnvironment))
                                {
                                    logger.LogInformation($"Session Storage files for AvatarId: {avatarIdToCopy} successfully copied.");
                                }
                                else 
                                {
                                    logger.LogError($"Failed to copy session storage files for AvatarId = {avatarIdToCopy}.");
                                }
                            }
                            catch (Exception e)
                            {
                                logger.LogError($"Failed to copy session storage files for AvatarId = {avatarIdToCopy}. Exception: {e}.");
                            }

                            sw.Stop();

                            logger.LogInformation($"Synchronization took {sw.Elapsed.TotalSeconds} seconds.");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]Invalid ProjectId, it doesn't exist in target DB: [/]{targetConn.ConnectionString}");
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]Invalid ProjectId. Should be an integer.[/]");
                    }
                }
                else
                {
                    originConn.Close();
                    AnsiConsole.MarkupLine($"[red]This Avatar can't be copied, it doesn't exist in origin DB: [/]{originConn.ConnectionString}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to copy Avatar. The following exception occurred. Exception : {ex}");
            logger.LogError($"Failed to copy Avatar. Please contact your administrator.");
        }
    }
    else
    {
        AnsiConsole.MarkupLine("[red]Invalid AvatarId.[/]");
    }
}

Console.Read();


