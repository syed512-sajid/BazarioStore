//using EcommerceStore.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using EcommerceStore.Data;
//using Microsoft.AspNetCore.Authorization;
//using MailKit.Security;
//using MimeKit;
//using MailKit.Net.Smtp;


//namespace EcommerceStore.Controllers
//{
//    [Authorize(Roles = "Admin")]
//    public class AdminController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IWebHostEnvironment _environment;
//        private readonly ILogger<AdminController> _logger;

//        public AdminController(
//     ApplicationDbContext context,
//     IWebHostEnvironment environment,
//     ILogger<AdminController> logger)
//        {
//            _context = context;
//            _environment = environment;
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }


//        public async Task<IActionResult> Index()
//        {
//            var products = await _context.Products
//                .Include(p => p.Images)
//                .OrderByDescending(p => p.CreatedAt)
//                .ToListAsync();
//            return View(products);
//        }

//        public IActionResult AddProduct()
//        {
//            return View();
//        }

//        [HttpPost]
//        public async Task<IActionResult> AddProduct(Product product, List<IFormFile>? images)
//        {
//            product.CreatedAt = DateTime.Now;
//            _context.Products.Add(product);
//            await _context.SaveChangesAsync();

//            if (images != null && images.Count > 0)
//            {
//                foreach (var image in images)
//                {
//                    var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
//                    var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);

//                    using (var stream = new FileStream(filePath, FileMode.Create))
//                    {
//                        await image.CopyToAsync(stream);
//                    }

//                    _context.ProductImages.Add(new ProductImage
//                    {
//                        ProductId = product.Id,
//                        ImageUrl = "/images/" + fileName
//                    });
//                }
//                await _context.SaveChangesAsync();
//            }
//            else
//            {
//                _context.ProductImages.Add(new ProductImage
//                {
//                    ProductId = product.Id,
//                    ImageUrl = "/images/no-image.jpg"
//                });
//                await _context.SaveChangesAsync();
//            }

//            TempData["Success"] = "Product added successfully!";
//            return RedirectToAction("Index");
//        }

//        public async Task<IActionResult> EditProduct(int id)
//        {
//            var product = await _context.Products
//                .Include(p => p.Images)
//                .FirstOrDefaultAsync(p => p.Id == id);

//            if (product == null)
//                return NotFound();

//            return View(product);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> EditProduct(
//            Product product,
//            List<IFormFile>? images,
//            string RemainingImageIds)
//        {
//            var dbProduct = await _context.Products
//                .Include(p => p.Images)
//                .FirstOrDefaultAsync(p => p.Id == product.Id);

//            if (dbProduct == null)
//                return NotFound();

//            // 🔹 BASIC UPDATE (with discount fields)
//            dbProduct.Name = product.Name;
//            dbProduct.Description = product.Description;
//            dbProduct.Price = product.Price;
//            dbProduct.Stock = product.Stock;
//            dbProduct.Category = product.Category;
//            dbProduct.OriginalPrice = product.OriginalPrice;
//            dbProduct.ShowDiscount = product.ShowDiscount;

//            // 🔴 DELETE REMOVED IMAGES
//            var remainingIds = string.IsNullOrEmpty(RemainingImageIds)
//                ? new List<int>()
//                : RemainingImageIds.Split(',').Select(int.Parse).ToList();

//            var imagesToDelete = dbProduct.Images
//                .Where(i => !remainingIds.Contains(i.Id))
//                .ToList();

//            foreach (var img in imagesToDelete)
//            {
//                var path = Path.Combine(_environment.WebRootPath, img.ImageUrl.TrimStart('/'));
//                if (System.IO.File.Exists(path))
//                    System.IO.File.Delete(path);

//                _context.ProductImages.Remove(img);
//            }

//            // 🟢 ADD NEW IMAGES
//            if (images != null && images.Any())
//            {
//                foreach (var file in images)
//                {
//                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
//                    var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);

//                    using var stream = new FileStream(filePath, FileMode.Create);
//                    await file.CopyToAsync(stream);

//                    dbProduct.Images.Add(new ProductImage
//                    {
//                        ProductId = dbProduct.Id,
//                        ImageUrl = "/images/" + fileName
//                    });
//                }
//            }

//            await _context.SaveChangesAsync();
//            TempData["Success"] = "Product updated successfully!";
//            return RedirectToAction("Index");
//        }

//        // DELETE SINGLE IMAGE
//        [HttpPost]
//        public async Task<IActionResult> DeleteProductImage([FromBody] int imageId)
//        {
//            var img = await _context.ProductImages.FindAsync(imageId);
//            if (img == null) return NotFound();

//            var path = Path.Combine(_environment.WebRootPath, img.ImageUrl.TrimStart('/'));
//            if (System.IO.File.Exists(path))
//                System.IO.File.Delete(path);

//            _context.ProductImages.Remove(img);
//            await _context.SaveChangesAsync();

//            return Ok();
//        }

//        // REPLACE IMAGE
//        [HttpPost]
//        public async Task<IActionResult> ReplaceProductImage(int imageId, int productId, IFormFile newImage)
//        {
//            if (newImage == null || newImage.Length == 0)
//            {
//                TempData["Error"] = "Please select an image!";
//                return RedirectToAction("EditProduct", new { id = productId });
//            }

//            var image = await _context.ProductImages.FindAsync(imageId);

//            if (image != null)
//            {
//                // Purani image delete karo
//                if (image.ImageUrl != "/images/no-image.jpg")
//                {
//                    var oldImagePath = Path.Combine(_environment.WebRootPath, image.ImageUrl.TrimStart('/'));
//                    if (System.IO.File.Exists(oldImagePath))
//                    {
//                        System.IO.File.Delete(oldImagePath);
//                    }
//                }

//                // Nayi image save karo
//                var fileName = Guid.NewGuid() + Path.GetExtension(newImage.FileName);
//                var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);

//                using (var stream = new FileStream(filePath, FileMode.Create))
//                {
//                    await newImage.CopyToAsync(stream);
//                }

//                // Database mein update karo
//                image.ImageUrl = "/images/" + fileName;
//                await _context.SaveChangesAsync();

