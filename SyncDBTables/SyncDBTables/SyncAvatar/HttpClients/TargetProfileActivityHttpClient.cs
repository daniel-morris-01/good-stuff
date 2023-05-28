using SyncAvatar.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SyncAvatar.HttpClients
{
    public class TargetProfileActivityHttpClient : HttpClient
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<TargetProfileActivityHttpClient> logger;
        private readonly ILoggerFactory loggerFactory;
        private JwtTokenStorage? jwtTokenStorage;

        public TargetProfileActivityHttpClient(IConfiguration configuration, 
                                        ILogger<TargetProfileActivityHttpClient> logger,
                                        ILoggerFactory loggerFactory)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.loggerFactory = loggerFactory;
        }

        public async Task PushProfileSessionFiles(int profileId, string targetEnvironment, string directoryPath, string fileName)
        {
            this.jwtTokenStorage = new(configuration, loggerFactory, eMigrationSide.Target, targetEnvironment);
            var token = jwtTokenStorage.GetToken(targetEnvironment);
            if (token != null)
            {
                var res = await this.PostFileWithTokenAsync(token, $"PushProfileSessionFiles/{profileId}", directoryPath, fileName);
                res.EnsureSuccessStatusCode();
            }
            else
            {
                logger.LogError("PushProfileSessionFiles - Failed to get Jwt Token from Service.");
            }
        }

    }
}
