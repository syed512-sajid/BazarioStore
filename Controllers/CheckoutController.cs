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

        // ============================
        // CHECKOUT PAGE
        // ============================
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

        // ============================
        // PLACE ORDER (FINAL FIX)
        // ============================
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
                _logger.LogInformation("📝 PlaceOrder started for {Customer}", customerName);

                // ----------------------------
                // VALIDATION
                // ----------------------------
                if (string.IsNullOrWhiteSpace(customerName) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(address) ||
                    string.IsNullOrWhiteSpace(phone) ||
                    string.IsNullOrWhiteSpace(paymentMethod))
                {
                    return Json(new
                    {
                        success = false,
                        message = "All required fields must be filled."
                    });
                }

                // ----------------------------
                // CART FROM SESSION
                // ----------------------------
                var cartJson = HttpContext.Session.GetString("Cart");
                var cart = string.IsNullOrEmpty(cartJson)
                    ? new List<CartItem>()
                    : JsonConvert.DeserializeObject<List<CartItem>>(cartJson) ?? new List<CartItem>();

                if (!cart.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Your cart is empty!"
                    });
                }

                // ----------------------------
                // CREATE ORDER
                // ----------------------------
                var order = new Order
                {
                    CustomerName = customerName,
                    Email = email,
                    Address = address,
                    Landmark = landmark ?? "",
                    Phone = phone,
                    PaymentMethod = paymentMethod,
                    OrderDate = DateTime.UtcNow,
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

                _logger.LogInformation("✅ Order #{OrderId} saved successfully", order.Id);

                // ----------------------------
                // SEND EMAILS (NO Task.Run)
                // ----------------------------
                try
                {
                    _logger.LogInformation("📧 Sending emails for Order #{OrderId}", order.Id);

                    await _emailService.SendOrderConfirmationAsync(order, cart);
                    await _emailService.SendAdminNotificationAsync(order, cart);

                    _logger.LogInformation("✅ Emails sent for Order #{OrderId}", order.Id);
                }
                catch (Exception mailEx)
                {
                    _logger.LogError(mailEx, "❌ Email sending failed for Order #{OrderId}", order.Id);
                }

                // ----------------------------
                // CLEAR CART
                // ----------------------------
                HttpContext.Session.Remove("Cart");

                return Json(new
                {
                    success = true,
                    orderId = order.Id,
                    message = "Order placed successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ PlaceOrder failed");

                return Json(new
                {
                    success = false,
                    message = "An error occurred while placing your order."
                });
            }
        }

        // ============================
        // ORDER CONFIRMATION PAGE
        // ============================
        public async Task<IActionResult> OrderConfirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // ============================
        // TRACK ORDER
        // ============================
        [HttpGet]
        public async Task<IActionResult> TrackOrder(string trackingId)
        {
            if (string.IsNullOrWhiteSpace(trackingId))
                return View();

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.TrackingId == trackingId);

            return View(order);
        }

        // ============================
        // HELPER
        // ============================
        private string GenerateTrackingId()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            return "BAZ" + new string(
                Enumerable.Repeat(chars, 8)
                    .Select(s => s[random.Next(s.Length)])
                    .ToArray()
            );
        }
    }
}
