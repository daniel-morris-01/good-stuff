using KeePassConnector.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace KeePassConnector
{
    public class KeePassConnector
    {
        private readonly ILogger<KeePassConnector> logger;
        private readonly string AESkeyName = "chromeConnector";
        private readonly byte[] AESkeyValue;
        private const string keePassHttpUrl = "http://localhost:19455";

        /// <summary>
        /// KeePassConector is using KeePassHttp plugin
        /// 1. Under KeePassHttp Settings entry
        /// 2. Under Advanced TAB
        /// 3. There is a list of string fields. Under this list there is a string field for the KeePassHttp plugin
        /// 4. The key name is usually - AES Key: chromipasskey or AES Key: chromeConnector
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public KeePassConnector(ILogger<KeePassConnector> logger, string AESkeyName, string AESkeyValue)
        {
            this.logger = logger;
            this.AESkeyName = AESkeyName;
            if (!string.IsNullOrWhiteSpace(AESkeyName) || !string.IsNullOrWhiteSpace(AESkeyValue))
            {
                this.AESkeyValue = Convert.FromBase64String(AESkeyValue);
            }
            else
            {
                throw new Exception("Unable to initialize KeePassConnector, one of the required parametres 'AESkeyName', 'AESkeyValue' is null.");
            }
        }

        public KeyPassEntry[]? GetKeePassEntries(string url)
        {
            var iv = RandomNumberGenerator.GetBytes(16);
            var ivBase64 = Convert.ToBase64String(iv);

            var urlEncrypted = EncryptionManager.Encrypt(url, AESkeyValue, iv);

            HttpClient client = new HttpClient();

            var request = new KeyPassHttpRequest
            {
                RequestType = "get-logins",
                SortSelection = "true",
                TriggerUnlock = "false",
                Id = AESkeyName,
                Nonce = ivBase64,
                Verifier = EncryptionManager.Encrypt(ivBase64, AESkeyValue, iv),
                Url = urlEncrypted
            };
            var httpRes = client
                .PostAsJsonAsync<KeyPassHttpRequest>(keePassHttpUrl, request)
                .Result;
            httpRes.EnsureSuccessStatusCode();

            var response = httpRes.Content.ReadFromJsonAsync<KeyPassHttpResponse>()
                .Result;

            if (response != null && response.Entries != null && response.Nonce != null)
            {
                var resIv = Convert.FromBase64String(response.Nonce);
                foreach (var entry in response.Entries)
                {
                    if (entry.Name != null)
                    {
                        entry.Name = EncryptionManager.Decrypt(entry.Name, AESkeyValue, resIv);
                    }
                    if (entry.Login != null)
                    {
                        entry.Login = EncryptionManager.Decrypt(entry.Login, AESkeyValue, resIv);
                    }
                    if (entry.Password != null)
                    {
                        entry.Password = EncryptionManager.Decrypt(entry.Password, AESkeyValue, resIv);
                    }
                }
            }
            else
            {
                logger.LogError($"No entry found with url: {url}. request: {JsonSerializer.Serialize(request)}. response: {JsonSerializer.Serialize(response)}");
            }

            return response!.Entries;
        }

        public KeyPassEntry? GetKeePassEntry(string url, string login)
        {
            KeyPassEntry keyPassEntry = null;
            var entries = GetKeePassEntries(url);
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry.Login == login)
                    {
                        keyPassEntry = entry;
                    }
                }
            }
            return keyPassEntry;
        }

        public string? GetKeePassEntryPassword(string url, string login)
        {
            string? keyPassEntryPassword = null;
            var entries = GetKeePassEntries(url);
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry.Login == login)
                    {
                        keyPassEntryPassword = entry.Password;
                    }
                }
            }
            return keyPassEntryPassword;
        }

    }
}