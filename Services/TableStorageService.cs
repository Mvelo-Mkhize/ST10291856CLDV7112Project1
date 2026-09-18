using Azure;
using Azure.Data.Tables;
using ST10291856CLDV7112Project1.Models;

namespace ST10291856CLDV7112Project1.Services
{
    public class TableStorageService
    {
        private readonly TableClient _customerTable;
        private readonly TableClient _productTable;

        public TableStorageService(IConfiguration configuration)
        {
            string connStr = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");

            _customerTable = new TableClient(connStr, StorageAccountService.TableCustomer);
            _productTable = new TableClient(connStr, StorageAccountService.TableProduct);
        }

        public async Task AddCustomerAsync(CustomerProfile customer)
        {
            customer.PartitionKey = "Customer";
            customer.RowKey = customer.Email;          
            await _customerTable.AddEntityAsync(customer);     
        }

        public async Task<List<CustomerProfile>> GetAllCustomersAsync()
        {
            var list = new List<CustomerProfile>();
            await foreach (var entity in _customerTable.QueryAsync<CustomerProfile>())
                list.Add(entity);
            return list.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToList();
        }

        public async Task AddProductAsync(Product product)
        {
            product.PartitionKey = "Product";
            await _productTable.AddEntityAsync(product);      
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var list = new List<Product>();
            await foreach (var entity in _productTable.QueryAsync<Product>())
                list.Add(entity);
            return list.OrderBy(p => p.Name).ToList();
        }

        public async Task<Product?> GetProductBySkuAsync(string sku)
        {
            try
            {
                var response = await _productTable.GetEntityAsync<Product>("Product", sku);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }
    }
}