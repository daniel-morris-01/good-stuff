using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace AimsProfileService.HttpClients
{
    public class HttpClientWithToken : HttpClient
    {
        private readonly TokenFetcher tokenFetcher;

        public HttpClientWithToken(TokenFetcher tokenFetcher)
        {
            this.tokenFetcher = tokenFetcher;
        }
        public async Task<HttpResponseMessage> GetWithTokenAsync(string requestUri)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenFetcher.GetToken());

            var response = await SendAsync(requestMessage);
            return response;
        }

        public async Task<HttpResponseMessage> PostAsJsonWithTokenAsync<T>(string requestUri, T value)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);

            requestMessage.Content = new ObjectContent<T>(value, new JsonMediaTypeFormatter());
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenFetcher.GetToken());

            var response = await SendAsync(requestMessage);
            return response;
        }
    }
}
