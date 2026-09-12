using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
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
        private readonly CloudFileShare _share;
        private readonly ILogger _logger;

        public DocumentService(string connectionString, string shareName, ILogger logger)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var fileClient = storageAccount.CreateCloudFileClient();
            _share = fileClient.GetShareReference(shareName);
            _share.CreateIfNotExists();
            _logger = logger;
        }

        public async Task UploadDocumentAsync(string fileName, Stream fileStream)
        {
            var rootDir = _share.GetRootDirectoryReference();
            var file = rootDir.GetFileReference(fileName);
            await file.UploadFromStreamAsync(fileStream);
        }

        public async Task<List<DocumentInfo>> ListDocumentsAsync()
        {
            var documents = new List<DocumentInfo>();
            var rootDir = _share.GetRootDirectoryReference();

            FileContinuationToken token = null;
            do
            {
                var segment = await rootDir.ListFilesAndDirectoriesSegmentedAsync(token);
                foreach (var item in segment.Results)
                {
                    if (item is CloudFile file)
                    {
                        await file.FetchAttributesAsync();
                        documents.Add(new DocumentInfo
                        {
                            Name = file.Name,
                            Size = file.Properties.Length,
                            LastModified = file.Properties.LastModified?.DateTime ?? DateTime.Now
                        });
                    }
                }
                token = segment.ContinuationToken;
            } while (token != null);

            return documents;
        }

        public async Task<Stream> DownloadDocumentAsync(string fileName)
        {
            var rootDir = _share.GetRootDirectoryReference();
            var file = rootDir.GetFileReference(fileName);

            if (!await file.ExistsAsync())
                return null;

            var memoryStream = new MemoryStream();
            await file.DownloadToStreamAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}