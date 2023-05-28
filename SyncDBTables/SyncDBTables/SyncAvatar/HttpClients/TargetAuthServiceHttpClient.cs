using Microsoft.Extensions.Configuration;

namespace SyncAvatar.HttpClients
{
    public class TargetAuthServiceHttpClient : HttpClient
    {
        public TargetAuthServiceHttpClient(IConfiguration configuration, string environment)
        {
            BaseAddress = new Uri(configuration[$"ExternalUrls:{environment}AuthService"]);
        }
    }
}
