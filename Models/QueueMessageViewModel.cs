namespace ST10291856CLDV7112Project1.Models
{
    public class QueueMessageViewModel
    {
        public string OrderId { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ImageBlobName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string RawMessage { get; set; } = string.Empty;
    }
}