using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace ST10291856CLDV7112Project1.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly StorageSharedKeyCredential _credential;
        private readonly int _sasExpiryMinutes;

        private static readonly string[] AllowedImageTypes =
            { "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp" };
        private const long MaxImageBytes = 5 * 1024 * 1024;

        public BlobStorageService(IConfiguration configuration)
        {
            string connStr = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");

            _sasExpiryMinutes = configuration.GetValue<int?>("AzureStorage:SasExpiryMinutes") ?? 60;

            var blobServiceClient = new BlobServiceClient(connStr);
            _containerClient = blobServiceClient.GetBlobContainerClient(StorageAccountService.BlobContainerName);

            var parts = connStr.Split(';')
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim(), p => p[1], StringComparer.OrdinalIgnoreCase);

            _credential = new StorageSharedKeyCredential(parts["AccountName"], parts["AccountKey"]);
        }

        public bool IsValidImage(IFormFile file, out string? error)
        {
            error = null;
            if (file.Length == 0) { error = "File is empty."; return false; }
            if (file.Length > MaxImageBytes) { error = "Image exceeds the 5 MB size limit."; return false; }
            if (!AllowedImageTypes.Contains(file.ContentType.ToLowerInvariant()))
            { error = "Unsupported image type. Use JPG, PNG, GIF, WEBP or BMP."; return false; }
            return true;
        }

        public async Task UploadBlobAsync(string blobName, Stream content, string contentType)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            });
        }

        public async Task<Stream> DownloadBlobAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
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
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        public string GetBlobUrl(string blobName)
        {
            if (string.IsNullOrEmpty(blobName)) return string.Empty;

            var blobClient = _containerClient.GetBlobClient(blobName);
            if (!blobClient.CanGenerateSasUri)
                return blobClient.Uri.AbsoluteUri;

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_sasExpiryMinutes)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }
    }
}