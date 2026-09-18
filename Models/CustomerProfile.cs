using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace ST10291856CLDV7112Project1.Models
{
    public class CustomerProfile : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = string.Empty;

        [Required, Display(Name = "First Name"), StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, Display(Name = "Last Name"), StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;

        public string ImageBlobName { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}