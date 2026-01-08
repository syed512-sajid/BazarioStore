using EcommerceStore.Data;
using EcommerceStore.Models;
using EcommerceStore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _emailService = emailService;
        }

        // =========================
        // LOGIN PAGE
        // =========================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // =========================
        // LOGIN (EMAIL + PASSWORD)
        // =========================
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Unauthorized access";
                return View();
            }

            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, password, false);
            if (!passwordCheck.Succeeded)
            {
                TempData["Error"] = "Invalid email or password";
                return View();
            }

            // 🔐 Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Delete old OTPs for this email
            var oldOtps = _context.EmailOtps.Where(x => x.Email == email);
            _context.EmailOtps.RemoveRange(oldOtps);

            _context.EmailOtps.Add(new EmailOtp
            {
                Email = email,
                Otp = otp,
                ExpiryTime = DateTime.Now.AddMinutes(5)
            });
            await _context.SaveChangesAsync();

            // ✅ Send OTP via Email
            await _emailService.SendOtpAsync(email, otp);

            TempData["Email"] = email;
            TempData["ReturnUrl"] = returnUrl;
            TempData["Success"] = "OTP sent to your email";

            return RedirectToAction("VerifyOtp");
        }

        // =========================
        // OTP PAGE
        // =========================
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            if (TempData["Email"] == null)
                return RedirectToAction("Login");

            // Keep email in TempData for next request
            TempData.Keep("Email");
            TempData.Keep("ReturnUrl");
            return View();
        }

        // =========================
        // OTP VERIFY
        // =========================
        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string otp)
        {
            var email = TempData["Email"]?.ToString();
            var returnUrl = TempData["ReturnUrl"]?.ToString();

            if (email == null)
                return RedirectToAction("Login");

            var record = _context.EmailOtps
                .Where(x => x.Email == email && x.Otp == otp && x.ExpiryTime > DateTime.Now)
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            if (record == null)
            {
                TempData["Error"] = "Invalid or expired OTP";
                TempData["Email"] = email;
                TempData["ReturnUrl"] = returnUrl;
                return RedirectToAction("VerifyOtp");
            }

            var user = await _userManager.FindByEmailAsync(email);
            await _signInManager.SignInAsync(user, false);

            // Delete used OTP
            _context.EmailOtps.Remove(record);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Admin");
        }

        // =========================
        // RESEND OTP
        // =========================
        [HttpGet]
        public async Task<IActionResult> ResendOtp()
        {
            var email = TempData["Email"]?.ToString();
            var returnUrl = TempData["ReturnUrl"]?.ToString();

            if (email == null)
                return RedirectToAction("Login");

            // Generate new OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Delete old OTPs for this email
            var oldOtps = _context.EmailOtps.Where(x => x.Email == email);
            _context.EmailOtps.RemoveRange(oldOtps);

            // Add new OTP
            _context.EmailOtps.Add(new EmailOtp
            {
                Email = email,
                Otp = otp,
                ExpiryTime = DateTime.Now.AddMinutes(5)
            });
            await _context.SaveChangesAsync();

            // Send new OTP
            await _emailService.SendOtpAsync(email, otp);

            TempData["Email"] = email;
            TempData["ReturnUrl"] = returnUrl;
            TempData["Success"] = "New OTP sent to your email";

            return RedirectToAction("VerifyOtp");
        }

        // =========================
        // LOGOUT
        // =========================
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}