namespace SyncDBTables.Models
{
    public class Table
    {
        public string? TableName { get; set; }
        public string? DestinationTableName { get; set; }
        public string? Description { get; set; }
        public string? KeyColumns { get; set; }
        public string? IdColumn { get; set; }
        public bool IdentityInsert { get; set; } = false;
        public string? WhereClause { get; set; }
        public Dictionary<string, object> TargetFields { get; set; } = new();
        public bool DeleteNotFoundInSource { get; set; } = false;
    }
}
