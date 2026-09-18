using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace ST10291856CLDV7112Project1.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";

        [Required, Display(Name = "SKU"), StringLength(50)]
        public string RowKey { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required, Range(0.01, 1_000_000)]
        public double Price { get; set; }

        [Required, Range(0, 1_000_000), Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        public string ImageBlobName { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}