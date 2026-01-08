using EcommerceStore.Data;
using EcommerceStore.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceStore.Controllers
{
    public class UserAuthController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserAuthController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Signup(string name, string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match";
                return View();
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                TempData["Error"] = "Email already registered";
                return View();
            }

            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
                return View();
            }

            await _userManager.AddToRoleAsync(user, "Customer");

            var customer = new Customer
            {
                Name = name,
                Email = email,
                UserId = user.Id
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["Success"] = "Account created successfully!";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Signin(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Signin(string email, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["Error"] = "Invalid email or password";
                return View();
            }

            var isCustomer = await _userManager.IsInRoleAsync(user, "Customer");
            if (!isCustomer)
            {
                TempData["Error"] = "Invalid credentials";
                return View();
            }

            var result = await _signInManager.CheckPasswordSignInAsync(
     user, password, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                TempData["Error"] = "Invalid email or password";
                return View();
            }

            // 🔹 Customer table se name uthao
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id),
    new Claim(ClaimTypes.Name, user.Email), // email
    new Claim("FullName", customer?.Name ?? "Customer"),
    new Claim(ClaimTypes.Role, "Customer")
};

            var identity = new ClaimsIdentity(
                claims, IdentityConstants.ApplicationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                principal);

            return RedirectToAction("Index", "Home");

        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                return RedirectToAction("Index", "Home");

            var orders = await _context.Orders
                .Where(o => o.CustomerId == customer.Id)
                .ToListAsync();

            ViewBag.CustomerName = customer.Name;
            ViewBag.CustomerEmail = customer.Email;
            ViewBag.CustomerPhone = customer.Phone ?? "Not provided";
            ViewBag.CustomerAddress = customer.Address ?? "Not provided";
            ViewBag.TotalOrders = orders.Count;
            ViewBag.CompletedOrders = orders.Count(o => o.Status == "Delivered");
            ViewBag.PendingOrders = orders.Count(o => o.Status == "Pending" || o.Status == "Processing");

            return View();
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                return RedirectToAction("Index", "Home");

            var orders = await _context.Orders
                .Where(o => o.CustomerId == customer.Id)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.CustomerName = customer.Name;
            return View(orders);
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                return NotFound();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customer.Id);

            if (order == null)
                return NotFound();

            ViewBag.CustomerName = customer.Name;
            return View(order);
        }

        // ✅ FIXED: Profile Update with proper validation
        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string name, string phone, string address)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    TempData["Error"] = "Customer profile not found";
                    return RedirectToAction("Dashboard");
                }

                // Update only changed fields
                if (!string.IsNullOrWhiteSpace(name))
                    customer.Name = name.Trim();

                if (!string.IsNullOrWhiteSpace(phone))
                    customer.Phone = phone.Trim();

                if (!string.IsNullOrWhiteSpace(address))
                    customer.Address = address.Trim();

                // Save changes
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating your profile. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }
    }
}