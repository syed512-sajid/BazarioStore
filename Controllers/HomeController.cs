using EcommerceStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceStore.Data;
using Newtonsoft.Json;

namespace EcommerceStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Home page - show all products
        public async Task<IActionResult> Index(string category)
        {
            var productsQuery = _context.Products.Include(p => p.Images).AsQueryable();

            if (!string.IsNullOrEmpty(category) && category != "All")
                productsQuery = productsQuery.Where(p => p.Category == category);

            var products = await productsQuery.ToListAsync();

            ViewBag.SelectedCategory = string.IsNullOrEmpty(category) ? "All" : category;
            return View(products);
        }

        // About page
        public IActionResult About()
        {
            return View();
        }

        // Product details page
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // Add product to cart
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return NotFound();

            if (quantity > product.Stock)
            {
                TempData["Error"] = $"Only {product.Stock} items available in stock";
                return RedirectToAction("Details", new { id = productId });
            }

            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(cartJson) ?? new List<CartItem>();

            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);

            if (existingItem != null)
            {
                if (existingItem.Quantity + quantity > product.Stock)
                {
                    TempData["Error"] = $"Only {product.Stock} items available in stock";
                    return RedirectToAction("Cart");
                }

                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    Stock = product.Stock,
                    ImageUrls = product.Images?.Select(i => i.ImageUrl).ToList()
                        ?? new List<string> { "/images/no-image.jpg" }
                });
            }

            HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
            TempData["Success"] = "Product added to cart!";
            return RedirectToAction("Cart");
        }

        // Show cart page
        public IActionResult Cart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(cartJson) ?? new List<CartItem>();

            ViewBag.Total = cart.Sum(c => c.Price * c.Quantity);
            return View(cart);
        }

        // Remove item from cart
        public IActionResult RemoveFromCart(int productId)
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (!string.IsNullOrEmpty(cartJson))
            {
                var cart = JsonConvert.DeserializeObject<List<CartItem>>(cartJson) ?? new List<CartItem>();
                cart.RemoveAll(c => c.ProductId == productId);
                HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
            }

            return RedirectToAction("Cart");
        }

        // Update cart quantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
                return RedirectToAction("Cart");

            if (quantity > product.Stock)
            {
                TempData["Error"] = $"Only {product.Stock} items available in stock";
                return RedirectToAction("Cart");
            }

            var cartJson = HttpContext.Session.GetString("Cart");
            if (!string.IsNullOrEmpty(cartJson))
            {
                var cart = JsonConvert.DeserializeObject<List<CartItem>>(cartJson) ?? new List<CartItem>();
                var item = cart.FirstOrDefault(c => c.ProductId == productId);

                if (item != null)
                {
                    item.Quantity = quantity < 1 ? 1 : quantity;
                    HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
                }
            }

            return RedirectToAction("Cart");
        }
        public IActionResult Checkout()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonConvert.DeserializeObject<List<CartItem>>(cartJson) ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Cart");
            }

            ViewBag.Total = cart.Sum(c => c.Price * c.Quantity);
            return View(cart); // Checkout.cshtml
        }

      

        // Use CheckoutController for all checkout/order-related actions to avoid duplicates
    }
}
