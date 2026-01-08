using System.ComponentModel.DataAnnotations;

namespace EcommerceStore.Models
{
    public class EmailOtp
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; }

        [Required]
        public DateTime ExpiryTime { get; set; }

        // Helper method to check if OTP is expired
        public bool IsExpired()
        {
            return DateTime.UtcNow > ExpiryTime;
        }
    }
}