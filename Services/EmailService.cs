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

            // Priority 1: Railway environment variables
            var envUser = Environment.GetEnvironmentVariable("EMAIL_USER");
            var envPass = Environment.GetEnvironmentVariable("EMAIL_PASS");
            var envHost = Environment.GetEnvironmentVariable("SMTP_HOST");
            var envPort = Environment.GetEnvironmentVariable("SMTP_PORT");

            if (!string.IsNullOrEmpty(envUser))
            {
                _emailSettings.SmtpUser = envUser;
                _emailSettings.FromEmail = envUser;
                _logger.LogInformation("✅ Using EMAIL_USER from environment: {Email}", envUser);
            }

            if (!string.IsNullOrEmpty(envPass))
            {
                _emailSettings.SmtpPass = envPass;
                _logger.LogInformation("✅ Using EMAIL_PASS from environment");
            }

            if (!string.IsNullOrEmpty(envHost))
            {
                _emailSettings.SmtpHost = envHost;
                _logger.LogInformation("✅ Using SMTP_HOST from environment: {Host}", envHost);
            }

            if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out int port))
            {
                _emailSettings.SmtpPort = port;
                _logger.LogInformation("✅ Using SMTP_PORT from environment: {Port}", port);
            }

            // Log final configuration (without password)
            _logger.LogInformation("📧 Email Configuration: Host={Host}, Port={Port}, User={User}",
                _emailSettings.SmtpHost, _emailSettings.SmtpPort, _emailSettings.SmtpUser);
        }

        public async Task SendOrderConfirmationAsync(Order order, List<CartItem> cart)
        {
            try
            {
                if (!IsConfigured())
                {
                    _logger.LogWarning("⚠️ Email not configured. Skipping customer email for Order #{OrderId}", order.Id);
                    return;
                }

                _logger.LogInformation("📧 Preparing customer email for Order #{OrderId} to {Email}", order.Id, order.Email);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
                message.Subject = $"✅ Order Confirmed - BAZARIO #{order.Id}";

                string body = BuildCustomerEmailBody(order, cart);
                message.Body = new TextPart("html") { Text = body };

                await SendEmailAsync(message);
                _logger.LogInformation("✅ Customer email sent successfully to {Email}", order.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send customer email for Order #{OrderId} to {Email}", order.Id, order.Email);
                throw; // Re-throw to let BackgroundService handle retry
            }
        }

        public async Task SendAdminNotificationAsync(Order order, List<CartItem> cart)
        {
            try
            {
                if (!IsConfigured())
                {
                    _logger.LogWarning("⚠️ Email not configured. Skipping admin email for Order #{OrderId}", order.Id);
                    return;
                }

                _logger.LogInformation("📧 Preparing admin email for Order #{OrderId}", order.Id);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress("Admin", "sajidabbas6024@gmail.com"));
                message.Subject = $"🔔 New Order #{order.Id} from {order.CustomerName}";

                string body = BuildAdminEmailBody(order, cart);
                message.Body = new TextPart("html") { Text = body };

                await SendEmailAsync(message);
                _logger.LogInformation("✅ Admin email sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send admin email for Order #{OrderId}", order.Id);
                throw;
            }
        }

        private bool IsConfigured()
        {
            bool isValid = !string.IsNullOrEmpty(_emailSettings.SmtpUser) &&
                          !string.IsNullOrEmpty(_emailSettings.SmtpPass) &&
                          !string.IsNullOrEmpty(_emailSettings.SmtpHost) &&
                          _emailSettings.SmtpPort > 0;

            if (!isValid)
            {
                _logger.LogWarning("⚠️ Email not configured: Host={Host}, Port={Port}, User={User}",
                    _emailSettings.SmtpHost ?? "null",
                    _emailSettings.SmtpPort,
                    _emailSettings.SmtpUser ?? "null");
            }

            return isValid;
        }

        private async Task SendEmailAsync(MimeMessage message)
        {
            var maxRetries = 3;
            var retryDelay = TimeSpan.FromSeconds(2);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                using var client = new SmtpClient();
                try
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    client.CheckCertificateRevocation = false;
                    client.Timeout = 30000;

                    _logger.LogInformation("🔌 [Attempt {Attempt}/{MaxRetries}] Connecting to SMTP {Host}:{Port}",
                        attempt, maxRetries, _emailSettings.SmtpHost, _emailSettings.SmtpPort);

                    var secureSocketOptions = _emailSettings.SmtpPort == 587
                        ? SecureSocketOptions.StartTls
                        : SecureSocketOptions.SslOnConnect;

                    await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, secureSocketOptions);
                    _logger.LogInformation("✅ Connected successfully");

                    _logger.LogInformation("🔐 Authenticating as {User}", _emailSettings.SmtpUser);
                    await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
                    _logger.LogInformation("✅ Authentication successful");

                    _logger.LogInformation("📤 Sending email to {Recipient}...", message.To.FirstOrDefault()?.ToString());
                    await client.SendAsync(message);
                    _logger.LogInformation("✅ Email sent successfully on attempt {Attempt}", attempt);

                    await client.DisconnectAsync(true);
                    return; // Success!
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [Attempt {Attempt}/{MaxRetries}] Email send failed", attempt, maxRetries);

                    try { await client.DisconnectAsync(true); } catch { }

                    if (attempt == maxRetries)
                    {
                        _logger.LogError("❌ All {MaxRetries} attempts failed. Giving up.", maxRetries);
                        throw;
                    }

                    _logger.LogWarning("⏳ Retrying in {Delay} seconds...", retryDelay.TotalSeconds);
                    await Task.Delay(retryDelay);
                }
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
                    <h2 style='color: #28a745;'>Order Confirmation</h2>
                    <p>Hi <strong>{order.CustomerName}</strong>,</p>
                    <p>Your order <strong>#{order.Id}</strong> has been received successfully!</p>
                    
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p style='margin: 5px 0;'><strong>Tracking ID:</strong> {order.TrackingId}</p>
                        <p style='margin: 5px 0;'><strong>Order Date:</strong> {order.OrderDate:dd MMM yyyy HH:mm}</p>
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
                        <tbody>
                            {itemsHtml}
                        </tbody>
                        <tfoot>
                            <tr style='background: #f8f9fa; font-weight: bold;'>
                                <td colspan='2' style='padding: 10px; border: 1px solid #ddd;'>Total</td>
                                <td style='padding: 10px; text-align: right; border: 1px solid #ddd; color: #28a745;'>Rs. {order.TotalAmount:N0}</td>
                            </tr>
                        </tfoot>
                    </table>

                    <h3>Delivery Information:</h3>
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 5px;'>
                        <p style='margin: 5px 0;'><strong>Address:</strong> {order.Address}</p>
                        {(!string.IsNullOrEmpty(order.Landmark) ? $"<p style='margin: 5px 0;'><strong>Landmark:</strong> {order.Landmark}</p>" : "")}
                        <p style='margin: 5px 0;'><strong>Phone:</strong> {order.Phone}</p>
                        <p style='margin: 5px 0;'><strong>Payment Method:</strong> {order.PaymentMethod}</p>
                    </div>

                    <p style='margin-top: 30px; color: #6c757d; font-size: 14px;'>Thank you for shopping with <strong>BAZARIO</strong>!</p>
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
                    <h2 style='color: #007bff;'>🔔 New Order Received</h2>
                    
                    <div style='background: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin: 20px 0;'>
                        <p style='margin: 0; font-size: 16px;'><strong>Order #{order.Id}</strong> from <strong>{order.CustomerName}</strong></p>
                        <p style='margin: 5px 0 0 0; color: #856404;'>Tracking: {order.TrackingId}</p>
                    </div>

                    <h3>Customer Details:</h3>
                    <table style='width: 100%; margin: 10px 0;'>
                        <tr>
                            <td style='padding: 5px;'><strong>Name:</strong></td>
                            <td style='padding: 5px;'>{order.CustomerName}</td>
                        </tr>
                        <tr>
                            <td style='padding: 5px;'><strong>Email:</strong></td>
                            <td style='padding: 5px;'>{order.Email}</td>
                        </tr>
                        <tr>
                            <td style='padding: 5px;'><strong>Phone:</strong></td>
                            <td style='padding: 5px;'>{order.Phone}</td>
                        </tr>
                        <tr>
                            <td style='padding: 5px;'><strong>Address:</strong></td>
                            <td style='padding: 5px;'>{order.Address}</td>
                        </tr>
                        {(!string.IsNullOrEmpty(order.Landmark) ? $@"
                        <tr>
                            <td style='padding: 5px;'><strong>Landmark:</strong></td>
                            <td style='padding: 5px;'>{order.Landmark}</td>
                        </tr>" : "")}
                        <tr>
                            <td style='padding: 5px;'><strong>Payment:</strong></td>
                            <td style='padding: 5px;'>{order.PaymentMethod}</td>
                        </tr>
                    </table>

                    <h3>Order Items:</h3>
                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                        <thead>
                            <tr style='background: #007bff; color: white;'>
                                <th style='padding: 10px; text-align: left;'>Product</th>
                                <th style='padding: 10px; text-align: center;'>Qty</th>
                                <th style='padding: 10px; text-align: right;'>Price</th>
                            </tr>
                        </thead>
                        <tbody>
                            {itemsHtml}
                        </tbody>
                        <tfoot>
                            <tr style='background: #d4edda; font-weight: bold;'>
                                <td colspan='2' style='padding: 10px; border: 1px solid #ddd;'>Total Amount</td>
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
