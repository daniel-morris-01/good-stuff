using Microsoft.Extensions.Configuration;
using SyncDBTables.Models;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace SyncDBTables
{
    public class DbSyncronizer
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<DbSyncronizer> logger;

        Func<Table, string, Dictionary<string, object>, string?>? GetValueFunc;

        public DbSyncronizer(IConfiguration configuration,
                             ILogger<DbSyncronizer> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task<int> SyncTableAsync(SqlConnection originConn, SqlConnection targetConn, Table table,
            Func<Table, string, Dictionary<string, object>, string?> GetValueFunc)
        {
            int affectedRecordsCount = 0;
            this.GetValueFunc = GetValueFunc;
            using var oldCmd = originConn.CreateCommand();
            oldCmd.CommandText = "select * from " + table.TableName!;
            if (!string.IsNullOrEmpty(table.WhereClause))
            {
                oldCmd.CommandText += " where " + table.WhereClause!;
            }

            using var reader = await oldCmd.ExecuteReaderAsync();

            var destTableName = string.IsNullOrEmpty(table.DestinationTableName) ? table.TableName : table.DestinationTableName;
            var destTableColumnInfo = await GetColumnInfoForAsync(targetConn, destTableName!).ToListAsync();

            while (reader.Read())
            {
                var fields = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    fields.Add(reader.GetName(i), reader[i]);
                }
                await AddRowAsync(targetConn, destTableColumnInfo, fields, table);
                affectedRecordsCount++;
            }
            reader.Close();

            if (table.DeleteNotFoundInSource)
            {
                using var newCmd = targetConn.CreateCommand();
                newCmd.CommandText = "select * from " + destTableName;
                if (!string.IsNullOrEmpty(table.WhereClause))
                {
                    newCmd.CommandText += " where " + table.WhereClause!;
                }
                var deleteStatements = new List<string>();
                using var targetReader = await newCmd.ExecuteReaderAsync();
                while (targetReader.Read())
                {
                    var fields = new Dictionary<string, object>();
                    for (int i = 0; i < targetReader.FieldCount; i++)
                    {
                        fields.Add(targetReader.GetName(i), targetReader[i]);
                    }
                    await AddDeleteStatementsIfNotFoundAsync(originConn,fields, table, deleteStatements);
                }
                targetReader.Close();
                foreach (var deleteStatement in deleteStatements)
                {
                    await RunSqlOntargetConnAsync(targetConn, deleteStatement);
                }

            }

            return affectedRecordsCount;
        }

        private async IAsyncEnumerable<ColumnInfo> GetColumnInfoForAsync(SqlConnection targetConn, string tableName)
        {
            using var cmd = targetConn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = N'{tableName}'";
            using var reader = await cmd.ExecuteReaderAsync();
            while (reader.Read())
            {
                yield return new ColumnInfo { Name = Convert.ToString(reader["COLUMN_NAME"]), IsNumber = IsNumber(Convert.ToString(reader["DATA_TYPE"])) };
            }
        }

        private bool IsNumber(string? dataType)
        {
            return dataType != null && !dataType.Contains("char") && dataType != "bit" && dataType != "datetime"; 
        }

        private async Task AddRowAsync(SqlConnection targetConn, List<ColumnInfo> columnInfo, Dictionary<string, object> fields, Table table)
        {
            var where = string.Join(" and ", table.KeyColumns!.Split('+').Select(c => $"{c}='{fields[c]}'").ToArray());

            SqlCommand existsCmd = targetConn.CreateCommand();

            var tableName = string.IsNullOrEmpty(table.DestinationTableName) ? table.TableName : table.DestinationTableName;

            existsCmd.CommandText = $"select count(*) from {tableName!} where {where}";
            
            var rowExists = Convert.ToInt32(existsCmd.ExecuteScalar()) > 0;
            if (rowExists)
            {
                await UpdateRowAsync(targetConn, where, columnInfo, fields, table);
            }
            else
            {
                await InsertRowAsync(targetConn, columnInfo, fields, table);
            }
        }

        private async Task UpdateRowAsync(SqlConnection targetConn, string where, List<ColumnInfo> columnsInfo, Dictionary<string, object> fields, Table table)
        {
            var tableName = string.IsNullOrEmpty(table.DestinationTableName) ? table.TableName : table.DestinationTableName;

            string sql = $"update {tableName!} set ";

            foreach (var columnInfo in columnsInfo.Where(c => c.Name != table.IdColumn))
            {
                sql += columnInfo.Name + "=" + GetValueForField(columnInfo, fields, table) + ",";
            }
            sql = sql.TrimEnd(',') + " where " + where;
            await RunSqlOntargetConnAsync(targetConn, sql);
        }

        private async Task InsertRowAsync(SqlConnection targetConn, List<ColumnInfo> columnsInfo, Dictionary<string, object> fields, Table table)
        {
            string sql = "";
            try
            {
                var tableName = string.IsNullOrEmpty(table.DestinationTableName) ? table.TableName : table.DestinationTableName;

                IEnumerable<ColumnInfo> columnsInfoFiltered = columnsInfo;
                if (table.IdentityInsert)
                {
                    sql = $"SET IDENTITY_INSERT {tableName!} ON; ";
                }
                else
                {
                    columnsInfoFiltered = columnsInfo.Where(c => c.Name != table.IdColumn!);
                }
                var columnsNames = string.Join(',', columnsInfoFiltered.Select(c => c.Name).ToArray());

                sql += $"insert into {tableName!} ({columnsNames}) values (";

                foreach (var columnInfo in columnsInfoFiltered)
                {
                    sql += GetValueForField(columnInfo, fields, table) + ",";
                }
            }
            catch (Exception e)
            {
                logger.LogError($"Failed to build Insert command. sql:{sql}. Make sure that the target table schema match the origin table schema. Exception: {e.Message}", e);
                throw;
            }
            sql = sql.TrimEnd(',') + ")";

            await RunSqlOntargetConnAsync(targetConn, sql);
        }

        
        private string? GetValueForField(ColumnInfo columnInfo, Dictionary<string, object> fields, Table table)
        {
            string apostrophe = columnInfo.IsNumber ? "" : "'";

            string? value = GetValueFunc!(table, columnInfo.Name!, fields);
            if (value == null)
            {
                value = Convert.ToString(fields[columnInfo.Name!]);
            }
                
            if (string.IsNullOrWhiteSpace(value))
            {
                return "null";
            }
            return apostrophe + value + apostrophe;
        }

        private async Task<int> RunSqlOntargetConnAsync(SqlConnection targetConn, string sql)
        {
            using var cmd = targetConn.CreateCommand();
            cmd.CommandText = sql;
            try
            {
                return await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                logger.LogError($"Failed to execute command:{cmd.CommandText}. Exception: {e.Message}", e);
                throw;
            }
            
        }

        public bool RecordExistsInDB(SqlConnection conn, string tableName, string idColumn, int recordId)
        {
            try
            {
                var existsCmd = conn.CreateCommand();
                existsCmd.CommandText = $"select * from {tableName} where {idColumn} = '{recordId}'";
                return Convert.ToInt32(existsCmd.ExecuteScalar()) > 0;
            }
            catch (Exception)
            {
                logger.LogInformation($"Failed to check if record exists in DB. tableName:{tableName}, recordId:{recordId}");
                throw;
            }
        }

        public bool ExecuteCommand(SqlConnection conn, string commandText)
        {
            try
            {
                var existsCmd = conn.CreateCommand();
                existsCmd.CommandText = commandText;
                return Convert.ToInt32(existsCmd.ExecuteScalar()) == 0;
            }
            catch (Exception)
            {
                logger.LogInformation($"Failed to execute command: {commandText}.");
                throw;
            }
        }

        private async Task AddDeleteStatementsIfNotFoundAsync(SqlConnection origConn
            ,  Dictionary<string, object> fields
            , Table table
            , List<string> deleteStatements)
        {
            var where = string.Join(" and ", table.KeyColumns!.Split('+').Select(c => $"{c}='{fields[c]}'").ToArray());

            SqlCommand existsCmd = origConn.CreateCommand();

            var sourceTableName = table.TableName;
            var destinationTableName = string.IsNullOrEmpty(table.DestinationTableName) ? table.TableName : table.DestinationTableName;

            existsCmd.CommandText = $"select count(*) from {sourceTableName!} where {where}";

            var rowExists = Convert.ToInt32(await existsCmd.ExecuteScalarAsync()) > 0;
            if (!rowExists)
            {
                AddDeleteStatement(deleteStatements, where, fields, destinationTableName!);
            }
            
            
        }

        private void AddDeleteStatement(List<string> deleteStatements, string where
            , Dictionary<string, object> fields, string tableName)
        {
            string sql = $"delete from {tableName} ";
                        
            sql = sql.TrimEnd(',') + " where " + where;
            deleteStatements.Add(sql);
        }
    }
}
