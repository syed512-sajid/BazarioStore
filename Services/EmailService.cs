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

        public EmailService(ILogger<EmailService> logger, Microsoft.Extensions.Options.IOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _emailSettings = emailSettings.Value;

            // Railway environment variables (Priority)
            var envUser = Environment.GetEnvironmentVariable("EMAIL_USER");
            var envPass = Environment.GetEnvironmentVariable("EMAIL_PASS");
            var envHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
            var envPort = Environment.GetEnvironmentVariable("SMTP_PORT") ?? "465"; // Default SSL port

            if (!string.IsNullOrEmpty(envUser))
            {
                _emailSettings.SmtpUser = envUser;
                _emailSettings.FromEmail = envUser;
            }

            if (!string.IsNullOrEmpty(envPass))
            {
                _emailSettings.SmtpPass = envPass.Replace(" ", ""); // Remove spaces
            }

            _emailSettings.SmtpHost = envHost;
            _emailSettings.SmtpPort = int.TryParse(envPort, out int port) ? port : 465;

            _logger.LogInformation("📧 Email Config: Host={Host}, Port={Port}, User={User}", 
                _emailSettings.SmtpHost, _emailSettings.SmtpPort, _emailSettings.SmtpUser);
        }

        public async Task SendOrderConfirmationAsync(Order order, List<CartItem> cart)
        {
            try
            {
                if (!IsConfigured())
                {
                    _logger.LogWarning("⚠️ Email not configured");
                    return;
                }

                _logger.LogInformation("📧 Sending customer email for Order #{OrderId}", order.Id);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
                message.Subject = $"✅ Order Confirmed #{order.Id} - BAZARIO";

                message.Body = new TextPart("html") { Text = BuildCustomerEmailBody(order, cart) };
                await SendEmailAsync(message);
                
                _logger.LogInformation("✅ Customer email sent to {Email}", order.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Customer email failed for Order #{OrderId}", order.Id);
                throw;
            }
        }

        public async Task SendAdminNotificationAsync(Order order, List<CartItem> cart)
        {
            try
            {
                if (!IsConfigured())
                {
                    _logger.LogWarning("⚠️ Email not configured");
                    return;
                }

                _logger.LogInformation("📧 Sending admin email for Order #{OrderId}", order.Id);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress("Admin", "sajidabbas6024@gmail.com"));
                message.Subject = $"🔔 New Order #{order.Id} - {order.CustomerName}";

                message.Body = new TextPart("html") { Text = BuildAdminEmailBody(order, cart) };
                await SendEmailAsync(message);
                
                _logger.LogInformation("✅ Admin email sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Admin email failed for Order #{OrderId}", order.Id);
                throw;
            }
        }

        private bool IsConfigured()
        {
            return !string.IsNullOrEmpty(_emailSettings.SmtpUser) &&
                   !string.IsNullOrEmpty(_emailSettings.SmtpPass) &&
                   !string.IsNullOrEmpty(_emailSettings.SmtpHost) &&
                   _emailSettings.SmtpPort > 0;
        }

        private async Task SendEmailAsync(MimeMessage message)
        {
            using var client = new SmtpClient();
            
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.CheckCertificateRevocation = false;
            client.Timeout = 60000; // 60 seconds

            try
            {
                _logger.LogInformation("🔌 Connecting to {Host}:{Port}", _emailSettings.SmtpHost, _emailSettings.SmtpPort);

                // Always use SSL for port 465
                await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, SecureSocketOptions.SslOnConnect);
                _logger.LogInformation("✅ Connected");

                await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
                _logger.LogInformation("✅ Authenticated");

                await client.SendAsync(message);
                _logger.LogInformation("✅ Email sent");

                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Email send failed");
                try { await client.DisconnectAsync(true); } catch { }
                throw;
            }
        }

        private string BuildCustomerEmailBody(Order order, List<CartItem> cart)
        {
            string itemsHtml = "";
            foreach (var item in cart)
            {
                itemsHtml += $@"
                    <tr>
                        <td style='padding: 10px; border: 1px solid #ddd;'>{item.ProductName}</td>
                        <td style='padding: 10px; text-align: center; border: 1px solid #ddd;'>{item.Quantity}</td>
                        <td style='padding: 10px; text-align: right; border: 1px solid #ddd;'>Rs. {item.Price:N0}</td>
                    </tr>";
            }

            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #28a745;'>✅ Order Confirmation</h2>
                    <p>Hi <strong>{order.CustomerName}</strong>,</p>
                    <p>Your order <strong>#{order.Id}</strong> has been received!</p>
                    
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p style='margin: 5px 0;'><strong>Tracking ID:</strong> {order.TrackingId}</p>
                        <p style='margin: 5px 0;'><strong>Order Date:</strong> {order.OrderDate:dd MMM yyyy}</p>
                        <p style='margin: 5px 0;'><strong>Status:</strong> {order.Status}</p>
                    </div>

                    <h3>Order Items:</h3>
                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                        <thead>
                            <tr style='background: #007bff; color: white;'>
                                <th style='padding: 10px; text-align: left;'>Product</th>
                                <th style='padding: 10px; text-align: center;'>Qty</th>
                                <th style='padding: 10px; text-align: right;'>Price</th>
                            </tr>
                        </thead>
                        <tbody>{itemsHtml}</tbody>
                        <tfoot>
                            <tr style='background: #f8f9fa; font-weight: bold;'>
                                <td colspan='2' style='padding: 10px; border: 1px solid #ddd;'>Total</td>
                                <td style='padding: 10px; text-align: right; border: 1px solid #ddd; color: #28a745;'>Rs. {order.TotalAmount:N0}</td>
                            </tr>
                        </tfoot>
                    </table>

                    <h3>Delivery Info:</h3>
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 5px;'>
                        <p style='margin: 5px 0;'><strong>Address:</strong> {order.Address}</p>
                        {(!string.IsNullOrEmpty(order.Landmark) ? $"<p style='margin: 5px 0;'><strong>Landmark:</strong> {order.Landmark}</p>" : "")}
                        <p style='margin: 5px 0;'><strong>Phone:</strong> {order.Phone}</p>
                        <p style='margin: 5px 0;'><strong>Payment:</strong> {order.PaymentMethod}</p>
                    </div>

                    <p style='margin-top: 30px;'>Thank you for shopping with <strong>BAZARIO</strong>!</p>
                </div>";
        }

        private string BuildAdminEmailBody(Order order, List<CartItem> cart)
        {
            string itemsHtml = "";
            foreach (var item in cart)
            {
                itemsHtml += $@"
                    <tr>
                        <td style='padding: 10px; border: 1px solid #ddd;'>{item.ProductName}</td>
                        <td style='padding: 10px; text-align: center; border: 1px solid #ddd;'>{item.Quantity}</td>
                        <td style='padding: 10px; text-align: right; border: 1px solid #ddd;'>Rs. {item.Price:N0}</td>
                    </tr>";
            }

            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #007bff;'>🔔 New Order</h2>
                    
                    <div style='background: #fff3cd; padding: 15px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Order #{order.Id}</strong> from <strong>{order.CustomerName}</strong></p>
                        <p style='margin: 5px 0 0 0;'>Tracking: {order.TrackingId}</p>
                    </div>

                    <h3>Customer:</h3>
                    <table style='width: 100%; margin: 10px 0;'>
                        <tr><td style='padding: 5px;'><strong>Name:</strong></td><td>{order.CustomerName}</td></tr>
                        <tr><td style='padding: 5px;'><strong>Email:</strong></td><td>{order.Email}</td></tr>
                        <tr><td style='padding: 5px;'><strong>Phone:</strong></td><td>{order.Phone}</td></tr>
                        <tr><td style='padding: 5px;'><strong>Address:</strong></td><td>{order.Address}</td></tr>
                        {(!string.IsNullOrEmpty(order.Landmark) ? $"<tr><td style='padding: 5px;'><strong>Landmark:</strong></td><td>{order.Landmark}</td></tr>" : "")}
                        <tr><td style='padding: 5px;'><strong>Payment:</strong></td><td>{order.PaymentMethod}</td></tr>
                    </table>

                    <h3>Items:</h3>
                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                        <thead>
                            <tr style='background: #007bff; color: white;'>
                                <th style='padding: 10px; text-align: left;'>Product</th>
                                <th style='padding: 10px; text-align: center;'>Qty</th>
                                <th style='padding: 10px; text-align: right;'>Price</th>
                            </tr>
                        </thead>
                        <tbody>{itemsHtml}</tbody>
                        <tfoot>
                            <tr style='background: #d4edda; font-weight: bold;'>
                                <td colspan='2' style='padding: 10px; border: 1px solid #ddd;'>Total</td>
                                <td style='padding: 10px; text-align: right; border: 1px solid #ddd; color: #28a745;'>Rs. {order.TotalAmount:N0}</td>
                            </tr>
                        </tfoot>
                    </table>

                    <p style='margin-top: 30px; padding: 15px; background: #e7f3ff; border-radius: 5px;'>
                        ⏰ <strong>Order Time:</strong> {order.OrderDate:dd MMM yyyy HH:mm}<br>
                        📦 <strong>Status:</strong> {order.Status}
                    </p>
                </div>";
        }
    }
}
