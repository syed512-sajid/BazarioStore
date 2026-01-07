using EcommerceStore.Models;
using Newtonsoft.Json;
using System.Text;

namespace EcommerceStore.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly HttpClient _http;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
            _http = new HttpClient();

            var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
                _logger.LogWarning("Resend API Key not set in environment variables.");

            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            _logger.LogInformation("📧 EmailService initialized using Resend");
        }

        public async Task SendOrderConfirmationAsync(Order order, List<CartItem> cart)
        {
            await SendEmail(order.Email, $"✅ Order Confirmed #{order.Id} - BAZARIO", BuildCustomerEmailBody(order, cart));
        }

        public async Task SendAdminNotificationAsync(Order order, List<CartItem> cart)
        {
            await SendEmail("sajidabbas6024@gmail.com", $"🔔 New Order #{order.Id}", BuildAdminEmailBody(order, cart));
        }

        private async Task SendEmail(string to, string subject, string html)
        {
            var payload = new
            {
                from = "BAZARIO Store <onboarding@resend.dev>",
                to = new[] { to },
                subject = subject,
                html = html
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("https://api.resend.com/emails", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ Resend failed: {Body}", body);
                throw new Exception("Email failed");
            }

            _logger.LogInformation("✅ Email sent to {Email} via Resend", to);
        }

        private string BuildCustomerEmailBody(Order order, List<CartItem> cart)
        {
            return $"<h2>Order #{order.Id}</h2><p>Thank you for shopping with BAZARIO!</p>";
        }

        private string BuildAdminEmailBody(Order order, List<CartItem> cart)
        {
            return $"<h2>New Order #{order.Id}</h2><p>Check admin panel for details.</p>";
        }
    }
}
