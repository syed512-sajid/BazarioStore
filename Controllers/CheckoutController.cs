using EcommerceStore.Data;
using EcommerceStore.Models;
using EcommerceStore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EcommerceStore.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CheckoutController> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public CheckoutController(
            ApplicationDbContext context,
            ILogger<CheckoutController> logger,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
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
                // 1️⃣ Validate required fields
                if (string.IsNullOrWhiteSpace(customerName) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(address) ||
                    string.IsNullOrWhiteSpace(phone) ||
                    string.IsNullOrWhiteSpace(paymentMethod))
                {
                    return Json(new { success = false, message = "All required fields must be filled." });
                }

                // 2️⃣ Load cart from session
                var cartJson = HttpContext.Session.GetString("Cart");
                var cart = string.IsNullOrEmpty(cartJson)
                    ? new List<CartItem>()
                    : JsonConvert.DeserializeObject<List<CartItem>>(cartJson) ?? new List<CartItem>();

                if (!cart.Any())
                    return Json(new { success = false, message = "Your cart is empty!" });

                // 3️⃣ Validate that all products exist in database
                var productIds = cart.Select(c => c.ProductId).ToList();
                var validProducts = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                // Remove invalid products
                var validCartItems = cart
                    .Where(c => validProducts.Any(p => p.Id == c.ProductId))
                    .ToList();

                if (!validCartItems.Any())
                    return Json(new { success = false, message = "No valid products found in your cart." });

                // Update session cart to remove invalid items
                HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(validCartItems));

                // 4️⃣ Get or Create Customer
                Customer customer = null;

                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = _userManager.GetUserId(User);
                    customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.UserId == userId);

                    if (customer == null)
                    {
                        // Create customer if doesn't exist
                        customer = new Customer
                        {
                            UserId = userId,
                            Name = customerName,
                            Email = email,
                            Phone = phone,
                            Address = address
                        };
                        _context.Customers.Add(customer);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Guest user - create new customer
                    customer = new Customer
                    {
                        Name = customerName,
                        Email = email,
                        Phone = phone,
                        Address = address
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                // 5️⃣ Create Order
                var order = new Order
                {
                    CustomerId = customer.Id,
                    CustomerName = customerName,
                    Email = email,
                    Address = address,
                    Landmark = landmark ?? "",
                    Phone = phone,
                    PaymentMethod = paymentMethod,
                    OrderDate = DateTime.Now,
                    TotalAmount = validCartItems.Sum(c => c.Price * c.Quantity),
                    Status = "Pending",
                    TrackingId = GenerateTrackingId(),
                    OrderItems = validCartItems.Select(c => new OrderItem
                    {
                        ProductId = c.ProductId,
                        Quantity = c.Quantity,
                        Price = c.Price,
                        Size = c.Size ?? ""
                    }).ToList()
                };

                // 6️⃣ Save Order and related OrderItems
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Order #{OrderId} saved to database", order.Id);

                // 7️⃣ Queue email for background processing (non-blocking)
                BackgroundEmailService.QueueEmail(order, validCartItems);
                _logger.LogInformation("📧 Email queued for Order #{OrderId}", order.Id);

                // 8️⃣ Clear cart session
                HttpContext.Session.Remove("Cart");

                return Json(new { success = true, orderId = order.Id, message = "Order placed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error placing order");
                return Json(new { success = false, message = "An error occurred while placing your order. Please try again." });
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