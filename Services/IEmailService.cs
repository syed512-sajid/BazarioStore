using EcommerceStore.Models;

namespace EcommerceStore.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(Order order, List<CartItem> cart);
        Task SendAdminNotificationAsync(Order order, List<CartItem> cart);
    }
}
