using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ST10291856CLDV7112Project1.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration configuration)
        {
            string connStr = configuration["AzureStorage:ConnectionString"]!;
            _containerClient = new BlobContainerClient(connStr, StorageAccountService.BlobContainerName);
        }

        public async Task UploadBlobAsync(string blobName, Stream content, string contentType)
        {
            BlobClient blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });
        }

        public async Task<Stream> DownloadBlobAsync(string blobName)
        {
            BlobClient blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }

        public async Task<List<string>> ListBlobsAsync()
        {
            var names = new List<string>();
            await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync())
                names.Add(blobItem.Name);
            return names;
        }

        public async Task DeleteBlobAsync(string blobName)
        {
            BlobClient blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        public string GetBlobUrl(string blobName)
        {
            if (string.IsNullOrEmpty(blobName))
                return string.Empty;
            return _containerClient.GetBlobClient(blobName).Uri.AbsoluteUri;
        }
    }
}