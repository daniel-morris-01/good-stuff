using Avatars.TaskAutomation.Lib;
using Avatars.TaskAutomation.Lib.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Avatars.TaskAutomation.Manager
{
    public class NodeHttpClientFactory
    {
        private static int instanceCount = 0;
        private ConcurrentDictionary<string, HttpClient> httpClientPool = new ConcurrentDictionary<string, HttpClient>();

        public NodeHttpClientFactory()
        {
            Debug.Assert(++instanceCount == 1);
        }

        public HttpClient GetClient(string url)
        {
            if(!httpClientPool.ContainsKey(url))
            {
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(url);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                httpClientPool.AddOrUpdate(url, httpClient, (k, v) => httpClient);
            }
            
            return httpClientPool[url];
        }
    }

}
