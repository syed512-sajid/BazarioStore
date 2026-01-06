using EcommerceStore.Data;
using EcommerceStore.Models;
using EcommerceStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EcommerceStore.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CheckoutController> _logger;
        private readonly IEmailService _emailService;

        public CheckoutController(
            ApplicationDbContext context,
            ILogger<CheckoutController> logger,
            IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
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
            try
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

                _logger.LogInformation("✅ Order #{OrderId} saved to database", order.Id);

                // ⚡ DIRECT EMAIL SEND (No background service)
                try
                {
                    _logger.LogInformation("📧 Sending emails directly for Order #{OrderId}", order.Id);
                    
                    await _emailService.SendOrderConfirmationAsync(order, cart);
                    _logger.LogInformation("✅ Customer email sent for Order #{OrderId}", order.Id);
                    
                    await _emailService.SendAdminNotificationAsync(order, cart);
                    _logger.LogInformation("✅ Admin email sent for Order #{OrderId}", order.Id);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "❌ Email sending failed for Order #{OrderId}", order.Id);
                    // Don't fail the order if email fails
                }

                HttpContext.Session.Remove("Cart");

                return Json(new { success = true, orderId = order.Id, message = "Order placed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error placing order");
                return Json(new { success = false, message = "An error occurred while placing your order." });
            }
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
    }
}
