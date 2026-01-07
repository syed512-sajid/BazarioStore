using EcommerceStore.Models;
using Newtonsoft.Json;
using System.Text;

namespace EcommerceStore.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly HttpClient _http;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(ILogger<EmailService> logger, IConfiguration config)
        {
            _logger = logger;

            var apiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("❌ RESEND_API_KEY not found in environment variables");

            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            _fromEmail = config["Resend:FromEmail"] ?? "onboarding@resend.dev";
            _fromName  = config["Resend:FromName"]  ?? "BAZARIO Store";
        }

        public Task SendOrderConfirmationAsync(Order order, List<CartItem> cart)
            => Send(order.Email,
                $"✅ Order Confirmed #{order.Id}",
                BuildCustomerEmailBody(order));

        public Task SendAdminNotificationAsync(Order order, List<CartItem> cart)
            => Send("sajidabbas6024@gmail.com",
                $"🔔 New Order #{order.Id}",
                BuildAdminEmailBody(order));

        private async Task Send(string to, string subject, string html)
        {
            var payload = new
            {
                from = $"{_fromName} <{_fromEmail}>",
                to = new[] { to },
                subject,
                html
            };

            var response = await _http.PostAsync(
                "https://api.resend.com/emails",
                new StringContent(JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"));

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ Resend error: {Body}", body);
                throw new Exception("Email sending failed");
            }

            _logger.LogInformation("✅ Email sent to {Email}", to);
        }

        private string BuildCustomerEmailBody(Order o)
            => $"<h2>Order Confirmed #{o.Id}</h2><p>Tracking ID: {o.TrackingId}</p>";

        private string BuildAdminEmailBody(Order o)
            => $"<h2>New Order #{o.Id}</h2><p>{o.CustomerName}</p>";
    }
}
