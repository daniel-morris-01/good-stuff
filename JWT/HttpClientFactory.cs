using System.Collections.Concurrent;

namespace HttpClients
{
    public class HttpClientFactory
    {
        private ConcurrentDictionary<string, HttpClient> httpClientPool = new ConcurrentDictionary<string, HttpClient>();

        public HttpClientFactory()
        {
            
        }

        public HttpClient this[string url]
        {
            get
            {
                if (!httpClientPool.ContainsKey(url))
                {
                    var httpClient = new HttpClient();
                    httpClient.BaseAddress = new Uri(url);
                    httpClientPool.AddOrUpdate(url, httpClient, (k, v) => httpClient);
                }

                return httpClientPool[url];
            }
        }
    }
}           