using EcommerceStore.Models;
using Newtonsoft.Json;
using System.Text;

namespace EcommerceStore.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public EmailService(
            ILogger<EmailService> logger,
            IConfiguration config)
        {
            _logger = logger;
            _config = config;
            _http = new HttpClient();

            var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public async Task SendOrderConfirmationAsync(Order order, List<CartItem> cart)
        {
            await SendEmail(
                order.Email,
                $"✅ Order Confirmed #{order.Id} - BAZARIO",
                BuildCustomerEmailBody(order, cart)
            );
        }

        public async Task SendAdminNotificationAsync(Order order, List<CartItem> cart)
        {
            await SendEmail(
                "sajidabbas6024@gmail.com",
                $"🔔 New Order #{order.Id}",
                BuildAdminEmailBody(order, cart)
            );
        }

        private async Task SendEmail(string to, string subject, string html)
        {
            var fromEmail = _config["Resend:FromEmail"];
            var fromName = _config["Resend:FromName"];

            var payload = new
            {
                from = $"{fromName} <{fromEmail}>",
                to = new[] { to },
                subject = subject,
                html = html
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.PostAsync("https://api.resend.com/emails", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ Resend failed: {Body}", body);
                throw new Exception("Email failed");
            }

            _logger.LogInformation("✅ Email sent to {Email}", to);
        }

        // 👇 SAME HTML METHODS (unchanged)
        private string BuildCustomerEmailBody(Order order, List<CartItem> cart) => $"<h2>Order #{order.Id}</h2>";
        private string BuildAdminEmailBody(Order order, List<CartItem> cart) => $"<h2>New Order #{order.Id}</h2>";
    }
}