//                TempData["Success"] = "Image replaced successfully!";
//            }

//            return RedirectToAction("EditProduct", new { id = productId });
//        }

//        [HttpPost]
//        public async Task<IActionResult> DeleteProduct(int id)
//        {
//            var product = await _context.Products
//                .Include(p => p.Images)
//                .FirstOrDefaultAsync(p => p.Id == id);

//            if (product != null)
//            {
//                foreach (var img in product.Images)
//                {
//                    var imagePath = Path.Combine(_environment.WebRootPath, img.ImageUrl.TrimStart('/'));
//                    if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);
//                }

//                _context.Products.Remove(product);
//                await _context.SaveChangesAsync();
//                TempData["Success"] = "Product deleted successfully!";
//            }

//            return RedirectToAction("Index");
//        }

//        public async Task<IActionResult> Orders()
//        {
//            var orders = await _context.Orders
//                .Include(o => o.OrderItems)
//                .ThenInclude(oi => oi.Product)
//                .OrderByDescending(o => o.OrderDate)
//                .ToListAsync();

//            return View(orders);
//        }


//        public async Task<IActionResult> OrderDetails(int id)
//        {
//            var order = await _context.Orders
//                .Include(o => o.OrderItems)
//                .ThenInclude(oi => oi.Product)
//                .FirstOrDefaultAsync(o => o.Id == id);

//            if (order == null) return NotFound();

//            return View(order); // ✅ Single order
//        }


//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> MarkAsDelivered(int orderId)
//        {
//            try
//            {
//                var order = await _context.Orders
//                    .Include(o => o.OrderItems)
//                    .ThenInclude(oi => oi.Product)
//                    .FirstOrDefaultAsync(o => o.Id == orderId);

//                if (order == null)
//                {
//                    TempData["Error"] = "Order not found.";
//                    return RedirectToAction("Orders");
//                }

//                if (order.Status != "Pending")
//                {
//                    TempData["Error"] = "Only pending orders can be marked as delivered.";
//                    return RedirectToAction("OrderDetails", new { id = orderId });
//                }

//                // Update order status
//                order.Status = "Delivered";
//                order.StatusUpdatedAt = DateTime.Now;
//                order.NotificationSent = true;

//                await _context.SaveChangesAsync();

//                // Send delivery confirmation email
//                try
//                {
//                    SendDeliveryConfirmationEmail(order);
//                    TempData["Success"] = "Order marked as delivered. Customer has been notified via email.";
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Failed to send delivery email");
//                    TempData["Success"] = "Order marked as delivered, but email notification failed.";
//                }

//                return RedirectToAction("OrderDetails", new { id = orderId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error marking order as delivered");
//                TempData["Error"] = "An error occurred. Please try again.";
//                return RedirectToAction("OrderDetails", new { id = orderId });
//            }
//        }

//        // Not Accept Order
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> NotAcceptOrder(int orderId, string reason)
//        {
//            try
//            {
//                var order = await _context.Orders
//                    .Include(o => o.OrderItems)
//                    .ThenInclude(oi => oi.Product)
//                    .FirstOrDefaultAsync(o => o.Id == orderId);

//                if (order == null)
//                {
//                    TempData["Error"] = "Order not found.";
//                    return RedirectToAction("Orders");
//                }

//                if (order.Status != "Pending")
//                {
//                    TempData["Error"] = "Only pending orders can be rejected.";
//                    return RedirectToAction("OrderDetails", new { id = orderId });
//                }

//                if (string.IsNullOrWhiteSpace(reason))
//                {
//                    TempData["Error"] = "Please provide a reason.";
//                    return RedirectToAction("OrderDetails", new { id = orderId });
//                }

//                // Update order status
//                order.Status = "NotAccepted";
//                order.StatusReason = reason;
//                order.StatusUpdatedAt = DateTime.Now;
//                order.NotificationSent = true;

//                await _context.SaveChangesAsync();

//                // Send rejection email
//                try
//                {
//                    SendNotAcceptedEmail(order);
//                    TempData["Success"] = "Order marked as not accepted. Customer has been notified.";
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Failed to send not accepted email");
//                    TempData["Success"] = "Order marked as not accepted, but email notification failed.";
//                }

//                return RedirectToAction("OrderDetails", new { id = orderId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error rejecting order");
//                TempData["Error"] = "An error occurred. Please try again.";
//                return RedirectToAction("OrderDetails", new { id = orderId });
//            }
//        }

//        // Cancel Order
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> CancelOrder(int orderId, string reason)
//        {
//            try
//            {
//                var order = await _context.Orders
//                    .Include(o => o.OrderItems)
//                    .ThenInclude(oi => oi.Product)
//                    .FirstOrDefaultAsync(o => o.Id == orderId);

//                if (order == null)
//                {
//                    TempData["Error"] = "Order not found.";
//                    return RedirectToAction("Orders");
//                }

//                if (order.Status != "Pending")
//                {
//                    TempData["Error"] = "Only pending orders can be cancelled.";
//                    return RedirectToAction("OrderDetails", new { id = orderId });
//                }

//                if (string.IsNullOrWhiteSpace(reason))
//                {
//                    TempData["Error"] = "Please provide a cancellation reason.";
//                    return RedirectToAction("OrderDetails", new { id = orderId });
//                }

//                // Update order status
//                order.Status = "Cancelled";
//                order.StatusReason = reason;
//                order.StatusUpdatedAt = DateTime.Now;
//                order.NotificationSent = true;

//                await _context.SaveChangesAsync();

//                // Send cancellation email
//                try
//                {
//                    SendCancellationEmail(order);
//                    TempData["Success"] = "Order cancelled successfully. Customer has been notified.";
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Failed to send cancellation email");
//                    TempData["Success"] = "Order cancelled successfully, but email notification failed.";
//                }

//                return RedirectToAction("OrderDetails", new { id = orderId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error cancelling order");
//                TempData["Error"] = "An error occurred. Please try again.";
//                return RedirectToAction("OrderDetails", new { id = orderId });
//            }
//        }

