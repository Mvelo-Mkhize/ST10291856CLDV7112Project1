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
        private readonly FunctionService _functions;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            TableStorageService tableService,
            BlobStorageService blobService,
            QueueStorageService queueService,
            FileShareService fileShareService,
            FunctionService functions,
            ILogger<HomeController> logger)
        {
            _tableService = tableService;
            _blobService = blobService;
            _queueService = queueService;
            _fileShareService = fileShareService;
            _functions = functions;
            _logger = logger;
        }

        private async Task LogAsync(string message)
        {
            _logger.LogInformation(message);
            try
            {
                var ok = await _functions.WriteLogAsync(null, message);
                if (!ok) throw new Exception("Function returned non-success.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Function log failed — falling back to direct write.");
                try { await _fileShareService.WriteLogAsync(null, message); }
                catch (Exception inner)
                {
                    _logger.LogWarning(inner, "Direct log write also failed.");
                }
            }
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel
            {
                Products = await _tableService.GetAllProductsAsync()
            };
            return View(vm);
        }

        public IActionResult Privacy() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCustomer(CustomerProfile customer, IFormFile? customerImage)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", new DashboardViewModel
                {
                    Customer = customer,
                    Products = await _tableService.GetAllProductsAsync()
                });
            }

            try
            {
                string? blobName = null;

                if (customerImage is { Length: > 0 })
                {
                    if (!_blobService.IsValidImage(customerImage, out var err))
                    {
                        ModelState.AddModelError("customerImage", err!);
                        return View("Index", new DashboardViewModel
                        {
                            Customer = customer,
                            Products = await _tableService.GetAllProductsAsync()
                        });
                    }

                    using var stream = customerImage.OpenReadStream();
                    blobName = await _functions.UploadImageAsync(
                        stream,
                        Path.GetFileName(customerImage.FileName),
                        customerImage.ContentType);

                    if (blobName is null)
                    {
                        ModelState.AddModelError("customerImage",
                            "Image upload via Azure Function failed.");
                        return View("Index", new DashboardViewModel
                        {
                            Customer = customer,
                            Products = await _tableService.GetAllProductsAsync()
                        });
                    }
                }

                var dto = new
                {
                    customer.FirstName,
                    customer.LastName,
                    customer.Email,
                    customer.Phone,
                    ImageBlobName = blobName ?? string.Empty
                };

                bool ok = await _functions.StoreCustomerAsync(dto);
                if (!ok)
                {
                    ModelState.AddModelError("Customer.Email",
                        "A customer with that email already exists, or the function rejected the request.");
                    return View("Index", new DashboardViewModel
                    {
                        Customer = customer,
                        Products = await _tableService.GetAllProductsAsync()
                    });
                }

                await LogAsync($"Customer added via Function: {customer.Email}");
                TempData["SuccessMessage"] =
                    $"Customer '{customer.Email}' added successfully.";
                return RedirectToAction(nameof(Customers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddCustomer failed.");
                ModelState.AddModelError("", "An unexpected error occurred: " + ex.Message);
                return View("Index", new DashboardViewModel
                {
                    Customer = customer,
                    Products = await _tableService.GetAllProductsAsync()
                });
            }
        }

        public async Task<IActionResult> Customers()
            => View(await _tableService.GetAllCustomersAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(Product product, IFormFile? productImage)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", new DashboardViewModel
                {
                    Product = product,
                    Products = await _tableService.GetAllProductsAsync()
                });
            }

            try
            {
                if (productImage is { Length: > 0 })
                {
                    if (!_blobService.IsValidImage(productImage, out var err))
                    {
                        ModelState.AddModelError("productImage", err!);
                        return View("Index", new DashboardViewModel
                        {
                            Product = product,
                            Products = await _tableService.GetAllProductsAsync()
                        });
                    }

                    using var stream = productImage.OpenReadStream();
                    var blobName = await _functions.UploadImageAsync(
                        stream,
                        Path.GetFileName(productImage.FileName),
                        productImage.ContentType);

                    if (blobName is null)
                    {
                        ModelState.AddModelError("productImage",
                            "Image upload via Azure Function failed.");
                        return View("Index", new DashboardViewModel
                        {
                            Product = product,
                            Products = await _tableService.GetAllProductsAsync()
                        });
                    }

                    product.ImageBlobName = blobName;
                }

                await _tableService.AddProductAsync(product);
                await LogAsync($"Product added: {product.RowKey} - {product.Name}");
                TempData["SuccessMessage"] =
                    $"Product '{product.Name}' added successfully.";
                return RedirectToAction(nameof(Products));
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 409)
            {
                ModelState.AddModelError("Product.RowKey",
                    "A product with that SKU already exists.");
                return View("Index", new DashboardViewModel
                {
                    Product = product,
                    Products = await _tableService.GetAllProductsAsync()
                });
            }
        }

        public async Task<IActionResult> Products()
            => View(await _tableService.GetAllProductsAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(IFormFile imageFile)
        {
            if (imageFile is null || imageFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please choose an image.";
                return RedirectToAction(nameof(Index));
            }

            if (!_blobService.IsValidImage(imageFile, out var err))
            {
                TempData["ErrorMessage"] = err;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var stream = imageFile.OpenReadStream();
                var blobName = await _functions.UploadImageAsync(
                    stream,
                    Path.GetFileName(imageFile.FileName),
                    imageFile.ContentType);

                if (blobName is null)
                {
                    TempData["ErrorMessage"] = "Upload via Azure Function failed.";
                    return RedirectToAction(nameof(Index));
                }

                await LogAsync($"Image uploaded via Function: {blobName}");
                TempData["SuccessMessage"] = $"Image '{blobName}' uploaded.";
                return RedirectToAction(nameof(Images));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadImage failed.");
                TempData["ErrorMessage"] = "Upload failed: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Images()
            => View(await _blobService.ListBlobsAsync());

        public async Task<IActionResult> DownloadImage(string blobName)
        {
            var stream = await _blobService.DownloadBlobAsync(blobName);
            return File(stream, "application/octet-stream", blobName);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder(string sku, int quantity)
        {
            if (string.IsNullOrWhiteSpace(sku) || quantity <= 0)
            {
                TempData["ErrorMessage"] =
                    "Please provide a valid SKU and quantity greater than 0.";
                return RedirectToAction(nameof(Index));
            }

            var product = await _tableService.GetProductBySkuAsync(sku);
            if (product == null)
            {
                TempData["ErrorMessage"] = $"Product with SKU '{sku}' not found.";
                return RedirectToAction(nameof(Index));
            }

            if (quantity > product.StockQuantity)
            {
                TempData["ErrorMessage"] =
                    $"Only {product.StockQuantity} in stock for '{product.Name}'.";
                return RedirectToAction(nameof(Index));
            }

            var orderMessage = new
            {
                OrderId = Guid.NewGuid().ToString(),
                SKU = product.RowKey,
                ProductName = product.Name,
                ImageBlobName = product.ImageBlobName,
                Quantity = quantity,
                CreatedUtc = DateTime.UtcNow
            };

            await _queueService.SendMessageAsync(
                JsonSerializer.Serialize(orderMessage));

            await LogAsync($"Order queued: {product.RowKey} x{quantity}");
            TempData["SuccessMessage"] =
                $"Order placed for '{product.Name}' (Qty: {quantity}). " +
                "The OrderQueue Function will process it shortly.";
            return RedirectToAction(nameof(Queues));
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

            ViewBag.QueueCount = await _queueService.GetApproximateCountAsync();
            return View(items);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessNextOrder()
        {
            var msg = await _queueService.DequeueMessageAsync();
            if (msg == null)
            {
                TempData["ErrorMessage"] = "Queue is empty.";
                return RedirectToAction(nameof(Queues));
            }

            try
            {
                await LogAsync($"Order processed (manual): {msg.Value.Body}");
                await _queueService.DeleteMessageAsync(msg.Value.MessageId, msg.Value.PopReceipt);
                TempData["SuccessMessage"] = "Order processed and removed from queue.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manual order processing failed.");
                TempData["ErrorMessage"] =
                    "Order processing failed — the message will retry after the visibility timeout.";
            }
            return RedirectToAction(nameof(Queues));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearQueue()
        {
            await _queueService.ClearAsync();
            await LogAsync("Queue cleared.");
            TempData["SuccessMessage"] = "Queue cleared.";
            return RedirectToAction(nameof(Queues));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> WriteLog(string? logFileName, string logMessage)
        {
            if (string.IsNullOrWhiteSpace(logMessage))
            {
                TempData["ErrorMessage"] = "Log message cannot be empty.";
                return RedirectToAction(nameof(Logs));
            }

            try
            {
                var ok = await _functions.WriteLogAsync(logFileName, logMessage);
                if (!ok)
                {
                    await _fileShareService.WriteLogAsync(logFileName, logMessage);
                    TempData["SuccessMessage"] =
                        "Log written directly (function unavailable).";
                }
                else
                {
                    TempData["SuccessMessage"] = "Log written via Azure Function.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WriteLog failed.");
                TempData["ErrorMessage"] = "Log write failed: " + ex.Message;
            }

            return RedirectToAction(nameof(Logs));
        }

        public async Task<IActionResult> Logs()
        {
            ViewBag.LogFiles = await _fileShareService.ListLogFilesAsync();
            return View();
        }

        public async Task<IActionResult> ViewLog(string fileName)
        {
            ViewBag.FileName = fileName;
            ViewBag.Content = await _fileShareService.ReadLogAsync(fileName);
            return View();
        }
    }
}