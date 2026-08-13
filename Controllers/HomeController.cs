using Microsoft.AspNetCore.Mvc;
using ST10291856CLDV7112Project1.Models;
using ST10291856CLDV7112Project1.Services;
using System.Text.Json;

namespace ST10291856CLDV7112Project1.Controllers
{
    public class HomeController : Controller
    {
        private readonly TableStorageService _tableService;
        private readonly BlobStorageService _blobService;
        private readonly QueueStorageService _queueService;
        private readonly FileShareService _fileShareService;

        public HomeController(
            TableStorageService tableService,
            BlobStorageService blobService,
            QueueStorageService queueService,
            FileShareService fileShareService)
        {
            _tableService = tableService;
            _blobService = blobService;
            _queueService = queueService;
            _fileShareService = fileShareService;
        }

        public IActionResult Index()
        {
            return View(new DashboardViewModel());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Customers
        [HttpPost]
        public async Task<IActionResult> AddCustomer(CustomerProfile customer, IFormFile? customerImage)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", new DashboardViewModel { Customer = customer });
            }

            if (customerImage != null && customerImage.Length > 0)
            {
                string blobName = $"customer-{Guid.NewGuid()}-{customerImage.FileName}";
                using var stream = customerImage.OpenReadStream();
                await _blobService.UploadBlobAsync(blobName, stream, customerImage.ContentType);
                customer.ImageBlobName = blobName;
            }

            await _tableService.AddCustomer(customer);
            TempData["SuccessMessage"] = $"Customer '{customer.Email}' added successfully.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Customers()
        {
            var customers = await _tableService.GetAllCustomers();
            return View(customers);
        }

        // Products
        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile? productImage)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", new DashboardViewModel { Product = product });
            }

            if (productImage != null && productImage.Length > 0)
            {
                string blobName = $"product-{Guid.NewGuid()}-{productImage.FileName}";
                using var stream = productImage.OpenReadStream();
                await _blobService.UploadBlobAsync(blobName, stream, productImage.ContentType);
                product.ImageBlobName = blobName;
            }

            await _tableService.AddProduct(product);
            TempData["SuccessMessage"] = $"Product '{product.Name}' added successfully.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Products()
        {
            var products = await _tableService.GetAllProducts();
            return View(products);
        }

        // Blob Images
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", new DashboardViewModel());
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                string blobName = $"{Guid.NewGuid()}_{imageFile.FileName}";
                using var stream = imageFile.OpenReadStream();
                await _blobService.UploadBlobAsync(blobName, stream, imageFile.ContentType);
                await _queueService.SendMessageAsync($"Uploading image '{blobName}'");
                TempData["SuccessMessage"] = $"Image '{blobName}' uploaded and queue message sent.";
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Images()
        {
            var blobNames = await _blobService.ListBlobsAsync();
            return View(blobNames);
        }

        public async Task<IActionResult> DownloadImage(string blobName)
        {
            var stream = await _blobService.DownloadBlobAsync(blobName);
            return File(stream, "application/octet-stream", blobName);
        }

        // Queues
        [HttpPost]
        public async Task<IActionResult> ProcessOrder(string sku, int quantity)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", new DashboardViewModel());
            }

            var product = await _tableService.GetProductBySku(sku);
            if (product == null)
            {
                ModelState.AddModelError("sku", $"Product with SKU '{sku}' not found.");
                return View("Index", new DashboardViewModel());
            }

            var orderMessage = new
            {
                OrderId = Guid.NewGuid().ToString(),
                SKU = product.RowKey,
                ProductName = product.Name,
                ImageBlobName = product.ImageBlobName,
                Quantity = quantity
            };

            string json = JsonSerializer.Serialize(orderMessage);
            await _queueService.SendMessageAsync(json);

            TempData["SuccessMessage"] = $"Order placed for '{product.Name}' (Qty: {quantity}).";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Queues()
        {
            var rawMessages = await _queueService.PeekMessagesAsync(32);
            var items = new List<QueueMessageViewModel>();

            foreach (var raw in rawMessages)
            {
                try
                {
                    var obj = JsonSerializer.Deserialize<QueueMessageViewModel>(raw);
                    if (obj != null)
                    {
                        obj.RawMessage = raw;
                        items.Add(obj);
                    }
                    else
                    {
                        items.Add(new QueueMessageViewModel { RawMessage = raw });
                    }
                }
                catch
                {
                    items.Add(new QueueMessageViewModel { RawMessage = raw });
                }
            }

            return View(items);
        }

        // Log Files
        [HttpPost]
        public async Task<IActionResult> WriteLog(string logFileName, string logMessage)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", new DashboardViewModel());
            }

            await _fileShareService.WriteLogAsync(logFileName, logMessage);
            TempData["SuccessMessage"] = $"Log written to file '{logFileName}'.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Logs()
        {
            var logFiles = await _fileShareService.ListLogFilesAsync();
            ViewBag.LogFiles = logFiles;
            return View();
        }

        public async Task<IActionResult> ViewLog(string fileName)
        {
            string content = await _fileShareService.ReadLogAsync(fileName);
            ViewBag.FileName = fileName;
            ViewBag.Content = content;
            return View();
        }
    }
}