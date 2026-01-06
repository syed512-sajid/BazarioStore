using EcommerceStore.Data;
using EcommerceStore.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Newtonsoft.Json;
using MailKit.Security;

namespace EcommerceStore.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(ApplicationDbContext context, ILogger<CheckoutController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Checkout page
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

        // Place Order
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
                {
                    return Json(new { success = false, message = "Your cart is empty!" });
                }

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

                // 🔥 EMAILS IN BACKGROUND (NON-BLOCKING)
                _ = Task.Run(() =>
                {
                    try
                    {
                        SendCustomerOrderEmail(order, cart);
                        SendAdminNotificationEmail(order, cart);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Email failed for Order ID {OrderId}", order.Id);
                    }
                });

                HttpContext.Session.Remove("Cart");

                return Json(new
                {
                    success = true,
                    orderId = order.Id,
                    message = "Order placed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order processing error");
                return Json(new { success = false, message = "Something went wrong." });
            }
        }

        //public async Task<IActionResult> PlaceOrder(string customerName, string email, string address,
        //    string landmark, string phone, string paymentMethod)
        //{
        //    try
        //    {
        //        // Validate input
        //        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(email) ||
        //            string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(phone) ||
        //            string.IsNullOrWhiteSpace(paymentMethod))
        //        {
        //            return Json(new { success = false, message = "All required fields must be filled." });
        //        }

        //        var cartJson = HttpContext.Session.GetString("Cart");
        //        var cart = string.IsNullOrEmpty(cartJson)
        //            ? new List<CartItem>()
        //            : JsonConvert.DeserializeObject<List<CartItem>>(cartJson) ?? new List<CartItem>();

        //        if (!cart.Any())
        //        {
        //            return Json(new { success = false, message = "Your cart is empty!" });
        //        }

        //        // Create order
        //        var order = new Order
        //        {
        //            CustomerName = customerName,
        //            Email = email,
        //            Address = address,
        //            Landmark = landmark ?? "",
        //            Phone = phone,
        //            PaymentMethod = paymentMethod,
        //            OrderDate = DateTime.Now,
        //            TotalAmount = cart.Sum(c => c.Price * c.Quantity),
        //            Status = "Pending",
        //            TrackingId = GenerateTrackingId(),
        //            OrderItems = cart.Select(c => new OrderItem
        //            {
        //                ProductId = c.ProductId,
        //                Quantity = c.Quantity,
        //                Price = c.Price,
        //                Size = c.Size ?? ""
        //            }).ToList()
        //        };

        //        // Save to DB
        //        _context.Orders.Add(order);
        //        await _context.SaveChangesAsync();

        //        // Send Emails
        //        // Send Emails in Background
        //        Task.Run(() =>
        //        {
        //            try
        //            {
        //                SendCustomerOrderEmail(order, cart);
        //                SendAdminNotificationEmail(order, cart);
        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogError(ex, "Email sending failed for Order ID: {OrderId}", order.Id);
        //            }
        //        });

        //        // Clear cart
        //        HttpContext.Session.Remove("Cart");

        //        return Json(new
        //        {
        //            success = true,
        //            orderId = order.Id,
        //            emailSent = emailSent,
        //            message = emailSent
        //                ? "Order placed successfully! Check your email for confirmation."
        //                : $"Order placed successfully! Your Tracking ID is {order.TrackingId}."
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error processing order");
        //        return Json(new { success = false, message = "An error occurred while processing your order." });
        //    }
        //}

        private string GenerateTrackingId()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return $"BAZ{code}";
        }

        // Order Confirmation Page
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

        // TRACKING FUNCTIONALITY
        [HttpGet]
        public async Task<IActionResult> TrackOrder(string trackingId)
        {
            if (string.IsNullOrEmpty(trackingId))
            {
                return View(); // Just show empty form if no trackingId provided
            }

            // Search for order in the database
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.TrackingId == trackingId);

            // Pass order to view (can be null if not found)
            return View(order);
        }

        //public async Task<IActionResult> OrderConfirmation(int id)
        //{
        //    var order = await _context.Orders
        //        .Include(o => o.OrderItems)
        //        .ThenInclude(oi => oi.Product)
        //        .FirstOrDefaultAsync(o => o.Id == id);

        //    if (order == null)
        //        return NotFound();

        //    return View(order);
        //}

        // Email to Customer - Simple Plain Text
        private void SendCustomerOrderEmail(Order order, List<CartItem> cart)
        {
            try
            {
                var emailUser = Environment.GetEnvironmentVariable("EMAIL_USER");
                var emailPass = Environment.GetEnvironmentVariable("EMAIL_PASS");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("BAZARIO Store", emailUser));
                message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
                message.Subject = "✅ Order Confirmed - BAZARIO";

                string body = $@"
<div style='font-family: Arial, sans-serif; max-width: 800px; margin: auto; background: #f5f5f5; padding: 20px;'>

    <div style='background: linear-gradient(135deg, #28a745, #20c997); color: white; padding: 30px; border-radius: 10px 10px 0 0; text-align: center;'>
        <h1 style='margin: 0;'>🎉 Order Confirmed!</h1>
        <p style='margin-top: 10px;'>Thank you for shopping with <strong>BAZARIO</strong></p>
    </div>

    <div style='background: white; padding: 30px; border-radius: 0 0 10px 10px;'>

        <p style='font-size: 16px;'>Hi <strong>{order.CustomerName}</strong>,</p>
        <p>Your order has been placed successfully. Below are your order details:</p>

        <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
            <h3 style='color: #28a745;'>📄 Order Summary</h3>
            <table style='width:100%;'>
<tr><td><strong>Tracking ID:</strong></td><td>{order.TrackingId}</td></tr>

                <tr><td><strong>Order ID:</strong></td><td>#{order.Id}</td></tr>
                <tr><td><strong>Order Date:</strong></td><td>{order.OrderDate:dd/MM/yyyy hh:mm tt}</td></tr>
                <tr><td><strong>Payment Method:</strong></td><td>{order.PaymentMethod}</td></tr>
                <tr>
                    <td><strong>Total Amount:</strong></td>
                    <td style='color:#28a745; font-weight:bold;'>Rs. {order.TotalAmount:N0}</td>
                </tr>
            </table>
        </div>

        <div style='background: #e7f3ff; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
            <h3 style='color: #004085;'>📦 Items Ordered</h3>
            <table style='width:100%; border-collapse: collapse;'>
                <thead>
                    <tr style='background:#28a745; color:white;'>
                        <th style='padding:10px; text-align:left;'>Product</th>
                        <th style='padding:10px; text-align:center;'>Size</th>
                        <th style='padding:10px; text-align:center;'>Qty</th>
                        <th style='padding:10px; text-align:right;'>Price</th>
                    </tr>
                </thead>
                <tbody>";

                foreach (var item in cart)
                {
                    body += $@"
                    <tr style='border-bottom:1px solid #ddd;'>
                        <td style='padding:10px;'>{item.ProductName}</td>
                        <td style='padding:10px; text-align:center;'>{(string.IsNullOrEmpty(item.Size) ? "N/A" : item.Size)}</td>
                        <td style='padding:10px; text-align:center;'>{item.Quantity}</td>
                        <td style='padding:10px; text-align:right; color:#28a745;'>
                            Rs. {(item.Price * item.Quantity):N0}
                        </td>
                    </tr>";
                }

                body += $@"
                </tbody>
            </table>
        </div>

        <div style='background: #fff3cd; padding: 20px; border-radius: 8px;'>
            <h3 style='color:#856404;'>🚚 Delivery Information</h3>
            <p><strong>Address:</strong> {order.Address}</p>
            <p><strong>Landmark:</strong> {(string.IsNullOrEmpty(order.Landmark) ? "N/A" : order.Landmark)}</p>
            <p><strong>Phone:</strong> {order.Phone}</p>
            <p><strong>Estimated Delivery:</strong> 3–5 Business Days</p>
        </div>

        <div style='margin-top: 30px; text-align: center;'>
            <p style='font-size:16px;'>❤️ Thank you for choosing <strong>BAZARIO</strong></p>
            <p style='color:#666;'>For any queries, feel free to contact us.</p>
        </div>

    </div>

    <div style='text-align:center; color:#999; font-size:12px; margin-top:20px;'>
        <p>© {DateTime.Now.Year} BAZARIO Store. All rights reserved.</p>
    </div>

</div>";

             
                message.Body = new TextPart("html") { Text = body };

                using var client = new MailKit.Net.Smtp.SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Connect("smtp.gmail.com", 465, MailKit.Security.SecureSocketOptions.SslOnConnect);
                client.Authenticate(emailUser, emailPass);
                client.Send(message);
                client.Disconnect(true);

                _logger.LogInformation("Customer email sent successfully to {Email}", order.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending customer email: {Message}", ex.Message);
            }
        }

        // Email to Admin - Rich HTML
        private void SendAdminNotificationEmail(Order order, List<CartItem> cart)
        {
            try
            {
                var emailUser = Environment.GetEnvironmentVariable("EMAIL_USER");
                var emailPass = Environment.GetEnvironmentVariable("EMAIL_PASS");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("BAZARIO Store", emailUser));
                message.To.Add(new MailboxAddress("Admin", "sajidabbas6024@gmail.com")); // Admin hardcoded
                message.Subject = $"🔔 New Order Received - Order #{order.Id}";

                string body = $@"
<div style='font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; background-color: #f5f5f5;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='margin: 0; font-size: 28px;'>🛍️ New Order Alert!</h1>
        <p style='margin: 10px 0 0 0; font-size: 16px;'>Order #{order.Id} has been placed</p>
    </div>
    
    <div style='background: white; padding: 30px; border-radius: 0 0 10px 10px;'>
        <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
            <h2 style='color: #667eea; margin-top: 0; font-size: 20px;'>📋 Order Information</h2>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 8px 0; color: #666; width: 40%;'><strong>Order ID:</strong></td>
                    <td style='padding: 8px 0; color: #333;'>#{order.Id}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #666;'><strong>Order Date:</strong></td>
                    <td style='padding: 8px 0; color: #333;'>{order.OrderDate:dd/MM/yyyy hh:mm tt}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #666;'><strong>Total Amount:</strong></td>
                    <td style='padding: 8px 0; color: #28a745; font-size: 18px; font-weight: bold;'>Rs. {order.TotalAmount:N0}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #666;'><strong>Payment Method:</strong></td>
                    <td style='padding: 8px 0; color: #333;'>{order.PaymentMethod}</td>
                </tr>
            </table>
        </div>

        <div style='background: #fff3cd; padding: 20px; border-radius: 8px; margin-bottom: 20px; border-left: 4px solid #ffc107;'>
            <h2 style='color: #856404; margin-top: 0; font-size: 20px;'>👤 Customer Details</h2>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 8px 0; color: #856404; width: 40%;'><strong>Name:</strong></td>
                    <td style='padding: 8px 0; color: #856404;'>{order.CustomerName}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #856404;'><strong>Email:</strong></td>
                    <td style='padding: 8px 0; color: #856404;'>{order.Email}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #856404;'><strong>Phone:</strong></td>
                    <td style='padding: 8px 0; color: #856404;'>{order.Phone}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #856404; vertical-align: top;'><strong>Address:</strong></td>
                    <td style='padding: 8px 0; color: #856404;'>{order.Address}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #856404;'><strong>Landmark:</strong></td>
                    <td style='padding: 8px 0; color: #856404;'>{(string.IsNullOrEmpty(order.Landmark) ? "N/A" : order.Landmark)}</td>
                </tr>
            </table>
        </div>

        <div style='background: #e7f3ff; padding: 20px; border-radius: 8px; margin-bottom: 20px; border-left: 4px solid #007bff;'>
            <h2 style='color: #004085; margin-top: 0; font-size: 20px;'>📦 Order Items</h2>
            <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
                <thead>
                    <tr style='background: #667eea; color: white;'>
                        <th style='padding: 12px; text-align: left; border-radius: 5px 0 0 5px;'>Product</th>
                        <th style='padding: 12px; text-align: center;'>Size</th>
                        <th style='padding: 12px; text-align: center;'>Qty</th>
                        <th style='padding: 12px; text-align: right; border-radius: 0 5px 5px 0;'>Price</th>
                    </tr>
                </thead>
                <tbody>";

                foreach (var item in cart)
                {
                    body += $@"
                    <tr style='border-bottom: 1px solid #ddd;'>
                        <td style='padding: 12px; color: #333;'><strong>{item.ProductName}</strong></td>
                        <td style='padding: 12px; text-align: center; color: #666;'>{(string.IsNullOrEmpty(item.Size) ? "N/A" : item.Size)}</td>
                        <td style='padding: 12px; text-align: center; color: #666;'>{item.Quantity}</td>
                        <td style='padding: 12px; text-align: right; color: #28a745; font-weight: bold;'>Rs. {(item.Price * item.Quantity):N0}</td>
                    </tr>";
                }

                body += $@"
                </tbody>
            </table>
        </div>

        <div style='background: #d4edda; padding: 20px; border-radius: 8px; text-align: center; border-left: 4px solid #28a745;'>
            <h3 style='color: #155724; margin-top: 0;'>Total Order Value</h3>
            <p style='font-size: 32px; font-weight: bold; color: #28a745; margin: 10px 0;'>Rs. {order.TotalAmount:N0}</p>
        </div>

        <div style='margin-top: 30px; padding: 20px; background: #f8f9fa; border-radius: 8px; text-align: center;'>
            <p style='color: #666; margin: 0 0 15px 0;'>Please check the website's Order Management section for complete details and to process this order.</p>
        </div>
    </div>

    <div style='text-align: center; margin-top: 20px; color: #999; font-size: 12px;'>
        <p>This is an automated notification from BAZARIO Store</p>
        <p>© {DateTime.Now.Year} BAZARIO. All rights reserved.</p>
    </div>
</div>";

                message.Body = new TextPart("html") { Text = body };

                using var client = new MailKit.Net.Smtp.SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Connect("smtp.gmail.com", 465, MailKit.Security.SecureSocketOptions.SslOnConnect);
                client.Authenticate(emailUser, emailPass);
                client.Send(message);
                client.Disconnect(true);

                _logger.LogInformation("Admin notification email sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending admin email: {Message}", ex.Message);
            }
        }
    }
}