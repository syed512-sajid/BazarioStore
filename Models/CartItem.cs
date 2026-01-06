namespace EcommerceStore.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int Stock { get; set; }
        public List<string> ImageUrls { get; set; }
        public string Size { get; set; }  // ← NEW (Optional size)
    }
}
