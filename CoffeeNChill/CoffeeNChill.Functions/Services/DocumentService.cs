using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Services
{
    public class DocumentInfo
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class DocumentService
    {
        private readonly ShareClient _shareClient;
        private readonly ILogger _logger;

        public DocumentService(string connectionString, string shareName, ILogger logger)
        {
            var shareServiceClient = new ShareServiceClient(connectionString);
            _shareClient = shareServiceClient.GetShareClient(shareName);
            _shareClient.CreateIfNotExists();
            _logger = logger;
        }

        public async Task UploadDocumentAsync(string fileName, Stream fileStream)
        {
            try
            {
                var directory = _shareClient.GetRootDirectoryClient();
                var file = directory.GetFileClient(fileName);
                await file.CreateAsync(fileStream.Length);
                fileStream.Position = 0;
                await file.UploadAsync(fileStream);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading document: {ex.Message}");
                throw;
            }
        }

        public async Task<List<DocumentInfo>> ListDocumentsAsync()
        {
            var documents = new List<DocumentInfo>();
            var directory = _shareClient.GetRootDirectoryClient();

            await foreach (var item in directory.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                {
                    var fileClient = directory.GetFileClient(item.Name);
                    var properties = await fileClient.GetPropertiesAsync();

                    // Get the last modified time - it's already a DateTimeOffset, not nullable
                    var lastModified = properties.Value.LastModified;

                    documents.Add(new DocumentInfo
                    {
                        Name = item.Name,
                        Size = properties.Value.ContentLength,
                        LastModified = lastModified.DateTime
                    });
                }
            }

            return documents;
        }

        public async Task<Stream> DownloadDocumentAsync(string fileName)
        {
            try
            {
                var directory = _shareClient.GetRootDirectoryClient();
                var file = directory.GetFileClient(fileName);

                if (!await file.ExistsAsync())
                {
                    return null;
                }

                var downloadInfo = await file.DownloadAsync();
                var memoryStream = new MemoryStream();
                await downloadInfo.Value.Content.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading document: {ex.Message}");
                throw;
            }
        }
    }
}