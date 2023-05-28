using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncAvatar
{
    public  class ConnectionManager
    {

        private readonly IConfiguration configuration;
        private readonly ILogger<ConnectionManager> logger;

        public ConnectionManager(IConfiguration configuration,
                             ILogger<ConnectionManager> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public string GetSqlConnectionByEnvironment(string environment, string password)
        {
            string connectionString;
            if (environment == "Local")
            {
                connectionString = configuration[$"ConnectionStrings:AimsConnStr"];
            }
            else
            {
                connectionString = configuration[$"SynDBConnectionString:{environment}"] + password;
            }
            return connectionString;
        }

        public string GetSqlConnectionUserIdByEnvironment(string environment)
        {
            string userId;
            if (environment == "Local")
            {
                userId = "";
            }
            else
            {
                string connectionString = configuration[$"SynDBConnectionString:{environment}"];
                int userIdIndex = connectionString.IndexOf("User ID=")+8;
                int lastSumsemicolonIndex = connectionString.LastIndexOf(";");
                userId = connectionString.Substring(userIdIndex, lastSumsemicolonIndex - userIdIndex);
            }
            return userId;
        }
    }
}
