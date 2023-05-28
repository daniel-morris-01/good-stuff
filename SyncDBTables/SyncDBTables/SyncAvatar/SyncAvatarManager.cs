using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SyncAvatar.HttpClients;
using SyncDBTables;
using SyncDBTables.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncAvatar
{
    public class SyncAvatarManager
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<SyncAvatarManager> logger;
        private readonly DbSyncronizer dbSyncronizer;
        private readonly OriginProfileActivityHttpClient originProfileActivityHttpClient;
        private readonly TargetProfileActivityHttpClient targetProfileActivityHttpClient;
        private const string ImageSetsTableName = "ImageSets";
        public SyncAvatarManager(IConfiguration configuration,
                             ILogger<SyncAvatarManager> logger,
                             DbSyncronizer dbSyncronizer,
                             OriginProfileActivityHttpClient originProfileActivityHttpClient,
                             TargetProfileActivityHttpClient targetProfileActivityHttpClient)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.dbSyncronizer = dbSyncronizer;
            this.originProfileActivityHttpClient = originProfileActivityHttpClient;
            this.targetProfileActivityHttpClient = targetProfileActivityHttpClient;
        }

        public async Task SyncAvatarTablesAsync(SqlConnection originConn, SqlConnection targetConn, int avatarIdToCopy, int targetProjectId)
        {
            var avatarsTables = configuration.GetSection("AvatarsTables").Get<List<Table>>();

            avatarsTables.Single(t => t.TableName == "Avatars").WhereClause =
                avatarsTables.Single(t => t.TableName == "Avatars").WhereClause!.Replace("{AvatarIdToCopy}", avatarIdToCopy.ToString());
            logger.LogInformation($"Start Avatars Tables synchronisation for AvatarId = {avatarIdToCopy}");
            int tableSuccessfullySynchronized = 0;
            bool ImageSetsTableSynchronized = false;
            bool TargetProfileImageIdUpdated = false; 
            foreach (var table in avatarsTables)
            {
                logger.LogInformation($"Start synchronization of table : {table.TableName!}.");
                try
                {
                    if (table.TableName == "Avatars")
                    {
                        AddTargetFieldToTable(originConn, targetConn, avatarIdToCopy, table, targetProjectId);
                    }   
                    table.WhereClause = table.WhereClause!.Replace("{AvatarIdToCopy}", avatarIdToCopy.ToString());
                    table.WhereClause = table.WhereClause!.Replace("AvatarWhereClause",
                        avatarsTables.Single(t => t.TableName == "Avatars").WhereClause!);
                    var recordsCount = await dbSyncronizer.SyncTableAsync(originConn, targetConn, table, GetValue);
                    tableSuccessfullySynchronized++;
                    logger.LogInformation($"Table {table.TableName!} successfully synchronized. {recordsCount} record/s was/were inserted/updated.");
                    ImageSetsTableSynchronized = (table.TableName == ImageSetsTableName);
                    if(ImageSetsTableSynchronized && !TargetProfileImageIdUpdated)
                    {
                        TargetProfileImageIdUpdated = UpdateTargetProfileImageId(targetConn, avatarIdToCopy);
                    }
                }
                catch (Exception)
                {
                    logger.LogError($"Failed to synchronize Table {table.TableName!}.");
                    if (table.TableName == "Avatars")
                    {
                        logger.LogError("Stoping tables synchronization. Can't continue since Avatar table failed to synchronize.");
                        return;
                    }
                }
            }
            logger.LogInformation($"Sync Avatars related Tables completed. {tableSuccessfullySynchronized}/{avatarsTables.Count} tables were successfully synchronized.");
        }

        public bool CopySessionStorageFiles(int avatarId, string originEnvironment, string targetEnvironment)
        {
            bool successfullyCopied = false;
            string originProfileActivityService = configuration[$"ExternalUrls:{originEnvironment}ProfileActivityService"]!;
            originProfileActivityHttpClient.BaseAddress = new Uri(originProfileActivityService);
            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "tmp");
            string fileName = $"{avatarId}_files.zip";
            if (originProfileActivityHttpClient.GetProfileSessionFile(avatarId, originEnvironment, directoryPath, fileName))
            {
                string targetProfileActivityService = configuration[$"ExternalUrls:{targetEnvironment}ProfileActivityService"]!;
                targetProfileActivityHttpClient.BaseAddress = new Uri(targetProfileActivityService);
                targetProfileActivityHttpClient.PushProfileSessionFiles(avatarId, targetEnvironment, directoryPath, fileName).Wait();
                successfullyCopied = true;
            }
            return successfullyCopied;
        }

        private string? GetValue(Table table, string columnName, Dictionary<string, object> fields)
        {
            string? value = $"{table.TableName!}.{columnName}" switch
            {
                "Avatars.ProxyId" => "",
                "Avatars.ProjectId" => Convert.ToString(table.TargetFields["TargetProjectId"]),
                "Avatars.CountryId" => Convert.ToString(table.TargetFields["TargetCountryId"]),
                "Avatars.StateId" => Convert.ToString(table.TargetFields["TargetStateId"]),
                "Avatars.CityId" => Convert.ToString(table.TargetFields["TargetCityId"]),

                "LoginData2.form_data" => fields["form_data"].GetType() != typeof(System.DBNull) ? GetVarBinaryString((byte[])fields["form_data"]) : "",
                "LoginData2.possible_username_pairs" => fields["possible_username_pairs"].GetType() != typeof(System.DBNull) ? GetVarBinaryString((byte[])fields["possible_username_pairs"]) : "",

                "ImageSets.Image" => GetVarBinaryString((byte[])fields["Image"]),

                "Bookmarks.Icon" => fields["Icon"].GetType() != typeof(System.DBNull) ? GetVarBinaryString((byte[])fields["Icon"]) : "",

                "SessionCookies.Value" => Convert.ToString(fields[columnName])!.Replace("'", "''"),

                _ => null
            };
            return value;
        }

        private string GetVarBinaryString(byte[] bytes)
        {
            return "CAST(0x" + Convert.ToHexString(bytes) + " AS VARBINARY(max))";
        }

        private bool UpdateTargetProfileImageId(SqlConnection targetConn, int avatarId)
        {
            string updateAvatarProfileImageIdCommand = $"update Avatars set ProfileImageId = (select setid from imagesets where avatarid = {avatarId}) where avatarid = {avatarId}";
            logger.LogInformation($"Launch update Avatar ProfileImageId Command : {updateAvatarProfileImageIdCommand}");
            return dbSyncronizer.ExecuteCommand(targetConn, updateAvatarProfileImageIdCommand);
        }

        private void AddTargetFieldToTable(SqlConnection originConn, SqlConnection targetConn, int avatarId, Table table, int targetProjectId)
        {
            table.TargetFields.Add("TargetProjectId", targetProjectId);
            var targetCountryId = GetTargetCountryId(originConn, targetConn, avatarId);
            table.TargetFields.Add("TargetCountryId", targetCountryId!);
            int? targetStateId = null;
            int? targetCityId = null;
            if (targetCountryId.HasValue)
            {
                targetStateId = GetTargetStateId(originConn, targetConn, (int)targetCountryId!, avatarId);               
                if(targetStateId.HasValue)
                {
                    targetCityId = GetTargetCityId(originConn, targetConn, (int)targetCountryId!, targetStateId, avatarId);
                }
            }
            table.TargetFields.Add("TargetStateId", targetStateId!);
            table.TargetFields.Add("TargetCityId", targetCityId!);
        }

        private int? GetTargetCountryId(SqlConnection originConn, SqlConnection targetConn, int avatarIdToCopy)
        {
            try
            {
                int? targetCountryId = null;
                var originCountryNameCmd = originConn.CreateCommand();
                originCountryNameCmd.CommandText = $"select name from countries where " +
                    $"CountryId = (select CountryId from Avatars where AvatarId = {avatarIdToCopy})";
                var originCountryName = originCountryNameCmd.ExecuteScalar();

                if (originCountryName != null)
                {
                    var targetCountryIdCmd = targetConn.CreateCommand();
                    targetCountryIdCmd.CommandText = $"select CountryId from countries where name = '{originCountryName}'";
                    targetCountryId = Convert.ToInt32(targetCountryIdCmd.ExecuteScalar());
                    if (targetCountryId == 0)
                    {
                        targetCountryId = null;
                    }
                }
                return targetCountryId;
            }
            catch (Exception)
            {
                logger.LogInformation($"Avatar's Country not found in target DB. CountryId will be set to null in target DB during Avatar (AvatarId = {avatarIdToCopy}) record copy.");
                throw;
            }
        }

        private int? GetTargetStateId(SqlConnection originConn, SqlConnection targetConn, int targetCountryId, int avatarIdToCopy)
        {
            try
            {
                int ? targetStateId = null;
                var originStateNameCmd = originConn.CreateCommand();
                originStateNameCmd.CommandText = $"select name from states where " +
                    $"CountryId = (select CountryId from Avatars where AvatarId = {avatarIdToCopy}) and " +
                    $"StateId = (select StateId from Avatars where AvatarId = {avatarIdToCopy})";
                var originStateName = originStateNameCmd.ExecuteScalar();

                if (originStateName != null)
                {
                    var targetStateIdCmd = targetConn.CreateCommand();
                    targetStateIdCmd.CommandText = $"select StateId from states where " +
                        $"CountryId = {targetCountryId} and " +
                        $"name = '{originStateName}'";
                    var execCmdResult = targetStateIdCmd.ExecuteScalar();
                    if (execCmdResult != null)
                    {
                        targetStateId = Convert.ToInt32(execCmdResult);
                    }
                    if (targetStateId == 0)
                    {
                        targetStateId = null;
                    }
                }
                return targetStateId;
            }
            catch (Exception)
            {
                logger.LogInformation($"Avatar's State not found in target DB. StateId will be set to null in target DB during Avatar (AvatarId = {avatarIdToCopy}) record copy.");
                throw;
            }
        }

        private int? GetTargetCityId(SqlConnection originConn, SqlConnection targetConn, int targetCountryId, int? targetStateId, int avatarIdToCopy)
        {
            try
            {
                int? targetCityId = null;
                var originCityNameCmd = originConn.CreateCommand();
                originCityNameCmd.CommandText = $"select name from cities where " +
                    $"CountryId = (select CountryId from Avatars where AvatarId = {avatarIdToCopy}) and ";
                if (targetStateId != null)
                {
                    originCityNameCmd.CommandText += $"StateId = (select StateId from Avatars where AvatarId = {avatarIdToCopy}) and ";
                }
                originCityNameCmd.CommandText += $"CityId = (select CityId from Avatars where AvatarId = {avatarIdToCopy})";
                var originCityName = originCityNameCmd.ExecuteScalar();

                if (originCityName != null)
                {
                    var targetCityIdCmd = targetConn.CreateCommand();
                    targetCityIdCmd.CommandText = $"select CityId from cities where " +
                        $"CountryId = {targetCountryId} and ";
                    if (targetStateId != null)
                    {
                        targetCityIdCmd.CommandText += $"StateId = {targetStateId} and ";
                    }
                    targetCityIdCmd.CommandText += $"name = '{originCityName}'";
                    targetCityId = Convert.ToInt32(targetCityIdCmd.ExecuteScalar());
                }
                return targetCityId;
            }
            catch (Exception)
            {
                logger.LogInformation($"Avatar's City not found in target DB. CityId will be set to null in target DB during Avatar (AvatarId = {avatarIdToCopy}) record copy.");
                throw;
            }
        }

    }
}
