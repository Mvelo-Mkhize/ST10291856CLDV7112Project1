using Azure;
using Azure.Data.Tables;

namespace ST10291856CLDV7112Project1.Models
{
    public class CustomerProfile : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = string.Empty;    
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string ImageBlobName { get; set; } = string.Empty;    
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}