using SyncAvatar.HttpClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Eventing.Reader;

namespace SyncAvatar.Security
{
    public class JwtTokenStorage
    {
        private readonly SemaphoreSlim AccessTokenSemaphore=new SemaphoreSlim(1,1);
        private readonly IConfiguration configuration;
        private readonly ILogger<JwtTokenStorage> logger;
        private readonly HttpClient? authHttpClient;
        private TokenResult? accessToken=null;
        private const int ThresholdMinutes= 5;
        private readonly KeePassConnector.KeePassConnector? keePassConnector = null;

        public JwtTokenStorage(IConfiguration configuration, ILoggerFactory loggerFactory, eMigrationSide migrationSide, string environment)
        {
            this.configuration = configuration;
            this.logger = loggerFactory.CreateLogger<JwtTokenStorage>();
            switch (migrationSide)
            {
                case eMigrationSide.Origin:
                    authHttpClient = new OriginAuthServiceHttpClient(configuration, environment);
                    break;
                case eMigrationSide.Target:
                    authHttpClient = new TargetAuthServiceHttpClient(configuration, environment);
                    break;
            }
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
        }

        public string GetToken(string environment)
        {
            string? token = null;
            try
            {
                AccessTokenSemaphore.Wait();

                if (accessToken != null && DateTime.Now.AddMinutes(ThresholdMinutes) < accessToken.expirationTimeUTC)
                {
                    token = accessToken.token;
                }
                else
                {
                    string keePassUrl = "SyncAvatar " + environment;
                    var kpEntry = keePassConnector!.GetKeePassEntries(keePassUrl)![0];

                    if (kpEntry != null)
                    {
                        var tokenResponse = authHttpClient.PostAsJsonAsync("api/Auth/LogonUser"
                                                    , new { userName = kpEntry.Login!, password = kpEntry.Password! }).Result;
                        tokenResponse.EnsureSuccessStatusCode();
                        accessToken = tokenResponse.Content.ReadAsAsync<TokenResult>().Result;
                        token = accessToken.token;
                    }
                    else
                    {
                        throw new Exception($"Failed to access keePass Url/Title;{keePassUrl}.");
                    }
                    
                    if (accessToken == null)
                    {
                        throw new Exception("Failed to deserialize access token.");
                    }
                }
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex,"Error fetching token.");
            }
            finally
            {
                AccessTokenSemaphore.Release(1);
            }
            return token!;
        }
    }

    
}
