using EcommerceStore.Models;
using System.Collections.Concurrent;

namespace EcommerceStore.Services
{
    public class BackgroundEmailService : BackgroundService
    {
        private static readonly ConcurrentQueue<EmailJob> _emailQueue = new();
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<BackgroundEmailService> _logger;

        public BackgroundEmailService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<BackgroundEmailService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public static void QueueEmail(Order order, List<CartItem> cart)
        {
            _emailQueue.Enqueue(new EmailJob
            {
                Order = order,
                Cart = cart,
                QueuedAt = DateTime.Now
            });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Background Email Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_emailQueue.TryDequeue(out var emailJob))
                    {
                        _logger.LogInformation("📧 Processing email for Order #{OrderId}", emailJob.Order.Id);

                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                            try
                            {
                                // Send customer confirmation email
                                await emailService.SendOrderConfirmationAsync(emailJob.Order, emailJob.Cart);
                                _logger.LogInformation("✅ Customer email sent for Order #{OrderId}", emailJob.Order.Id);

                                // Send admin notification email
                                await emailService.SendAdminNotificationAsync(emailJob.Order, emailJob.Cart);
                                _logger.LogInformation("✅ Admin email sent for Order #{OrderId}", emailJob.Order.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "❌ Failed to send emails for Order #{OrderId}", emailJob.Order.Id);
                            }
                        }
                    }

                    // Wait 2 seconds before checking queue again
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("⏹️ Background Email Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in email processing loop");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("🛑 Background Email Service stopped");
        }

        private class EmailJob
        {
            public Order Order { get; set; } = null!;
            public List<CartItem> Cart { get; set; } = null!;
            public DateTime QueuedAt { get; set; }
        }
    }
}
