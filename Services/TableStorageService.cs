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
            string connStr = configuration["AzureStorage:ConnectionString"]!;
            _customerTable = new TableClient(connStr, StorageAccountService.TableCustomer);
            _productTable = new TableClient(connStr, StorageAccountService.TableProduct);
        }

        public async Task AddCustomer(CustomerProfile customer)
        {
            customer.PartitionKey = "Customer";
            customer.RowKey = customer.Email;
            await _customerTable.AddEntityAsync(customer);
        }

        public async Task<List<CustomerProfile>> GetAllCustomers()
        {
            var customers = new List<CustomerProfile>();
            await foreach (var entity in _customerTable.QueryAsync<CustomerProfile>())
                customers.Add(entity);
            return customers;
        }

        public async Task AddProduct(Product product)
        {
            product.PartitionKey = "Product";
            await _productTable.AddEntityAsync(product);
        }

        public async Task<List<Product>> GetAllProducts()
        {
            var products = new List<Product>();
            await foreach (var entity in _productTable.QueryAsync<Product>())
                products.Add(entity);
            return products;
        }

        public async Task<Product?> GetProductBySku(string sku)
        {
            try
            {
                var response = await _productTable.GetEntityAsync<Product>("Product", sku);
                return response.Value;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }
    }
}