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
        private readonly EmailSettings _emailSettings;

        public EmailService(
            ILogger<EmailService> logger,
            Microsoft.Extensions.Options.IOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _emailSettings = emailSettings.Value;

            // 🔥 LOAD ALL SETTINGS FROM RAILWAY ENV VARIABLES
            _emailSettings.SmtpUser = Environment.GetEnvironmentVariable("EMAIL_USER");
            _emailSettings.SmtpPass = Environment.GetEnvironmentVariable("EMAIL_PASS");
            _emailSettings.SmtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
            _emailSettings.FromName = Environment.GetEnvironmentVariable("FROM_NAME") ?? "BAZARIO";

            var port = Environment.GetEnvironmentVariable("SMTP_PORT");
            _emailSettings.SmtpPort = int.TryParse(port, out var p) ? p : 587;

            _emailSettings.FromEmail = _emailSettings.SmtpUser;

            // 🔍 LOG CONFIG (DEBUG PURPOSE)
            _logger.LogInformation("📧 EMAIL CONFIGURATION CHECK");
            _logger.LogInformation("SMTP User: {User}", _emailSettings.SmtpUser);
            _logger.LogInformation("SMTP Host: {Host}", _emailSettings.SmtpHost);
            _logger.LogInformation("SMTP Port: {Port}", _emailSettings.SmtpPort);
            _logger.LogInformation("From Email: {Email}", _emailSettings.FromEmail);
            _logger.LogInformation("From Name: {Name}", _emailSettings.FromName);
        }

        // ===============================
        // CUSTOMER EMAIL
        // ===============================
        public async Task SendOrderConfirmationAsync(Order order, List<CartItem> cart)
        {
            if (!IsConfigured()) return;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
            message.Subject = $"✅ Order Confirmed - BAZARIO #{order.Id}";

            message.Body = new TextPart("html")
            {
                Text = BuildCustomerEmailBody(order, cart)
            };

            await SendAsync(message);
        }

        // ===============================
        // ADMIN EMAIL
        // ===============================
        public async Task SendAdminNotificationAsync(Order order, List<CartItem> cart)
        {
            if (!IsConfigured()) return;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(new MailboxAddress("Admin", "sajidabbas6024@gmail.com"));
            message.Subject = $"🔔 New Order #{order.Id} - {order.CustomerName}";

            message.Body = new TextPart("html")
            {
                Text = BuildAdminEmailBody(order, cart)
            };

            await SendAsync(message);
        }

        // ===============================
        // SMTP CORE METHOD
        // ===============================
        private async Task SendAsync(MimeMessage message)
        {
            using var client = new SmtpClient();

            try
            {
                _logger.LogInformation("🔌 Connecting to SMTP...");

                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Timeout = 60000;

                // ✅ GMAIL SAFE MODE
                await client.ConnectAsync(
                    _emailSettings.SmtpHost,
                    _emailSettings.SmtpPort,
                    SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(
                    _emailSettings.SmtpUser,
                    _emailSettings.SmtpPass
                );

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("✅ EMAIL SENT SUCCESSFULLY");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ EMAIL FAILED");
                throw;
            }
        }

        // ===============================
        // HELPERS
        // ===============================
        private bool IsConfigured()
        {
            if (string.IsNullOrEmpty(_emailSettings.SmtpUser) ||
                string.IsNullOrEmpty(_emailSettings.SmtpPass))
            {
                _logger.LogWarning("⚠️ Email credentials missing. Email skipped.");
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
                <p>Hi <b>{order.CustomerName}</b>,</p>
                <p>Your order <b>#{order.Id}</b> has been confirmed.</p>
                <table border='1' cellpadding='8' cellspacing='0'>
                    <tr><th>Product</th><th>Qty</th><th>Price</th></tr>
                    {items}
                    <tr><td colspan='2'><b>Total</b></td><td><b>Rs. {order.TotalAmount:N0}</b></td></tr>
                </table>
                <p><b>Tracking ID:</b> {order.TrackingId}</p>
                <p>— BAZARIO</p>";
        }

        private string BuildAdminEmailBody(Order order, List<CartItem> cart)
        {
            var items = string.Join("", cart.Select(i =>
                $"<tr><td>{i.ProductName}</td><td>{i.Quantity}</td><td>Rs. {i.Price:N0}</td></tr>"
            ));

            return $@"
                <h2>New Order Received</h2>
                <p><b>Order:</b> #{order.Id}</p>
                <p><b>Customer:</b> {order.CustomerName}</p>
                <p><b>Phone:</b> {order.Phone}</p>
                <table border='1' cellpadding='8' cellspacing='0'>
                    <tr><th>Product</th><th>Qty</th><th>Price</th></tr>
                    {items}
                    <tr><td colspan='2'><b>Total</b></td><td><b>Rs. {order.TotalAmount:N0}</b></td></tr>
                </table>";
        }
    }
}
