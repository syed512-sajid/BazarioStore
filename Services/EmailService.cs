using EcommerceStore.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace EcommerceStore.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
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

            var items = string.Join("", cart.Select(i =>
                $"<tr><td>{i.ProductName}</td><td>{i.Quantity}</td><td>Rs. {i.Price:N0}</td></tr>"
            ));

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>Thank you {order.CustomerName}</h2>
                    <p>Your order #{order.Id} is confirmed.</p>
                    <table border='1'>
                        <tr><th>Product</th><th>Qty</th><th>Price</th></tr>
                        {items}
                        <tr><td colspan='2'>Total</td><td>Rs. {order.TotalAmount:N0}</td></tr>
                    </table>
                    <p>Tracking ID: {order.TrackingId}</p>"
            };

            await SendAsync(message);
        }

        public async Task SendAdminNotificationAsync(Order order, List<CartItem> cart)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress("Admin", "sajidabbas6024@gmail.com"));
            message.Subject = $"🔔 New Order #{order.Id}";

            var items = string.Join("", cart.Select(i =>
                $"<tr><td>{i.ProductName}</td><td>{i.Quantity}</td><td>Rs. {i.Price:N0}</td></tr>"
            ));

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>New Order</h2>
                    <p>Order #{order.Id}</p>
                    <p>Customer: {order.CustomerName}</p>
                    <p>Phone: {order.Phone}</p>
                    <table border='1'>
                        <tr><th>Product</th><th>Qty</th><th>Price</th></tr>
                        {items}
                        <tr><td colspan='2'>Total</td><td>Rs. {order.TotalAmount:N0}</td></tr>
                    </table>"
            };

            await SendAsync(message);
        }
    }
}
