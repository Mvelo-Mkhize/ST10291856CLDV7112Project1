namespace ST10291856CLDV7112Project1.Models
{
    public class DashboardViewModel
    {
        public CustomerProfile Customer { get; set; } = new();
        public Product Product { get; set; } = new();
        public List<Product> Products { get; set; } = new();
    }
}