using EcommerceStore.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EcommerceStore.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(Order order, List<CartItem> cart);
        Task SendAdminNotificationAsync(Order order, List<CartItem> cart);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(Microsoft.Extensions.Options.IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        private async Task SendAsync(MimeMessage message)
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendOrderConfirmationAsync(Order order, List<CartItem> cart)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
            message.Subject = $"✅ Order Confirmed - BAZARIO #{order.Id}";

            message.Body = new TextPart("html")
            {
                Text = $"<h2>Thank you {order.CustomerName}</h2><p>Your order #{order.Id} is confirmed.</p>"
            };

            await SendAsync(message);
        }

        public async Task SendAdminNotificationAsync(Order order, List<CartItem> cart)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress("Admin", "sajidabbas6024@gmail.com"));
            message.Subject = $"🔔 New Order #{order.Id}";
            message.Body = new TextPart("html")
            {
                Text = $"<h2>New Order</h2><p>Order #{order.Id} by {order.CustomerName}</p>"
            };

            await SendAsync(message);
        }
    }
}
