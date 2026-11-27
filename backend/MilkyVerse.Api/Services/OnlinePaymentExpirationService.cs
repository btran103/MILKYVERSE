using MilkyVerse.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MilkyVerse.Api.Services;

/// <summary>
/// Background job dedicated strictly to expiring overdue ONLINE payment sessions (e.g. MoMo/SePay).
/// Explicitly excludes COD orders so cash-on-delivery orders never expire or auto-cancel.
/// </summary>
public class OnlinePaymentExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OnlinePaymentExpirationService> _logger;

    public OnlinePaymentExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<OnlinePaymentExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                using var scope = _scopeFactory.CreateScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                int expiredCount = await orderService.ExpireOverdueOnlinePaymentsAsync(stoppingToken);
                if (expiredCount > 0)
                {
                    _logger.LogInformation("[OnlinePaymentExpirationService] Expired {Count} overdue online payment sessions.", expiredCount);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[OnlinePaymentExpirationService] Error while sweeping overdue online payments.");
            }
        }
    }
}
