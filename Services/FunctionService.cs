using System.Text;
using System.Text.Json;

namespace ST10291856CLDV7112Project1.Services
{
    public class FunctionService
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _key;

        public FunctionService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _baseUrl = config["AzureFunctions:BaseUrl"]
                ?? throw new InvalidOperationException("AzureFunctions:BaseUrl missing.");
            _key = config["AzureFunctions:Key"] ?? string.Empty;
        }

        private string Url(string route)
            => string.IsNullOrEmpty(_key)
               ? $"{_baseUrl.TrimEnd('/')}/api/{route}"
               : $"{_baseUrl.TrimEnd('/')}/api/{route}?code={_key}";

        public async Task<bool> StoreCustomerAsync(object customerDto)
        {
            var json = JsonSerializer.Serialize(customerDto);
            var resp = await _http.PostAsync(Url("customers"),
                new StringContent(json, Encoding.UTF8, "application/json"));
            return resp.IsSuccessStatusCode;
        }

        public async Task<string?> UploadImageAsync(Stream stream, string fileName, string contentType)
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "image", fileName);

            var resp = await _http.PostAsync(Url("images"), content);
            if (!resp.IsSuccessStatusCode) return null;

            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("BlobName").GetString();
        }

        public async Task<bool> WriteLogAsync(string? fileName, string message)
        {
            var json = JsonSerializer.Serialize(new { FileName = fileName, Message = message });
            var resp = await _http.PostAsync(Url("logs"),
                new StringContent(json, Encoding.UTF8, "application/json"));
            return resp.IsSuccessStatusCode;
        }
    }
}