//        // Delete Order
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteOrder(int orderId)
//        {
//            try
//            {
//                var order = await _context.Orders
//                    .Include(o => o.OrderItems)
//                    .FirstOrDefaultAsync(o => o.Id == orderId);

//                if (order == null)
//                {
//                    TempData["Error"] = "Order not found.";
//                    return RedirectToAction("Orders");
//                }

//                if (order.Status == "Pending")
//                {
//                    TempData["Error"] = "Cannot delete pending orders. Please process the order first.";
//                    return RedirectToAction("OrderDetails", new { id = orderId });
//                }

//                _context.Orders.Remove(order);
//                await _context.SaveChangesAsync();

//                TempData["Success"] = $"Order #{orderId} has been permanently deleted.";
//                return RedirectToAction("Orders");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error deleting order");
//                TempData["Error"] = "An error occurred while deleting the order.";
//                return RedirectToAction("OrderDetails", new { id = orderId });
//            }
//        }

//        // ============================================
//        // EMAIL SENDING METHODS
//        // ============================================

//        private void SendDeliveryConfirmationEmail(Order order)
//        {
//            try
//            {
//                var message = new MimeMessage();
//                message.From.Add(new MailboxAddress("BAZARIO Store", "INFO.BAZARIO.STORE@gmail.com"));
//                message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
//                message.Subject = "🎉 Your Order Has Been Delivered - BAZARIO";

//                string body = $@"
//<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f5f5f5;'>
//    <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
//        <h1 style='margin: 0; font-size: 28px;'>🎉 Order Delivered!</h1>
//        <p style='margin: 10px 0 0 0; font-size: 16px;'>Your package has arrived</p>
//    </div>

//    <div style='background: white; padding: 30px; border-radius: 0 0 10px 10px;'>
//        <p style='font-size: 16px; color: #333;'>Dear <strong>{order.CustomerName}</strong>,</p>

//        <p style='font-size: 16px; color: #333; line-height: 1.6;'>
//            Great news! Your order <strong>#{order.Id}</strong> has been successfully delivered and is now ready for you to enjoy!
//        </p>

//        <div style='background: #d4edda; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #28a745;'>
//            <h3 style='color: #155724; margin-top: 0;'>📦 Delivery Confirmation</h3>
//            <p style='color: #155724; margin: 5px 0;'><strong>Order ID:</strong> #{order.Id}</p>
//            <p style='color: #155724; margin: 5px 0;'><strong>Delivered On:</strong> {DateTime.Now:dd MMM yyyy, hh:mm tt}</p>
//            <p style='color: #155724; margin: 5px 0;'><strong>Delivery Address:</strong> {order.Address}</p>
//        </div>

//        <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
//            <h3 style='color: #333; margin-top: 0;'>📋 Order Summary</h3>
//            <table style='width: 100%; border-collapse: collapse;'>
//                <tr style='border-bottom: 1px solid #dee2e6;'>
//                    <td style='padding: 10px 0; color: #666;'>Total Items:</td>
//                    <td style='padding: 10px 0; color: #333; text-align: right;'>{order.OrderItems.Count} item(s)</td>
//                </tr>
//                <tr style='border-bottom: 1px solid #dee2e6;'>
//                    <td style='padding: 10px 0; color: #666;'>Total Amount:</td>
//                    <td style='padding: 10px 0; color: #28a745; text-align: right; font-weight: bold; font-size: 18px;'>Rs. {order.TotalAmount:N0}</td>
//                </tr>
//                <tr>
//                    <td style='padding: 10px 0; color: #666;'>Payment Method:</td>
//                    <td style='padding: 10px 0; color: #333; text-align: right;'>{order.PaymentMethod}</td>
//                </tr>
//            </table>
//        </div>

//        <div style='background: #fff3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
//            <h3 style='color: #856404; margin-top: 0;'>💬 We'd Love Your Feedback!</h3>
//            <p style='color: #856404; margin: 0;'>
//                Your satisfaction is our priority. If you have any questions or concerns about your order, 
//                please don't hesitate to contact us.
//            </p>
//        </div>

//        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
//            Thank you for choosing <strong>BAZARIO</strong>! We hope you love your purchase.
//        </p>

//        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
//            Best Regards,<br>
//            <strong>The BAZARIO Team</strong>
//        </p>
//    </div>

//    <div style='text-align: center; margin-top: 20px; color: #999; font-size: 12px;'>
//        <p>This is an automated notification from BAZARIO Store</p>
//        <p>© {DateTime.Now.Year} BAZARIO. All rights reserved.</p>
//    </div>
//</div>";

//                message.Body = new TextPart("html") { Text = body };

//                using (var client = new SmtpClient())
//                {
//                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
//                    client.Connect("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
//                    client.Authenticate("INFO.BAZARIO.STORE@gmail.com", "pyhs yayn vecz yakf");
//                    client.Send(message);
//                    client.Disconnect(true);
//                }

//                _logger.LogInformation("Delivery confirmation email sent to {Email}", order.Email);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error sending delivery confirmation email");
//                throw;
//            }
//        }

//        private void SendNotAcceptedEmail(Order order)
//        {
//            try
//            {
//                var message = new MimeMessage();
//                message.From.Add(new MailboxAddress("BAZARIO Store", "INFO.BAZARIO.STORE@gmail.com"));
//                message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
//                message.Subject = "Order Update - Unable to Process Your Order - BAZARIO";

//                string body = $@"
//<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f5f5f5;'>
//    <div style='background: linear-gradient(135deg, #ffc107 0%, #ff9800 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
//        <h1 style='margin: 0; font-size: 28px;'>⚠️ Order Update</h1>
//        <p style='margin: 10px 0 0 0; font-size: 16px;'>Regarding Order #{order.Id}</p>
//    </div>

//    <div style='background: white; padding: 30px; border-radius: 0 0 10px 10px;'>
//        <p style='font-size: 16px; color: #333;'>Dear <strong>{order.CustomerName}</strong>,</p>

//        <p style='font-size: 16px; color: #333; line-height: 1.6;'>
//            We regret to inform you that we are unable to process your order <strong>#{order.Id}</strong> at this time.
//        </p>

