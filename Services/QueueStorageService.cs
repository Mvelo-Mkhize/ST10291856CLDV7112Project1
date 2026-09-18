using Azure.Storage.Queues;

namespace ST10291856CLDV7112Project1.Services
{
    public class QueueStorageService
    {
        private readonly QueueClient _queueClient;

        public QueueStorageService(IConfiguration configuration)
        {
            string connStr = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");
            _queueClient = new QueueClient(connStr, StorageAccountService.QueueName);
        }

        public async Task SendMessageAsync(string messageText)
            => await _queueClient.SendMessageAsync(messageText);

        public async Task<List<string>> PeekMessagesAsync(int maxMessages = 32)
        {
            var peeked = await _queueClient.PeekMessagesAsync(maxMessages);
            return peeked.Value.Select(m => m.MessageText).ToList();
        }

        public async Task<int> GetApproximateCountAsync()
        {
            var props = await _queueClient.GetPropertiesAsync();
            return props.Value.ApproximateMessagesCount;
        }

        public async Task<(string MessageId, string PopReceipt, string Body)?> DequeueMessageAsync(
            TimeSpan? visibilityTimeout = null)
        {
            var messages = await _queueClient.ReceiveMessagesAsync(
                maxMessages: 1,
                visibilityTimeout: visibilityTimeout ?? TimeSpan.FromMinutes(2));

            if (messages.Value.Length == 0) return null;

            var m = messages.Value[0];
            return (m.MessageId, m.PopReceipt, m.MessageText);
        }

        public async Task DeleteMessageAsync(string messageId, string popReceipt)
            => await _queueClient.DeleteMessageAsync(messageId, popReceipt);

        public async Task ClearAsync()
            => await _queueClient.ClearMessagesAsync();
    }
}