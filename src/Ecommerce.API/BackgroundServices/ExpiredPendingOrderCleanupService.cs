using Ecommerce.Domain.Interfaces;

namespace Ecommerce.API.BackgroundServices;

/// <summary>
/// Định kỳ hủy đơn Pending quá PaymentExpiresAt và hoàn trả tồn kho.
/// </summary>
public class ExpiredPendingOrderCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredPendingOrderCleanupService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    public ExpiredPendingOrderCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredPendingOrderCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepo>();
                var count = await orderRepo.CancelExpiredPendingOrdersAndRestockAsync(stoppingToken);
                if (count > 0)
                    _logger.LogInformation("Cancelled {Count} expired pending order(s) and restored stock.", count);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expired pending order cleanup failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
