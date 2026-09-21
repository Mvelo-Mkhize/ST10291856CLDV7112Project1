using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System.Text;

namespace ST10291856CLDV7112Project1.Services
{
    public class FileShareService
    {
        private readonly ShareClient _shareClient;
        private static readonly SemaphoreSlim _lock = new(1, 1);

        public FileShareService(IConfiguration configuration)
        {
            string connStr = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");
            _shareClient = new ShareClient(connStr, StorageAccountService.FileShareName);
        }

        private ShareDirectoryClient LogDirectory =>
            _shareClient.GetRootDirectoryClient()
                        .GetSubdirectoryClient(StorageAccountService.LogDirectoryName);

        public async Task WriteLogAsync(string? fileName, string logEntry)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = $"log-{DateTime.UtcNow:yyyy-MM-dd}.log";
            if (!fileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                fileName += ".log";

            await _lock.WaitAsync();
            try
            {
                var dir = LogDirectory;
                await dir.CreateIfNotExistsAsync();

                var fileClient = dir.GetFileClient(fileName);

                string existing = string.Empty;
                if (await fileClient.ExistsAsync())
                {
                    try
                    {
                        var download = await fileClient.DownloadAsync();
                        using var reader = new StreamReader(download.Value.Content);
                        existing = await reader.ReadToEndAsync();
                    }
                    catch { /* empty file */ }
                }

                string updated = existing +
                    $"{DateTime.UtcNow:O} - {logEntry}{Environment.NewLine}";
                var bytes = Encoding.UTF8.GetBytes(updated);
                long maxSize = Math.Max(bytes.Length, 1024 * 1024);

                if (await fileClient.ExistsAsync())
                    await fileClient.DeleteAsync();

                await fileClient.CreateAsync(maxSize);

                using var stream = new MemoryStream(bytes);
                await fileClient.UploadAsync(stream);
            }
            finally { _lock.Release(); }
        }

        public async Task<string> ReadLogAsync(string fileName)
        {
            var dir = LogDirectory;
            var fileClient = dir.GetFileClient(fileName);
            if (!await fileClient.ExistsAsync()) return "Log file not found.";

            var download = await fileClient.DownloadAsync();
            using var reader = new StreamReader(download.Value.Content);
            return await reader.ReadToEndAsync();
        }

        public async Task<List<string>> ListLogFilesAsync()
        {
            var dir = LogDirectory;
            await dir.CreateIfNotExistsAsync();

            var files = new List<string>();
            await foreach (ShareFileItem item in dir.GetFilesAndDirectoriesAsync())
                files.Add(item.Name);

            return files.OrderByDescending(f => f).ToList();
        }
    }
}