using EcommerceStore.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace EcommerceStore.Services
{
    public class EmailQueueItem
    {
        public Order Order { get; set; } = null!;
        public List<CartItem> Cart { get; set; } = new();
        public int Attempts { get; set; } = 0;
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
            await Task.Delay(3000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_emailQueue.TryDequeue(out var item))
                {
                    item.Attempts++;
                    _logger.LogInformation("📬 Processing Order #{OrderId} (Attempt {Attempt}/3)", item.Order.Id, item.Attempts);

                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        await emailService.SendOrderConfirmationAsync(item.Order, item.Cart);
                        await emailService.SendAdminNotificationAsync(item.Order, item.Cart);

                        _logger.LogInformation("✅ Order #{OrderId} emails sent", item.Order.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Order #{OrderId} failed (Attempt {Attempt})", item.Order.Id, item.Attempts);
                        if (item.Attempts < 3)
                        {
                            await Task.Delay(5000, stoppingToken);
                            _emailQueue.Enqueue(item); // retry
                        }
                    }
                }
                else
                {
                    await Task.Delay(2000, stoppingToken);
                }
            }
        }
    }
}
