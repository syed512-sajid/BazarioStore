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
        private readonly IConfiguration _configuration;

        public CheckoutController(
            ApplicationDbContext context,
            ILogger<CheckoutController> logger,
            IOptions<EmailSettings> emailSettings,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _emailSettings = emailSettings.Value;

            // CRITICAL: Load from environment variables (Railway)
            var envUser = Environment.GetEnvironmentVariable("EMAIL_USER");
            var envPass = Environment.GetEnvironmentVariable("EMAIL_PASS");

            if (!string.IsNullOrEmpty(envUser))
            {
                _emailSettings.SmtpUser = envUser;
                _emailSettings.FromEmail = envUser;
            }

            if (!string.IsNullOrEmpty(envPass))
            {
                _emailSettings.SmtpPass = envPass;
            }

            // Log configuration (without exposing password)
            _logger.LogInformation("Email Config - Host: {Host}, Port: {Port}, User: {User}", 
                _emailSettings.SmtpHost, 
                _emailSettings.SmtpPort, 
                _emailSettings.SmtpUser);
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

            if (!cart.Any()) 
                return Json(new { success = false, message = "Your cart is empty!" });

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

            // CRITICAL FIX: Send emails synchronously or with proper background task
            try
            {
                // Option 1: Synchronous (blocks response but guaranteed delivery)
                await SendCustomerEmailAsync(order, cart);
                await SendAdminEmailAsync(order, cart);
                
                // Option 2: Background task (if you prefer async, use IHostedService instead)
                // _ = Task.Run(async () =>
                // {
                //     try
                //     {
                //         await SendCustomerEmailAsync(order, cart);
                //         await SendAdminEmailAsync(order, cart);
                //     }
                //     catch (Exception ex)
                //     {
                //         _logger.LogError(ex, "Failed to send email for Order ID {OrderId}", order.Id);
                //     }
                // });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send emails for Order ID {OrderId}", order.Id);
                // Don't fail the order if email fails
            }

            HttpContext.Session.Remove("Cart");

            return Json(new { success = true, orderId = order.Id, message = "Order placed successfully." });
        }

        private string GenerateTrackingId()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return $"BAZ{new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray())}";
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

        private async Task SendCustomerEmailAsync(Order order, List<CartItem> cart)
        {
            try
            {
                // Validate email settings
                if (string.IsNullOrEmpty(_emailSettings.SmtpUser) || 
                    string.IsNullOrEmpty(_emailSettings.SmtpPass))
                {
                    _logger.LogWarning("Email credentials not configured. Skipping customer email.");
                    return;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
                message.Subject = $"✅ Order Confirmed - BAZARIO #{order.Id}";

                string body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #28a745;'>Hi {order.CustomerName}, your order #{order.Id} has been received!</h2>
                        <p><strong>Tracking ID:</strong> {order.TrackingId}</p>
                        <p><strong>Order Date:</strong> {order.OrderDate:dd MMM yyyy HH:mm}</p>
                        <hr>
                        <h3>Order Items:</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <thead>
                                <tr style='background: #f8f9fa;'>
                                    <th style='padding: 10px; text-align: left; border: 1px solid #ddd;'>Product</th>
                                    <th style='padding: 10px; text-align: center; border: 1px solid #ddd;'>Qty</th>
                                    <th style='padding: 10px; text-align: right; border: 1px solid #ddd;'>Price</th>
                                </tr>
                            </thead>
                            <tbody>";

                foreach (var item in cart)
                {
                    body += $@"
                                <tr>
                                    <td style='padding: 10px; border: 1px solid #ddd;'>{item.ProductName}</td>
                                    <td style='padding: 10px; text-align: center; border: 1px solid #ddd;'>{item.Quantity}</td>
                                    <td style='padding: 10px; text-align: right; border: 1px solid #ddd;'>Rs. {item.Price:N0}</td>
                                </tr>";
                }

                body += $@"
                            </tbody>
                            <tfoot>
                                <tr style='background: #f8f9fa; font-weight: bold;'>
                                    <td colspan='2' style='padding: 10px; border: 1px solid #ddd;'>Total</td>
                                    <td style='padding: 10px; text-align: right; border: 1px solid #ddd;'>Rs. {order.TotalAmount:N0}</td>
                                </tr>
                            </tfoot>
                        </table>
                        <hr>
                        <p><strong>Delivery Address:</strong><br>{order.Address}</p>
                        <p><strong>Payment Method:</strong> {order.PaymentMethod}</p>
                        <p style='color: #6c757d; font-size: 12px;'>Thank you for shopping with BAZARIO!</p>
                    </div>";

                message.Body = new TextPart("html") { Text = body };

                using var client = new SmtpClient();
                
                // CRITICAL: Proper SSL/TLS handling
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.CheckCertificateRevocation = false;

                _logger.LogInformation("Connecting to SMTP: {Host}:{Port}", _emailSettings.SmtpHost, _emailSettings.SmtpPort);
                
                // Use StartTls for port 587, SslOnConnect for port 465
                var secureSocketOptions = _emailSettings.SmtpPort == 587 
                    ? SecureSocketOptions.StartTls 
                    : SecureSocketOptions.SslOnConnect;

                await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, secureSocketOptions);
                
                _logger.LogInformation("Authenticating with user: {User}", _emailSettings.SmtpUser);
                await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
                
                _logger.LogInformation("Sending customer email to: {Email}", order.Email);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("✅ Customer email sent successfully to {Email}", order.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send customer email to {Email}", order.Email);
                throw;
            }
        }

        private async Task SendAdminEmailAsync(Order order, List<CartItem> cart)
        {
            try
            {
                // Validate email settings
                if (string.IsNullOrEmpty(_emailSettings.SmtpUser) || 
                    string.IsNullOrEmpty(_emailSettings.SmtpPass))
                {
                    _logger.LogWarning("Email credentials not configured. Skipping admin email.");
                    return;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress("Admin", "sajidabbas6024@gmail.com"));
                message.Subject = $"🔔 New Order Received - Order #{order.Id}";

                string body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #007bff;'>New Order #{order.Id} from {order.CustomerName}</h2>
                        <p><strong>Tracking ID:</strong> {order.TrackingId}</p>
                        <p><strong>Order Date:</strong> {order.OrderDate:dd MMM yyyy HH:mm}</p>
                        <hr>
                        <h3>Customer Details:</h3>
                        <p><strong>Name:</strong> {order.CustomerName}</p>
                        <p><strong>Email:</strong> {order.Email}</p>
                        <p><strong>Phone:</strong> {order.Phone}</p>
                        <p><strong>Address:</strong> {order.Address}</p>
                        {(!string.IsNullOrEmpty(order.Landmark) ? $"<p><strong>Landmark:</strong> {order.Landmark}</p>" : "")}
                        <hr>
                        <h3>Order Items:</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <thead>
                                <tr style='background: #f8f9fa;'>
                                    <th style='padding: 10px; text-align: left; border: 1px solid #ddd;'>Product</th>
                                    <th style='padding: 10px; text-align: center; border: 1px solid #ddd;'>Qty</th>
                                    <th style='padding: 10px; text-align: right; border: 1px solid #ddd;'>Price</th>
                                </tr>
                            </thead>
                            <tbody>";

                foreach (var item in cart)
                {
                    body += $@"
                                <tr>
                                    <td style='padding: 10px; border: 1px solid #ddd;'>{item.ProductName}</td>
                                    <td style='padding: 10px; text-align: center; border: 1px solid #ddd;'>{item.Quantity}</td>
                                    <td style='padding: 10px; text-align: right; border: 1px solid #ddd;'>Rs. {item.Price:N0}</td>
                                </tr>";
                }

                body += $@"
                            </tbody>
                            <tfoot>
                                <tr style='background: #f8f9fa; font-weight: bold;'>
                                    <td colspan='2' style='padding: 10px; border: 1px solid #ddd;'>Total</td>
                                    <td style='padding: 10px; text-align: right; border: 1px solid #ddd;'>Rs. {order.TotalAmount:N0}</td>
                                </tr>
                            </tfoot>
                        </table>
                        <hr>
                        <p><strong>Payment Method:</strong> {order.PaymentMethod}</p>
                    </div>";

                message.Body = new TextPart("html") { Text = body };

                using var client = new SmtpClient();
                
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.CheckCertificateRevocation = false;

                var secureSocketOptions = _emailSettings.SmtpPort == 587 
                    ? SecureSocketOptions.StartTls 
                    : SecureSocketOptions.SslOnConnect;

                await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, secureSocketOptions);
                await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("✅ Admin email sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send admin email");
                throw;
            }
        }
    }
}
