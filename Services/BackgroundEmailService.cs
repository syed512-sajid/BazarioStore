using EcommerceStore.Models;
using System.Collections.Concurrent;

namespace EcommerceStore.Services
{
    public class EmailQueueItem
    {
        public Order Order { get; set; } = null!;
        public List<CartItem> Cart { get; set; } = new();
    }

    public class BackgroundEmailService : BackgroundService
    {
        private readonly ILogger<BackgroundEmailService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private static readonly ConcurrentQueue<EmailQueueItem> _emailQueue = new();

        public BackgroundEmailService(
            ILogger<BackgroundEmailService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public static void QueueEmail(Order order, List<CartItem> cart)
        {
            _emailQueue.Enqueue(new EmailQueueItem { Order = order, Cart = cart });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Background Email Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_emailQueue.TryDequeue(out var emailItem))
                    {
                        _logger.LogInformation("📬 Processing email for Order #{OrderId}", emailItem.Order.Id);

                        using var scope = _serviceScopeFactory.CreateScope();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        // Send both emails
                        await emailService.SendOrderConfirmationAsync(emailItem.Order, emailItem.Cart);
                        await emailService.SendAdminNotificationAsync(emailItem.Order, emailItem.Cart);

                        _logger.LogInformation("✅ Emails processed for Order #{OrderId}", emailItem.Order.Id);
                    }
                    else
                    {
                        // No emails in queue, wait before checking again
                        await Task.Delay(2000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error processing email queue");
                    await Task.Delay(5000, stoppingToken); // Wait longer on error
                }
            }

            _logger.LogInformation("⛔ Background Email Service stopped");
        }
    }
}