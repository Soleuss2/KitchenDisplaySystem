using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SelfOrderingSystemKiosk.Services
{
    /// <summary>Creates MongoDB indexes for Orders on startup (non-blocking).</summary>
    public class OrderIndexesHostedService : IHostedService
    {
        private readonly OrderService _orderService;
        private readonly StockMovementService _stockMovementService;
        private readonly ILogger<OrderIndexesHostedService> _logger;

        public OrderIndexesHostedService(OrderService orderService, StockMovementService stockMovementService, ILogger<OrderIndexesHostedService> logger)
        {
            _orderService = orderService;
            _stockMovementService = stockMovementService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _orderService.EnsureIndexesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Order collection index creation failed; queries may be slower until indexes exist.");
            }
            try
            {
                await _stockMovementService.EnsureIndexesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stock movement index creation failed.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