//        <div style='background: #fff3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
//            <h3 style='color: #856404; margin-top: 0;'>📝 Reason</h3>
//            <p style='color: #856404; margin: 0; font-size: 16px;'><strong>{order.StatusReason}</strong></p>
//        </div>

//        <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
//            <h3 style='color: #333; margin-top: 0;'>📦 Order Details</h3>
//            <p style='margin: 5px 0; color: #666;'><strong>Order ID:</strong> #{order.Id}</p>
//            <p style='margin: 5px 0; color: #666;'><strong>Order Date:</strong> {order.OrderDate:dd MMM yyyy}</p>
//            <p style='margin: 5px 0; color: #666;'><strong>Total Amount:</strong> Rs. {order.TotalAmount:N0}</p>
//        </div>

//        <div style='background: #e7f3ff; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #007bff;'>
//            <h3 style='color: #004085; margin-top: 0;'>💡 What's Next?</h3>
//            <ul style='color: #004085; margin: 10px 0; padding-left: 20px;'>
//                <li style='margin: 8px 0;'>If you paid online, your refund will be processed within 5-7 business days</li>
//                <li style='margin: 8px 0;'>You can browse our store for alternative products</li>
//                <li style='margin: 8px 0;'>Contact our customer support if you have any questions</li>
//            </ul>
//        </div>

//        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
//            We sincerely apologize for any inconvenience this may have caused. We value your business and hope to serve you better in the future.
//        </p>

//        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
//            If you have any questions or concerns, please feel free to contact us.
//        </p>

//        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
//            Best Regards,<br>
//            <strong>The BAZARIO Team</strong>
//        </p>
//    </div>

//    <div style='text-align: center; margin-top: 20px; color: #999; font-size: 12px;'>
//        <p>This is an automated notification from BAZARIO Store</p>
//        <p>© {DateTime.Now.Year} BAZARIO. All rights reserved.</p>
//    </div>
//</div>";

//                message.Body = new TextPart("html") { Text = body };

//                using (var client = new SmtpClient())
//                {
//                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
//                    client.Connect("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
//                    client.Authenticate("INFO.BAZARIO.STORE@gmail.com", "pyhs yayn vecz yakf");
//                    client.Send(message);
//                    client.Disconnect(true);
//                }

//                _logger.LogInformation("Not accepted email sent to {Email}", order.Email);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error sending not accepted email");
//                throw;
//            }
//        }

//        private void SendCancellationEmail(Order order)
//        {
//            try
//            {
//                var message = new MimeMessage();
//                message.From.Add(new MailboxAddress("BAZARIO Store", "INFO.BAZARIO.STORE@gmail.com"));
//                message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
//                message.Subject = "Order Cancelled - Refund Information - BAZARIO";

//                string body = $@"
//<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f5f5f5;'>
//    <div style='background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
//        <h1 style='margin: 0; font-size: 28px;'>❌ Order Cancelled</h1>
//        <p style='margin: 10px 0 0 0; font-size: 16px;'>Order #{order.Id}</p>
//    </div>

//    <div style='background: white; padding: 30px; border-radius: 0 0 10px 10px;'>
//        <p style='font-size: 16px; color: #333;'>Dear <strong>{order.CustomerName}</strong>,</p>

//        <p style='font-size: 16px; color: #333; line-height: 1.6;'>
//            We're writing to confirm that your order <strong>#{order.Id}</strong> has been cancelled.
//        </p>

//        <div style='background: #f8d7da; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #dc3545;'>
//            <h3 style='color: #721c24; margin-top: 0;'>📝 Cancellation Reason</h3>
//            <p style='color: #721c24; margin: 0; font-size: 16px;'><strong>{order.StatusReason}</strong></p>
//        </div>

//        <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
//            <h3 style='color: #333; margin-top: 0;'>📦 Cancelled Order Details</h3>
//            <p style='margin: 5px 0; color: #666;'><strong>Order ID:</strong> #{order.Id}</p>
//            <p style='margin: 5px 0; color: #666;'><strong>Order Date:</strong> {order.OrderDate:dd MMM yyyy}</p>
//            <p style='margin: 5px 0; color: #666;'><strong>Cancelled On:</strong> {DateTime.Now:dd MMM yyyy, hh:mm tt}</p>
//            <p style='margin: 5px 0; color: #666;'><strong>Order Amount:</strong> Rs. {order.TotalAmount:N0}</p>
//        </div>

//        <div style='background: #d1ecf1; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #17a2b8;'>
//            <h3 style='color: #0c5460; margin-top: 0;'>💰 Refund Information</h3>
//            <p style='color: #0c5460; margin: 8px 0;'>
//                {(order.PaymentMethod == "COD"
//                    ? "Since this was a Cash on Delivery order, no payment was collected. No refund is necessary."
//                    : "Your refund of Rs. " + order.TotalAmount.ToString("N0") + " will be processed within 5-7 business days and credited to your original payment method.")}
//            </p>
//        </div>

//        <div style='background: #e7f3ff; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #007bff;'>
//            <h3 style='color: #004085; margin-top: 0;'>🛍️ Continue Shopping</h3>
//            <p style='color: #004085; margin: 0;'>
//                We're sorry this order didn't work out. We hope you'll give us another chance to serve you. 
//                Feel free to browse our latest collection and place a new order anytime!
//            </p>
//        </div>

//        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
//            We apologize for any inconvenience caused. If you have any questions or concerns regarding this cancellation, 
//            please don't hesitate to contact our customer support team.
//        </p>

//        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
//            Thank you for your understanding.
//        </p>

//        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
//            Best Regards,<br>
//            <strong>The BAZARIO Team</strong>
//        </p>
//    </div>

//    <div style='text-align: center; margin-top: 20px; color: #999; font-size: 12px;'>
//        <p>This is an automated notification from BAZARIO Store</p>
//        <p>© {DateTime.Now.Year} BAZARIO. All rights reserved.</p>
//    </div>
//</div>";

//                message.Body = new TextPart("html") { Text = body };

//                using (var client = new SmtpClient())
//                {
//                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
//                    client.Connect("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
//                    client.Authenticate("INFO.BAZARIO.STORE@gmail.com", "pyhs yayn vecz yakf");
//                    client.Send(message);
//                    client.Disconnect(true);
//                }

