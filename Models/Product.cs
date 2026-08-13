using Azure;
using Azure.Data.Tables;

namespace ST10291856CLDV7112Project1.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = string.Empty;    
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public int StockQuantity { get; set; }
        public string ImageBlobName { get; set; } = string.Empty;    
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}