using System.Net.Http.Headers;

namespace SyncAvatar.HttpClients
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> PostFileWithTokenAsync(this HttpClient client, string token, string requestUri, string directoryPath, string fileName)
        {
            using var content = new MultipartFormDataContent();

            var fileFullName = Path.Combine(directoryPath, fileName);
            byte[] file = File.ReadAllBytes(fileFullName);
            var byteArrayContent = new ByteArrayContent(file);
            content.Add(byteArrayContent, "file", Path.GetFileNameWithoutExtension(fileName));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsync(requestUri, content);

            return response;
        }

        public static HttpResponseMessage GetWithToken(this HttpClient client, string token, string requestUri)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = client.SendAsync(requestMessage).Result;
            return response;

        }

    }
}
