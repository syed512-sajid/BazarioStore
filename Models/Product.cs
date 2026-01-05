using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceStore.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        // Selling Price
        [Required]
        public decimal Price { get; set; }

        // Original / MRP Price
        public decimal? OriginalPrice { get; set; }

        // Admin control (IMPORTANT)
        public bool ShowDiscount { get; set; } = false;

        [Required]
        public int Stock { get; set; }

        [Required]
        public string Category { get; set; } = "Uncategorized";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Images
        public List<ProductImage> Images { get; set; } = new();

        // 🔹 Discount calculation (UI only)
        [NotMapped]
        public int DiscountPercent =>
            ShowDiscount && OriginalPrice.HasValue && OriginalPrice > Price
                ? (int)Math.Round(((OriginalPrice.Value - Price) / OriginalPrice.Value) * 100)
                : 0;
    }

    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }
}
