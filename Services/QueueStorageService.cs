using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace ST10291856CLDV7112Project1.Services
{
    public class QueueStorageService
    {
        private readonly QueueClient _queueClient;

        public QueueStorageService(IConfiguration configuration)
        {
            string connStr = configuration["AzureStorage:ConnectionString"]!;
            _queueClient = new QueueClient(connStr, StorageAccountService.QueueName);
        }

        public async Task SendMessageAsync(string messageText)
        {
            await _queueClient.SendMessageAsync(messageText);
        }

        public async Task<List<string>> PeekMessagesAsync(int maxMessages = 32)
        {
            PeekedMessage[] peeked = await _queueClient.PeekMessagesAsync(maxMessages);
            var messages = new List<string>();
            foreach (var msg in peeked)
                messages.Add(msg.MessageText);
            return messages;
        }

        public async Task<string?> DequeueMessageAsync()
        {
            QueueMessage[] messages = await _queueClient.ReceiveMessagesAsync(1);
            if (messages.Length == 0) return null;
            await _queueClient.DeleteMessageAsync(messages[0].MessageId, messages[0].PopReceipt);
            return messages[0].MessageText;
        }
    }
}