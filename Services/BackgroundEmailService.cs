using EcommerceStore.Models;
using System.Collections.Concurrent;

namespace EcommerceStore.Services
{
    public class EmailQueueItem
    {
        public Order Order { get; set; } = null!;
        public List<CartItem> Cart { get; set; } = new();
        public int RetryCount { get; set; } = 0;
        public DateTime QueuedAt { get; set; } = DateTime.Now;
    }

    public class BackgroundEmailService : BackgroundService
    {
        private readonly ILogger<BackgroundEmailService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private static readonly ConcurrentQueue<EmailQueueItem> _emailQueue = new();
        private const int MAX_RETRIES = 3;
        private const int RETRY_DELAY_SECONDS = 5;
        private const int EMPTY_QUEUE_DELAY_MS = 2000;

        public BackgroundEmailService(
            ILogger<BackgroundEmailService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public static void QueueEmail(Order order, List<CartItem> cart)
        {
            _emailQueue.Enqueue(new EmailQueueItem 
            { 
                Order = order, 
                Cart = cart,
                QueuedAt = DateTime.Now
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Background Email Service started");
            _logger.LogInformation("📊 Queue Status: {Count} emails pending", _emailQueue.Count);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_emailQueue.TryDequeue(out var emailItem))
                    {
                        var queueTime = DateTime.Now - emailItem.QueuedAt;
                        _logger.LogInformation("📬 Processing email for Order #{OrderId} (Queued for {QueueTime}s, Retry: {RetryCount}/{MaxRetries})", 
                            emailItem.Order.Id, 
                            queueTime.TotalSeconds,
                            emailItem.RetryCount,
                            MAX_RETRIES);

                        try
                        {
                            using var scope = _serviceScopeFactory.CreateScope();
                            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                            // Send customer email
                            await emailService.SendOrderConfirmationAsync(emailItem.Order, emailItem.Cart);
                            _logger.LogInformation("✅ Customer email sent for Order #{OrderId}", emailItem.Order.Id);

                            // Send admin email
                            await emailService.SendAdminNotificationAsync(emailItem.Order, emailItem.Cart);
                            _logger.LogInformation("✅ Admin email sent for Order #{OrderId}", emailItem.Order.Id);

                            _logger.LogInformation("✅ All emails processed successfully for Order #{OrderId}", emailItem.Order.Id);
                        }
                        catch (Exception ex)
                        {
                            emailItem.RetryCount++;
                            _logger.LogError(ex, "❌ Error sending emails for Order #{OrderId} (Attempt {RetryCount}/{MaxRetries})", 
                                emailItem.Order.Id, 
                                emailItem.RetryCount, 
                                MAX_RETRIES);

                            if (emailItem.RetryCount < MAX_RETRIES)
                            {
                                _logger.LogWarning("🔄 Re-queueing Order #{OrderId} for retry in {Delay}s", 
                                    emailItem.Order.Id, 
                                    RETRY_DELAY_SECONDS);
                                
                                // Re-queue for retry
                                await Task.Delay(TimeSpan.FromSeconds(RETRY_DELAY_SECONDS), stoppingToken);
                                _emailQueue.Enqueue(emailItem);
                            }
                            else
                            {
                                _logger.LogError("❌ FAILED: Order #{OrderId} - Maximum retries ({MaxRetries}) exceeded. Email will NOT be sent.", 
                                    emailItem.Order.Id, 
                                    MAX_RETRIES);
                            }
                        }
                    }
                    else
                    {
                        // Queue is empty, wait before checking again
                        await Task.Delay(EMPTY_QUEUE_DELAY_MS, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Critical error in email processing loop");
                    await Task.Delay(TimeSpan.FromSeconds(RETRY_DELAY_SECONDS), stoppingToken);
                }
            }

            _logger.LogInformation("⛔ Background Email Service stopped");
            _logger.LogInformation("📊 Final Queue Status: {Count} emails remaining", _emailQueue.Count);
        }
    }
}
