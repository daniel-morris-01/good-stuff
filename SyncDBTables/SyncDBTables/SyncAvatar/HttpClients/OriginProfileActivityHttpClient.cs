using SyncAvatar.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.IO;
using System;

namespace SyncAvatar.HttpClients
{
    public class OriginProfileActivityHttpClient : HttpClient
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<OriginProfileActivityHttpClient> logger;
        private readonly ILoggerFactory loggerFactory;
        private JwtTokenStorage? jwtTokenStorage;

        public OriginProfileActivityHttpClient(IConfiguration configuration, 
                                        ILogger<OriginProfileActivityHttpClient> logger,
                                        ILoggerFactory loggerFactory)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.loggerFactory = loggerFactory;
        }

        public bool GetProfileSessionFile(int profileId, string originEnvironment, string directoryPath, string fileName)
        {
            bool successfullyDownloaded = false;
            this.jwtTokenStorage = new(configuration, loggerFactory, eMigrationSide.Origin, originEnvironment);
            var token = jwtTokenStorage.GetToken(originEnvironment);
            if (token != null)
            {
                var res = this.GetWithToken(token, $"GetProfileSessionFiles?profileId={profileId}");
                res.EnsureSuccessStatusCode();
                var stream =  res.Content.ReadAsStreamAsync().Result;
                if (stream != null)
                {
                    WriteInputFile(stream!, directoryPath, fileName);
                    successfullyDownloaded = true;
                }
            }
            else
            {
                logger.LogError("ImageSetsNotInInventory - Failed to get Jwt Token from Service.");
            }
            return successfullyDownloaded;
        }

        private string WriteInputFile(Stream stream, string directoryPath, string fileName)
        {
            string fileFullName = "";
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            using (var s = stream)
            {
                var fileInfo = new FileInfo(Path.Combine(directoryPath, fileName));
                if (File.Exists(fileInfo.FullName))
                {
                    File.Delete(fileInfo.FullName); //Delete previous download
                }
                using (var fileStream = fileInfo.OpenWrite())
                {
                    logger.LogInformation($"WriteInputFile to {fileInfo.FullName}");
                    stream.CopyToAsync(fileStream).Wait();
                }
                fileFullName = fileInfo.FullName;
            }
            return fileFullName;
        }

    }
}
