using EcommerceStore.Data;
using EcommerceStore.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json;

namespace EcommerceStore.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CheckoutController> _logger;
        private readonly EmailSettings _emailSettings;

        public CheckoutController(
            ApplicationDbContext context,
            ILogger<CheckoutController> logger,
            IOptions<EmailSettings> emailSettings)
        {
            _context = context;
            _logger = logger;
            _emailSettings = emailSettings.Value;

            // Override SMTP user/pass from environment variables (Railway)
            _emailSettings.SmtpUser = Environment.GetEnvironmentVariable("EMAIL_USER") ?? _emailSettings.SmtpUser;
            _emailSettings.SmtpPass = Environment.GetEnvironmentVariable("EMAIL_PASS") ?? _emailSettings.SmtpPass;
            _emailSettings.FromEmail = _emailSettings.SmtpUser;
        }

        public IActionResult Index()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(cartJson) ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Cart", "Home");
            }

            ViewBag.Total = cart.Sum(c => c.Price * c.Quantity);
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            string customerName,
            string email,
            string address,
            string landmark,
            string phone,
            string paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(customerName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(address) ||
                string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(paymentMethod))
            {
                return Json(new { success = false, message = "All required fields must be filled." });
            }

            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(cartJson) ?? new List<CartItem>();

            if (!cart.Any()) return Json(new { success = false, message = "Your cart is empty!" });

            var order = new Order
            {
                CustomerName = customerName,
                Email = email,
                Address = address,
                Landmark = landmark ?? "",
                Phone = phone,
                PaymentMethod = paymentMethod,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(c => c.Price * c.Quantity),
                Status = "Pending",
                TrackingId = GenerateTrackingId(),
                OrderItems = cart.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    Price = c.Price,
                    Size = c.Size ?? ""
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _ = Task.Run(() =>
            {
                try
                {
                    SendCustomerEmail(order, cart);
                    SendAdminEmail(order, cart);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email for Order ID {OrderId}", order.Id);
                }
            });

            HttpContext.Session.Remove("Cart");

            return Json(new { success = true, orderId = order.Id, message = "Order placed successfully." });
        }

        private string GenerateTrackingId()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return $"BAZ{new string(Enumerable.Repeat(chars, 8).Select(s => s[new Random().Next(s.Length)]).ToArray())}";
        }

        public async Task<IActionResult> OrderConfirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            return order == null ? NotFound() : View(order);
        }

        [HttpGet]
        public async Task<IActionResult> TrackOrder(string trackingId)
        {
            if (string.IsNullOrEmpty(trackingId)) return View();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.TrackingId == trackingId);
            return View(order);
        }

        // =============================== EMAILS ===============================

        private void SendCustomerEmail(Order order, List<CartItem> cart)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
            message.Subject = $"✅ Order Confirmed - BAZARIO #{order.Id}";

            string body = $"<h2>Hi {order.CustomerName}, your order #{order.Id} has been received!</h2>";
            foreach (var item in cart)
                body += $"<p>{item.ProductName} - Qty: {item.Quantity} - Price: {item.Price:N0}</p>";

            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.Connect(_emailSettings.SmtpHost, _emailSettings.SmtpPort, SecureSocketOptions.SslOnConnect);
            client.Authenticate(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
            client.Send(message);
            client.Disconnect(true);

            _logger.LogInformation("Customer email sent to {Email}", order.Email);
        }

        private void SendAdminEmail(Order order, List<CartItem> cart)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(new MailboxAddress("Admin", "sajidabbas6024@gmail.com"));
            message.Subject = $"🔔 New Order Received - Order #{order.Id}";

            string body = $"<h2>New order #{order.Id} from {order.CustomerName}</h2>";
            foreach (var item in cart)
                body += $"<p>{item.ProductName} - Qty: {item.Quantity} - Price: {item.Price:N0}</p>";

            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.Connect(_emailSettings.SmtpHost, _emailSettings.SmtpPort, SecureSocketOptions.SslOnConnect);
            client.Authenticate(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
            client.Send(message);
            client.Disconnect(true);

            _logger.LogInformation("Admin email sent");
        }
    }
}
