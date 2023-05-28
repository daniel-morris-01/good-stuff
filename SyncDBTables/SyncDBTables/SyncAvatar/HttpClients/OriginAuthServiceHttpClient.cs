using Microsoft.Extensions.Configuration;

namespace SyncAvatar.HttpClients
{
    public class OriginAuthServiceHttpClient : HttpClient
    {
        public OriginAuthServiceHttpClient(IConfiguration configuration, string environment)
        {
            BaseAddress = new Uri(configuration[$"ExternalUrls:{environment}AuthService"]);
        }
    }
}
