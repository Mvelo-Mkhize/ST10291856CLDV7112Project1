using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;

namespace ST10291856CLDV7112Project1.Services
{
    public class StorageAccountService
    {
        private readonly string _connectionString;

        public const string TableCustomer = "CustomerProfiles";
        public const string TableProduct = "Products";
        public const string BlobContainerName = "product-images";
        public const string QueueName = "order-processing";
        public const string FileShareName = "abc-retail-logs";
        public const string LogDirectoryName = "logs";

        public StorageAccountService(IConfiguration configuration)
        {
            _connectionString = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "AzureStorage:ConnectionString is not configured. Set it in user secrets or App Service settings.");
        }

        public async Task InitializeAsync()
        {
            var tableServiceClient = new TableServiceClient(_connectionString);
            await tableServiceClient.CreateTableIfNotExistsAsync(TableCustomer);
            await tableServiceClient.CreateTableIfNotExistsAsync(TableProduct);

            var blobServiceClient = new BlobServiceClient(_connectionString);
            var container = blobServiceClient.GetBlobContainerClient(BlobContainerName);
            await container.CreateIfNotExistsAsync();

            var queueServiceClient = new QueueServiceClient(_connectionString);
            var queue = queueServiceClient.GetQueueClient(QueueName);
            await queue.CreateIfNotExistsAsync();

            var shareServiceClient = new ShareServiceClient(_connectionString);
            var share = shareServiceClient.GetShareClient(FileShareName);
            await share.CreateIfNotExistsAsync();

            var logDir = share.GetRootDirectoryClient().GetSubdirectoryClient(LogDirectoryName);
            await logDir.CreateIfNotExistsAsync();
        }
    }
}