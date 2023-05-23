using System.Net.Http.Headers;

namespace AimsProfileService.HttpClients
{
    public class TokenFetcher
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public TokenFetcher(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public string GetToken()
        {
            string token = "";

            if (httpContextAccessor.HttpContext != null)
            {
                var bearer = httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToList().Find(x => x != null && x.StartsWith("Bearer"));
                if (!string.IsNullOrEmpty(bearer))
                {
                    token = bearer.Replace("Bearer ", "");
                }
            }
            return token;
        }
    }
}