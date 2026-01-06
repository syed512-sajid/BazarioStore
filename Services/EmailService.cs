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
        private readonly ILogger<EmailService> _logger;

        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _fromName;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;

            _smtpUser = Environment.GetEnvironmentVariable("EMAIL_USER");
            _smtpPass = Environment.GetEnvironmentVariable("EMAIL_PASS");
            _smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
            _fromName = Environment.GetEnvironmentVariable("FROM_NAME") ?? "BAZARIO";

            var port = Environment.GetEnvironmentVariable("SMTP_PORT");
            _smtpPort = int.TryParse(port, out var p) ? p : 587;

            _logger.LogInformation("📧 EMAIL CONFIG CHECK");
            _logger.LogInformation("SMTP User: {User}", _smtpUser);
            _logger.LogInformation("SMTP Host: {Host}", _smtpHost);
            _logger.LogInformation("SMTP Port: {Port}", _smtpPort);
        }

        public async Task SendOrderConfirmationAsync(Order order, List<CartItem> cart)
        {
            if (!IsConfigured()) return;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _smtpUser));
            message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
            message.Subject = $"✅ Order Confirmed - BAZARIO #{order.Id}";

            message.Body = new TextPart("html")
            {
                Text = BuildCustomerEmailBody(order, cart)
            };

            await SendAsync(message);
        }

        public async Task SendAdminNotificationAsync(Order order, List<CartItem> cart)
        {
            if (!IsConfigured()) return;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _smtpUser));
          message.To.Add(new MailboxAddress("Admin", "sajidabbas6024@gmail.com"));

            message.Subject = $"🔔 New Order #{order.Id}";

            message.Body = new TextPart("html")
            {
                Text = BuildAdminEmailBody(order, cart)
            };

            await SendAsync(message);
        }

        private async Task SendAsync(MimeMessage message)
        {
            using var client = new SmtpClient();
            try
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

             await client.ConnectAsync(
    "smtp.gmail.com",
    465,
    SecureSocketOptions.SslOnConnect
);

);
                await client.AuthenticateAsync(_smtpUser, _smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("✅ EMAIL SENT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ EMAIL FAILED");
            }
        }

        private bool IsConfigured()
        {
            if (string.IsNullOrEmpty(_smtpUser) || string.IsNullOrEmpty(_smtpPass))
            {
                _logger.LogWarning("⚠️ Email credentials missing");
                return false;
            }
            return true;
        }

        private string BuildCustomerEmailBody(Order order, List<CartItem> cart)
        {
            var items = string.Join("", cart.Select(i =>
                $"<tr><td>{i.ProductName}</td><td>{i.Quantity}</td><td>Rs. {i.Price:N0}</td></tr>"
            ));

            return $@"
                <h2>Thank you for your order!</h2>
                <p>Hi <b>{order.CustomerName}</b></p>
                <p>Your order <b>#{order.Id}</b> is confirmed.</p>
                <table border='1' cellpadding='8'>
                    <tr><th>Product</th><th>Qty</th><th>Price</th></tr>
                    {items}
                    <tr><td colspan='2'><b>Total</b></td><td><b>Rs. {order.TotalAmount:N0}</b></td></tr>
                </table>
                <p>Tracking ID: <b>{order.TrackingId}</b></p>";
        }

        private string BuildAdminEmailBody(Order order, List<CartItem> cart)
        {
            var items = string.Join("", cart.Select(i =>
                $"<tr><td>{i.ProductName}</td><td>{i.Quantity}</td><td>Rs. {i.Price:N0}</td></tr>"
            ));

            return $@"
                <h2>New Order Received</h2>
                <p>Order #{order.Id}</p>
                <p>Customer: {order.CustomerName}</p>
                <p>Phone: {order.Phone}</p>
                <table border='1' cellpadding='8'>
                    <tr><th>Product</th><th>Qty</th><th>Price</th></tr>
                    {items}
                </table>";
        }
    }
}
