using System.ComponentModel.DataAnnotations;

namespace EcommerceStore.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }
        public string? UserId { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        // Link to Identity User
    

        // Navigation property
        public ICollection<Order>? Orders { get; set; }
    }
}