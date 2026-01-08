using System.ComponentModel.DataAnnotations;

namespace EcommerceStore.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string CustomerName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Address { get; set; }

        public string? Landmark { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

        public DateTime OrderDate { get; set; }

        public decimal TotalAmount { get; set; }

        // Order Status: Pending, Processing, Delivered, Cancelled, NotAccepted
        public string Status { get; set; } = "Pending";

        // Reason for cancellation or rejection
        public string? StatusReason { get; set; }

        // Track when status was last updated
        public DateTime? StatusUpdatedAt { get; set; }

        // Track if notification email was sent
        public bool NotificationSent { get; set; } = false;

        // Unique Tracking ID
        [Required]
        public string TrackingId { get; set; } = "";
        public int CustomerId { get; set; }        // <--- add this
        public Customer Customer { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? Size { get; set; }

        public virtual Order? Order { get; set; }
        public virtual Product? Product { get; set; }
    }
}