//                _logger.LogInformation("Cancellation email sent to {Email}", order.Email);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error sending cancellation email");
//                throw;
//            }
//        }
//    }
//}
using EcommerceStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceStore.Data;
using Microsoft.AspNetCore.Authorization;
using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;

namespace EcommerceStore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<AdminController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(products);
        }

        public IActionResult AddProduct()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, List<IFormFile>? images)
        {
            product.CreatedAt = DateTime.Now;
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            if (images != null && images.Count > 0)
            {
                foreach (var image in images)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = "/images/" + fileName
                    });
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    ImageUrl = "/images/no-image.jpg"
                });
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Product added successfully!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(
            Product product,
            List<IFormFile>? images,
            string RemainingImageIds)
        {
            var dbProduct = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == product.Id);

            if (dbProduct == null)
                return NotFound();

            dbProduct.Name = product.Name;
            dbProduct.Description = product.Description;
            dbProduct.Price = product.Price;
            dbProduct.Stock = product.Stock;
            dbProduct.Category = product.Category;
            dbProduct.OriginalPrice = product.OriginalPrice;
            dbProduct.ShowDiscount = product.ShowDiscount;

            var remainingIds = string.IsNullOrEmpty(RemainingImageIds)
                ? new List<int>()
                : RemainingImageIds.Split(',').Select(int.Parse).ToList();

            var imagesToDelete = dbProduct.Images
                .Where(i => !remainingIds.Contains(i.Id))
                .ToList();

            foreach (var img in imagesToDelete)
            {
                var path = Path.Combine(_environment.WebRootPath, img.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);

                _context.ProductImages.Remove(img);
            }

            if (images != null && images.Any())
            {
                foreach (var file in images)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    dbProduct.Images.Add(new ProductImage
                    {
                        ProductId = dbProduct.Id,
                        ImageUrl = "/images/" + fileName
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Product updated successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProductImage([FromBody] int imageId)
        {
            var img = await _context.ProductImages.FindAsync(imageId);
            if (img == null) return NotFound();

            var path = Path.Combine(_environment.WebRootPath, img.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);

            _context.ProductImages.Remove(img);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ReplaceProductImage(int imageId, int productId, IFormFile newImage)
        {
            if (newImage == null || newImage.Length == 0)
            {
                TempData["Error"] = "Please select an image!";
                return RedirectToAction("EditProduct", new { id = productId });
            }

            var image = await _context.ProductImages.FindAsync(imageId);

            if (image != null)
            {
                if (image.ImageUrl != "/images/no-image.jpg")
                {
                    var oldImagePath = Path.Combine(_environment.WebRootPath, image.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                var fileName = Guid.NewGuid() + Path.GetExtension(newImage.FileName);
                var filePath = Path.Combine(_environment.WebRootPath, "images", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newImage.CopyToAsync(stream);
                }

                image.ImageUrl = "/images/" + fileName;
                await _context.SaveChangesAsync();

                TempData["Success"] = "Image replaced successfully!";
            }

            return RedirectToAction("EditProduct", new { id = productId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)
            {
                foreach (var img in product.Images)
                {
                    var imagePath = Path.Combine(_environment.WebRootPath, img.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product deleted successfully!";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // Mark as Delivered
        [HttpPost]
        public async Task<IActionResult> MarkAsDelivered([FromBody] OrderActionRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                if (order.Status != "Processing")
                {
                    return Json(new { success = false, message = "Only Processing orders can be marked as delivered." });
                }

                order.Status = "Delivered";
                order.StatusUpdatedAt = DateTime.Now;
                order.NotificationSent = true;

                await _context.SaveChangesAsync();

                try
                {
                    SendDeliveryConfirmationEmail(order);
                    _logger.LogInformation("Delivery email sent for Order #{OrderId}", order.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send delivery email for Order #{OrderId}", order.Id);
                }

                return Json(new { success = true, message = "Order marked as delivered and customer notified." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking order as delivered");
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        // Cancel Order
        [HttpPost]
        public async Task<IActionResult> CancelOrder([FromBody] OrderActionRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                if (order.Status != "Pending")
                {
                    return Json(new { success = false, message = "Only pending orders can be cancelled." });
                }

                // Auto-generated professional reason
                string cancellationReason = "We regret to inform you that due to unforeseen circumstances, we are unable to fulfill your order at this time. This may be due to product unavailability, logistics issues, or other operational constraints.";

                order.Status = "Cancelled";
                order.StatusReason = cancellationReason;
                order.StatusUpdatedAt = DateTime.Now;
                order.NotificationSent = true;

                await _context.SaveChangesAsync();

                try
                {
                    SendCancellationEmail(order);
                    _logger.LogInformation("Cancellation email sent for Order #{OrderId}", order.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send cancellation email for Order #{OrderId}", order.Id);
                }

                return Json(new { success = true, message = "Order cancelled and customer notified." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order");
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        // Not Accept Order
        [HttpPost]
        public async Task<IActionResult> NotAcceptOrder([FromBody] OrderActionRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                if (order.Status != "Pending")
                {
                    return Json(new { success = false, message = "Only pending orders can be rejected." });
                }

                // Auto-generated professional reason
                string rejectionReason = "After careful review, we are unable to process your order. This decision may be due to verification issues, payment concerns, shipping restrictions to your area, or product availability constraints.";

                order.Status = "NotAccepted";
                order.StatusReason = rejectionReason;
                order.StatusUpdatedAt = DateTime.Now;
                order.NotificationSent = true;

                await _context.SaveChangesAsync();

                try
                {
                    SendNotAcceptedEmail(order);
                    _logger.LogInformation("Not accepted email sent for Order #{OrderId}", order.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send not accepted email for Order #{OrderId}", order.Id);
                }

                return Json(new { success = true, message = "Order marked as not accepted and customer notified." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting order");
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        // Delete Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction("Orders");
                }

                if (order.Status == "Pending")
                {
                    TempData["Error"] = "Cannot delete pending orders. Please process the order first.";
                    return RedirectToAction("OrderDetails", new { id = orderId });
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Order #{orderId} has been permanently deleted.";
                return RedirectToAction("Orders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order");
                TempData["Error"] = "An error occurred while deleting the order.";
                return RedirectToAction("OrderDetails", new { id = orderId });
            }
        }

        // ============================================
        // EMAIL SENDING METHODS
        // ============================================

        private void SendDeliveryConfirmationEmail(Order order)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("BAZARIO Store", "INFO.BAZARIO.STORE@gmail.com"));
                message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
                message.Subject = "🎉 Your Order Has Been Delivered - BAZARIO";

                string body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f5f5f5;'>
    <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='margin: 0; font-size: 28px;'>🎉 Order Delivered!</h1>
        <p style='margin: 10px 0 0 0; font-size: 16px;'>Your package has arrived</p>
    </div>
    
    <div style='background: white; padding: 30px; border-radius: 0 0 10px 10px;'>
        <p style='font-size: 16px; color: #333;'>Dear <strong>{order.CustomerName}</strong>,</p>
        
        <p style='font-size: 16px; color: #333; line-height: 1.6;'>
            Great news! Your order <strong>#{order.Id}</strong> has been successfully delivered and is now ready for you to enjoy!
        </p>

        <div style='background: #d4edda; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #28a745;'>
            <h3 style='color: #155724; margin-top: 0;'>📦 Delivery Confirmation</h3>
            <p style='color: #155724; margin: 5px 0;'><strong>Order ID:</strong> #{order.Id}</p>
            <p style='color: #155724; margin: 5px 0;'><strong>Delivered On:</strong> {DateTime.Now:dd MMM yyyy, hh:mm tt}</p>
            <p style='color: #155724; margin: 5px 0;'><strong>Delivery Address:</strong> {order.Address}</p>
        </div>

        <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <h3 style='color: #333; margin-top: 0;'>📋 Order Summary</h3>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr style='border-bottom: 1px solid #dee2e6;'>
                    <td style='padding: 10px 0; color: #666;'>Total Items:</td>
                    <td style='padding: 10px 0; color: #333; text-align: right;'>{order.OrderItems.Count} item(s)</td>
                </tr>
                <tr style='border-bottom: 1px solid #dee2e6;'>
                    <td style='padding: 10px 0; color: #666;'>Total Amount:</td>
                    <td style='padding: 10px 0; color: #28a745; text-align: right; font-weight: bold; font-size: 18px;'>Rs. {order.TotalAmount:N0}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 0; color: #666;'>Payment Method:</td>
                    <td style='padding: 10px 0; color: #333; text-align: right;'>{order.PaymentMethod}</td>
                </tr>
            </table>
        </div>

        <div style='background: #fff3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
            <h3 style='color: #856404; margin-top: 0;'>💬 We'd Love Your Feedback!</h3>
            <p style='color: #856404; margin: 0;'>
                Your satisfaction is our priority. If you have any questions or concerns about your order, 
                please don't hesitate to contact us.
            </p>
        </div>

        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
            Thank you for choosing <strong>BAZARIO</strong>! We hope you love your purchase.
        </p>

        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
            Best Regards,<br>
            <strong>The BAZARIO Team</strong>
        </p>
    </div>

    <div style='text-align: center; margin-top: 20px; color: #999; font-size: 12px;'>
        <p>This is an automated notification from BAZARIO Store</p>
        <p>© {DateTime.Now.Year} BAZARIO. All rights reserved.</p>
    </div>
</div>";

                message.Body = new TextPart("html") { Text = body };

                using (var client = new SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    client.Connect("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
                    client.Authenticate("INFO.BAZARIO.STORE@gmail.com", "pyhs yayn vecz yakf");
                    client.Send(message);
                    client.Disconnect(true);
                }

                _logger.LogInformation("Delivery confirmation email sent to {Email}", order.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending delivery confirmation email");
                throw;
            }
        }

        private void SendNotAcceptedEmail(Order order)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("BAZARIO Store", "INFO.BAZARIO.STORE@gmail.com"));
                message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
                message.Subject = "Order Update - Unable to Process Your Order - BAZARIO";

                string body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f5f5f5;'>
    <div style='background: linear-gradient(135deg, #ffc107 0%, #ff9800 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='margin: 0; font-size: 28px;'>⚠️ Order Update</h1>
        <p style='margin: 10px 0 0 0; font-size: 16px;'>Regarding Order #{order.Id}</p>
    </div>
    
    <div style='background: white; padding: 30px; border-radius: 0 0 10px 10px;'>
        <p style='font-size: 16px; color: #333;'>Dear <strong>{order.CustomerName}</strong>,</p>
        
        <p style='font-size: 16px; color: #333; line-height: 1.6;'>
            We regret to inform you that we are unable to process your order <strong>#{order.Id}</strong> at this time.
        </p>

        <div style='background: #fff3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
            <h3 style='color: #856404; margin-top: 0;'>📝 Reason</h3>
            <p style='color: #856404; margin: 0; font-size: 15px; line-height: 1.6;'>{order.StatusReason}</p>
        </div>

        <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <h3 style='color: #333; margin-top: 0;'>📦 Order Details</h3>
            <p style='margin: 5px 0; color: #666;'><strong>Order ID:</strong> #{order.Id}</p>
            <p style='margin: 5px 0; color: #666;'><strong>Order Date:</strong> {order.OrderDate:dd MMM yyyy}</p>
            <p style='margin: 5px 0; color: #666;'><strong>Total Amount:</strong> Rs. {order.TotalAmount:N0}</p>
        </div>

        <div style='background: #e7f3ff; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #007bff;'>
            <h3 style='color: #004085; margin-top: 0;'>💡 What's Next?</h3>
            <ul style='color: #004085; margin: 10px 0; padding-left: 20px;'>
                <li style='margin: 8px 0;'>If you paid online, your refund will be processed within 5-7 business days</li>
                <li style='margin: 8px 0;'>You can browse our store for alternative products</li>
                <li style='margin: 8px 0;'>Contact our customer support if you have any questions</li>
            </ul>
        </div>

        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
            We sincerely apologize for any inconvenience this may have caused. We value your business and hope to serve you better in the future.
        </p>

        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
            If you have any questions or concerns, please feel free to contact us.
        </p>

        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
            Best Regards,<br>
            <strong>The BAZARIO Team</strong>
        </p>
    </div>

    <div style='text-align: center; margin-top: 20px; color: #999; font-size: 12px;'>
        <p>This is an automated notification from BAZARIO Store</p>
        <p>© {DateTime.Now.Year} BAZARIO. All rights reserved.</p>
    </div>
</div>";

                message.Body = new TextPart("html") { Text = body };

                using (var client = new SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    client.Connect("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
                    client.Authenticate("INFO.BAZARIO.STORE@gmail.com", "pyhs yayn vecz yakf");
                    client.Send(message);
                    client.Disconnect(true);
                }

                _logger.LogInformation("Not accepted email sent to {Email}", order.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending not accepted email");
                throw;
            }
        }
        private void SendCancellationEmail(Order order)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("BAZARIO Store", "INFO.BAZARIO.STORE@gmail.com"));
                message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
                message.Subject = "❌ Order Cancelled - BAZARIO";

                string refundMessage =
                    order.PaymentMethod == "COD"
                    ? "Since this was a Cash on Delivery order, no payment was collected. No refund is required."
                    : $"Your refund of <strong>Rs. {order.TotalAmount:N0}</strong> will be processed within 5–7 business days to your original payment method.";

                string body = $@"
<div style='font-family: Arial, sans-serif; max-width:600px; margin:auto; background:#f5f5f5; padding:20px;'>

    <div style='background:linear-gradient(135deg,#dc3545,#c82333); color:white; padding:30px; text-align:center; border-radius:10px 10px 0 0;'>
        <h1 style='margin:0;'>❌ Order Cancelled</h1>
        <p style='margin-top:10px;'>Order #{order.Id}</p>
    </div>

    <div style='background:white; padding:30px; border-radius:0 0 10px 10px;'>
        <p>Dear <strong>{order.CustomerName}</strong>,</p>

        <p>
            We regret to inform you that your order <strong>#{order.Id}</strong> has been cancelled.
        </p>

        <div style='background:#f8d7da; padding:15px; border-left:4px solid #dc3545; margin:20px 0;'>
            <strong>Cancellation Reason:</strong><br/>
            {order.StatusReason}
        </div>

        <div style='background:#f8f9fa; padding:15px; margin:20px 0;'>
            <p><strong>Order Date:</strong> {order.OrderDate:dd MMM yyyy}</p>
            <p><strong>Cancelled On:</strong> {DateTime.Now:dd MMM yyyy, hh:mm tt}</p>
            <p><strong>Order Amount:</strong> Rs. {order.TotalAmount:N0}</p>
            <p><strong>Payment Method:</strong> {order.PaymentMethod}</p>
        </div>

        <div style='background:#d1ecf1; padding:15px; border-left:4px solid #17a2b8; margin:20px 0;'>
            <strong>Refund Information</strong><br/>
            {refundMessage}
        </div>

        <p>
            We apologize for any inconvenience caused.  
            If you have any questions, please contact our support team.
        </p>

        <p>
            Best Regards,<br/>
            <strong>BAZARIO Team</strong>
        </p>
    </div>

    <div style='text-align:center; color:#999; font-size:12px; margin-top:15px;'>
        <p>This is an automated email. Please do not reply.</p>
        <p>© {DateTime.Now.Year} BAZARIO. All rights reserved.</p>
    </div>

</div>";

                message.Body = new TextPart("html") { Text = body };

                using (var client = new SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    client.Connect("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
                    client.Authenticate("INFO.BAZARIO.STORE@gmail.com", "pyhs yayn vecz yakf");
                    client.Send(message);
                    client.Disconnect(true);
                }

                _logger.LogInformation("Cancellation email sent to {Email}", order.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending cancellation email");
                throw;
            }
        }
        [HttpPost]
        public async Task<IActionResult> MoveToProcessing([FromBody] OrderActionRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                if (order.Status != "Pending")
                {
                    return Json(new { success = false, message = "Only pending orders can be moved to processing." });
                }

                order.Status = "Processing";
                order.StatusUpdatedAt = DateTime.Now;
                order.NotificationSent = true;

                await _context.SaveChangesAsync();

                try
                {
                    SendProcessingEmail(order);
                    _logger.LogInformation("Processing email sent for Order #{OrderId}", order.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send processing email for Order #{OrderId}", order.Id);
                }

                return Json(new { success = true, message = "Order moved to processing and customer notified." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving order to processing");
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        // Add this email method in the EMAIL SENDING METHODS section
        private void SendProcessingEmail(Order order)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("BAZARIO Store", "INFO.BAZARIO.STORE@gmail.com"));
                message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
                message.Subject = "🔄 Your Order is Being Processed - BAZARIO";

                string body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f5f5f5;'>
    <div style='background: linear-gradient(135deg, #17a2b8 0%, #138496 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='margin: 0; font-size: 28px;'>🔄 Order Processing</h1>
        <p style='margin: 10px 0 0 0; font-size: 16px;'>Your order is on its way!</p>
    </div>
    
    <div style='background: white; padding: 30px; border-radius: 0 0 10px 10px;'>
        <p style='font-size: 16px; color: #333;'>Dear <strong>{order.CustomerName}</strong>,</p>
        
        <p style='font-size: 16px; color: #333; line-height: 1.6;'>
            Great news! Your order <strong>#{order.Id}</strong> is now being processed and will be delivered to you within 1-2 business days.
        </p>

        <div style='background: #d1ecf1; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #17a2b8;'>
            <h3 style='color: #0c5460; margin-top: 0;'>📦 Order Status Update</h3>
            <p style='color: #0c5460; margin: 5px 0;'><strong>Order ID:</strong> #{order.Id}</p>
            <p style='color: #0c5460; margin: 5px 0;'><strong>Status:</strong> Processing</p>
            <p style='color: #0c5460; margin: 5px 0;'><strong>Updated On:</strong> {DateTime.Now:dd MMM yyyy, hh:mm tt}</p>
            <p style='color: #0c5460; margin: 5px 0;'><strong>Estimated Delivery:</strong> 1-2 Business Days</p>
        </div>

        <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <h3 style='color: #333; margin-top: 0;'>📋 Order Summary</h3>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr style='border-bottom: 1px solid #dee2e6;'>
                    <td style='padding: 10px 0; color: #666;'>Total Items:</td>
                    <td style='padding: 10px 0; color: #333; text-align: right;'>{order.OrderItems.Count} item(s)</td>
                </tr>
                <tr style='border-bottom: 1px solid #dee2e6;'>
                    <td style='padding: 10px 0; color: #666;'>Total Amount:</td>
                    <td style='padding: 10px 0; color: #17a2b8; text-align: right; font-weight: bold; font-size: 18px;'>Rs. {order.TotalAmount:N0}</td>
                </tr>
                <tr style='border-bottom: 1px solid #dee2e6;'>
                    <td style='padding: 10px 0; color: #666;'>Payment Method:</td>
                    <td style='padding: 10px 0; color: #333; text-align: right;'>{order.PaymentMethod}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 0; color: #666;'>Delivery Address:</td>
                    <td style='padding: 10px 0; color: #333; text-align: right;'>{order.Address}</td>
                </tr>
            </table>
        </div>

        <div style='background: #fff3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
            <h3 style='color: #856404; margin-top: 0;'>⏱️ What Happens Next?</h3>
            <ul style='color: #856404; margin: 10px 0; padding-left: 20px; line-height: 1.8;'>
                <li>Your order is being carefully prepared for shipment</li>
                <li>Our team is ensuring quality packaging</li>
                <li>You'll receive a delivery confirmation email once dispatched</li>
                <li>Expected delivery: 1-2 business days</li>
            </ul>
        </div>

        <div style='background: #e7f3ff; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #007bff;'>
            <h3 style='color: #004085; margin-top: 0;'>📞 Need Help?</h3>
            <p style='color: #004085; margin: 0;'>
                If you have any questions about your order, our customer support team is here to help. 
                Feel free to reach out to us anytime.
            </p>
        </div>

        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
            Thank you for choosing <strong>BAZARIO</strong>! We're working hard to get your order to you as quickly as possible.
        </p>

        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
            Best Regards,<br>
            <strong>The BAZARIO Team</strong>
        </p>
    </div>

    <div style='text-align: center; margin-top: 20px; color: #999; font-size: 12px;'>
        <p>This is an automated notification from BAZARIO Store</p>
        <p>© {DateTime.Now.Year} BAZARIO. All rights reserved.</p>
    </div>
</div>";

                message.Body = new TextPart("html") { Text = body };

                using (var client = new SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    client.Connect("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
                    client.Authenticate("INFO.BAZARIO.STORE@gmail.com", "pyhs yayn vecz yakf");
                    client.Send(message);
                    client.Disconnect(true);
                }

                _logger.LogInformation("Processing email sent to {Email}", order.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending processing email");
                throw;
            }
        }

    }
}

//private void SendCancellationEmail(Order order)
//{
//    try
//    {
//        var message = new MimeMessage();
//        message.From.Add(new MailboxAddress("BAZARIO Store", "INFO.BAZARIO.STORE@gmail.com"));
//        message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
//        message.Subject = "Order Cancelled - Refund Information - BAZARIO";

//        string body = $@"
//<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f5f5f5;'>
//    <div style='background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
//        <h1 style='margin: 0; font-size: 28px;'>❌ Order Cancelled</h1>
//        <p style='margin: 10px 0 0 0; font-size: 16px;'>Order #{order.Id}</p>
//    </div>
    
//    <div style='background: white; padding: 30px; border-radius: 0 0 10px 10px;'>
//        <p style='font-size: 16px; color: #333;'>Dear <strong>{order.CustomerName}</strong>,</p>
        
//        <p style='font-size: 16px; color: #333; line-height: 1.6;'>
//            We're writing to confirm that your order <strong>#{order.Id}</strong> has been cancelled.
//        </p>

//        <div style='background: #f8d7da; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #dc3545;'>
//            <h3 style='color: #721c24; margin-top: 0;'>📝 Cancellation Reason</h3>
//            <p style='color: #721c24; margin: 0; font-size: 15px; line-height: 1.6;'>{order.StatusReason}</p>
//        </div>

//        <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
//            <h3 style='color: #333; margin-top: 0;'>📦 Cancelled Order Details</h3>
//            <p style='margin: 5px 0; color: #666;'><strong>Order ID:</strong> #{order.Id}</p>
//            <p style='margin: 5px 0; color: #666;'><strong>Order Date:</strong> {order.OrderDate:dd MMM yyyy}</p>
//            <p style='margin: 5px 0; color: #666;'><strong>Cancelled On:</strong> {DateTime.Now:dd MMM yyyy, hh:mm tt}</p>
//            <p style='margin: 5px 0; color: #666;'><strong>Order Amount:</strong> Rs. {order.TotalAmount:N0}</p>
//        </div>

//        <div style='background: #d1ecf1; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #17a2b8;'>
//            <h3 style='color: #0c5460; margin-top: 0;'>💰 Refund Information</h3>
//            <p style='color: #0c5460; margin: 8px 0;'>
//                {(order.PaymentMethod == "COD"
//            ? "Since this was a Cash on Delivery order, no payment was collected. No refund is necessary."
//            : "Your refund of Rs. " + order.TotalAmount.ToString("N0") + " will be processed within 5-7 business days and credited to your original payment method.")}
//            </p>
//        </div>

//        <div style='background: #e7f3ff; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #007bff;'>
//            <h3 style='color: #004085; margin-top: 0;'>🛍️ Continue Shopping</h3>
//            <p style='color: #004085; margin: 0;'>
//                We're sorry this order didn't work out. We hope you'll give us another chance to serve you. 
//                Feel free to browse our latest collection and place a new order anytime!
//            </p>
//        </div>

//        <p style='font-size: 16px; color: #333; margin-top: 20px;'>
//            We apologize