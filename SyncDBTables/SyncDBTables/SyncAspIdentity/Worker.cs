using SyncDBTables.Models;
using System.Data.SqlClient;
using System.Diagnostics;

namespace SyncDBTables
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly IConfiguration configuration;
        private readonly DbSyncronizer dbSyncronizer;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, DbSyncronizer dbSyncronizer)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.dbSyncronizer = dbSyncronizer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await SyncAsync();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task SyncAsync()
        {
            var sw = Stopwatch.StartNew();

            using var originConn = new SqlConnection(configuration["originIdentityConnectionString"]);
            originConn.Open();
            using var targetConn = new SqlConnection(configuration["targetIdentityConnectionString"]);
            targetConn.Open();

            await SyncAspIdentityTablesAsync(originConn, targetConn);

            originConn.Close();
            targetConn.Close();
            sw.Stop();

            logger.LogInformation($"Synchronization took {sw.Elapsed.TotalSeconds} seconds");
        }

        private async Task SyncAspIdentityTablesAsync(SqlConnection originConn, SqlConnection targetConn)
        {
            var aspIdentityTables = configuration.GetSection("AspIdentityTables").Get<List<Table>>();
            int tableSuccessfullySynchronized = 0;
            foreach (var table in aspIdentityTables)
            {
                try
                {
                    await this.dbSyncronizer.SyncTableAsync(originConn, targetConn, table, GetValue);
                    tableSuccessfullySynchronized++;
                    logger.LogInformation($"Table {table.TableName!} successfully synchronized.");
                }
                catch (Exception)
                {
                    logger.LogError($"Failed to synchronize Table {table.TableName!}.");
                }

            }
            logger.LogInformation($"Sync AspIdentity Tables completed. {tableSuccessfullySynchronized}/{aspIdentityTables.Count} tables were successfully synchronized.");
        }

        private string? GetValue(Table table, string columnName, Dictionary<string, object> fields)
        {
            string? value = $"{table.TableName!}.{columnName}" switch
            {
                "AspNetRoles.NormalizedName" => Convert.ToString(fields["Name"])!.ToUpper(),
                "AspNetRoles.ConcurrencyStamp" => Guid.NewGuid().ToString(),
                "AspNetUsers.NormalizedUserName" => Convert.ToString(fields["UserName"])!.ToUpper(),
                "AspNetUsers.NormalizedEmail" => Convert.ToString(fields["Email"])!.ToUpper(),
                "AspNetUsers.ConcurrencyStamp" => Guid.NewGuid().ToString(),
                "AspNetUsers.LockoutEnd" => Convert.ToString(fields["LockoutEndDateUtc"]),

                _ => null
            };
            return value;
        }
    }
}