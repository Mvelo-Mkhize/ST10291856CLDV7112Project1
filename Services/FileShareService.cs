using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System.Text;

namespace ST10291856CLDV7112Project1.Services
{
    public class FileShareService
    {
        private readonly ShareClient _shareClient;
        private const string LogDirectory = "logs";

        public FileShareService(IConfiguration configuration)
        {
            string connStr = configuration["AzureStorage:ConnectionString"]!;
            _shareClient = new ShareClient(connStr, StorageAccountService.FileShareName);
        }

        public async Task WriteLogAsync(string fileName, string logEntry)
        {
            ShareDirectoryClient rootDir = _shareClient.GetRootDirectoryClient();
            ShareDirectoryClient logDir = rootDir.GetSubdirectoryClient(LogDirectory);
            await logDir.CreateIfNotExistsAsync();

            ShareFileClient fileClient = logDir.GetFileClient(fileName);
            if (!await fileClient.ExistsAsync())
            {
                await fileClient.CreateAsync(maxSize: 1024 * 1024);
            }

            string existingContent = string.Empty;
            try
            {
                var download = await fileClient.DownloadAsync();
                using var reader = new StreamReader(download.Value.Content);
                existingContent = await reader.ReadToEndAsync();
            }
            catch
            {
                // file may be empty
            }

            string updatedContent = existingContent + $"{DateTime.UtcNow:O} - {logEntry}{Environment.NewLine}";
            using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(updatedContent));
            await fileClient.UploadAsync(uploadStream);
        }

        public async Task<string> ReadLogAsync(string fileName)
        {
            ShareDirectoryClient rootDir = _shareClient.GetRootDirectoryClient();
            ShareDirectoryClient logDir = rootDir.GetSubdirectoryClient(LogDirectory);
            ShareFileClient fileClient = logDir.GetFileClient(fileName);
            if (!await fileClient.ExistsAsync())
                return "Log file not found.";

            var download = await fileClient.DownloadAsync();
            using var reader = new StreamReader(download.Value.Content);
            return await reader.ReadToEndAsync();
        }

        public async Task<List<string>> ListLogFilesAsync()
        {
            ShareDirectoryClient rootDir = _shareClient.GetRootDirectoryClient();
            ShareDirectoryClient logDir = rootDir.GetSubdirectoryClient(LogDirectory);
            await logDir.CreateIfNotExistsAsync();

            var files = new List<string>();
            await foreach (ShareFileItem item in logDir.GetFilesAndDirectoriesAsync())
                files.Add(item.Name);
            return files;
        }
    }
}