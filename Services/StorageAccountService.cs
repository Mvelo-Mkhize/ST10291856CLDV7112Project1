using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using Azure.Data.Tables;

namespace ST10291856CLDV7112Project1.Services
{
    public class StorageAccountService
    {
        private readonly string _connectionString;
        public const string TableCustomer = "CustomerProfiles";
        public const string TableProduct = "Products";
        public const string BlobContainerName = "product-images";
        public const string QueueName = "order-processing";
        public const string FileShareName = "logs";

        public StorageAccountService(IConfiguration configuration)
        {
            _connectionString = configuration["AzureStorage:ConnectionString"]!;
        }

        public async Task InitializeAsync()
        {
            var tableServiceClient = new TableServiceClient(_connectionString);
            await tableServiceClient.CreateTableIfNotExistsAsync(TableCustomer);
            await tableServiceClient.CreateTableIfNotExistsAsync(TableProduct);

            var blobServiceClient = new BlobServiceClient(_connectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(BlobContainerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                await blobContainerClient.CreateAsync();
            }

            var queueServiceClient = new QueueServiceClient(_connectionString);
            var queueClient = queueServiceClient.GetQueueClient(QueueName);
            if (!await queueClient.ExistsAsync())
            {
                await queueClient.CreateAsync();
            }

            var shareServiceClient = new ShareServiceClient(_connectionString);
            var shareClient = shareServiceClient.GetShareClient(FileShareName);
            if (!await shareClient.ExistsAsync())
            {
                await shareClient.CreateAsync();
            }
        }
    }
}