using FraudMonitor.Application.Interfaces.Queue;
using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.ValueObjects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraudMonitor.Worker.Simulation;

/// <summary>
/// Continuous online transaction stream simulator.
/// Generates legitimate traffic and fraudulent patterns (atypical high-value, burst frequency, and impossible travel/teleportation).
/// </summary>
public class TransactionStreamSimulator : BackgroundService
{
    private readonly ITransactionQueue _queue;
    private readonly ILogger<TransactionStreamSimulator> _logger;
    private readonly Random _random = new();

    private static readonly Location SaoPaulo = new(-23.5505, -46.6333, "São Paulo", "SP", "BR");
    private static readonly Location RioDeJaneiro = new(-22.9068, -43.1729, "Rio de Janeiro", "RJ", "BR");
    private static readonly Location BeloHorizonte = new(-19.9167, -43.9345, "Belo Horizonte", "MG", "BR");
    private static readonly Location Curitiba = new(-25.4284, -49.2733, "Curitiba", "PR", "BR");
    private static readonly Location NewYork = new(40.7128, -74.0060, "New York", "NY", "US");
    private static readonly Location Tokyo = new(35.6762, 139.6503, "Tokyo", "Tokyo", "JP");

    public TransactionStreamSimulator(ITransactionQueue queue, ILogger<TransactionStreamSimulator> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(">>> TransactionStreamSimulator started. Injecting real-time transactional stream...");
        await Task.Delay(500, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var scenario = _random.Next(100);

                if (scenario < 65)
                {
                    // Legitimate transaction (65%)
                    await EmitLegitimateTransactionAsync(stoppingToken);
                    await Task.Delay(_random.Next(150, 300), stoppingToken);
                }
                else if (scenario < 80)
                {
                    // Atypical high-value transaction (15%)
                    await EmitHighValueTransactionAsync(stoppingToken);
                    await Task.Delay(_random.Next(200, 400), stoppingToken);
                }
                else if (scenario < 90)
                {
                    // Burst frequency transaction (10%)
                    await EmitBurstFrequencyTransactionsAsync(stoppingToken);
                    await Task.Delay(_random.Next(300, 500), stoppingToken);
                }
                else
                {
                    // Impossible travel / divergent geolocation (10%)
                    await EmitImpossibleTravelTransactionsAsync(stoppingToken);
                    await Task.Delay(_random.Next(300, 500), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SIMULATOR_ERROR] Unexpected error in transaction simulator loop");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation(">>> TransactionStreamSimulator stopped.");
    }

    private async Task EmitLegitimateTransactionAsync(CancellationToken stoppingToken)
    {
        var locations = new[] { SaoPaulo, RioDeJaneiro, BeloHorizonte, Curitiba };
        var clientIndex = _random.Next(100, 999);
        var location = locations[_random.Next(locations.Length)];

        var tx = new Transaction
        {
            TransactionId = $"TXN-LEG-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            ClientId = $"CLI-{clientIndex}",
            Amount = Math.Round((decimal)(_random.NextDouble() * 400 + 20), 2),
            Timestamp = DateTime.UtcNow,
            DeviceId = $"DEV-MOBILE-{clientIndex}",
            Location = location
        };

        await _queue.EnqueueAsync(tx, stoppingToken);
    }

    private async Task EmitHighValueTransactionAsync(CancellationToken stoppingToken)
    {
        var isCritical = _random.Next(2) == 0;
        var amount = isCritical ? _random.Next(55000, 95000) : _random.Next(12000, 35000);
        var clientIndex = _random.Next(100, 999);

        var tx = new Transaction
        {
            TransactionId = $"TXN-VAL-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            ClientId = $"CLI-{clientIndex}",
            Amount = amount,
            Timestamp = DateTime.UtcNow,
            DeviceId = $"DEV-WEB-{clientIndex}",
            Location = SaoPaulo
        };

        _logger.LogWarning("[SIMULATOR] Emitting suspicious HIGH_VALUE scenario: Amount: ${Amount:N2}, ClientId: {ClientId}", tx.Amount, tx.ClientId);
        await _queue.EnqueueAsync(tx, stoppingToken);
    }

    private async Task EmitBurstFrequencyTransactionsAsync(CancellationToken stoppingToken)
    {
        var clientId = $"CLI-BURST-{_random.Next(10, 99)}";
        var burstCount = _random.Next(4, 6);

        _logger.LogWarning("[SIMULATOR] Emitting suspicious BURST_FREQUENCY scenario ({Count} txns in rapid burst) for ClientId: {ClientId}", burstCount, clientId);

        for (int i = 0; i < burstCount; i++)
        {
            var tx = new Transaction
            {
                TransactionId = $"TXN-FRQ-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                ClientId = clientId,
                Amount = Math.Round((decimal)(_random.NextDouble() * 200 + 50), 2),
                Timestamp = DateTime.UtcNow,
                DeviceId = $"DEV-ATTACK-{clientId}",
                Location = SaoPaulo
            };

            await _queue.EnqueueAsync(tx, stoppingToken);
            await Task.Delay(20, stoppingToken);
        }
    }

    private async Task EmitImpossibleTravelTransactionsAsync(CancellationToken stoppingToken)
    {
        var clientId = $"CLI-TELEPORT-{_random.Next(10, 99)}";

        _logger.LogWarning("[SIMULATOR] Emitting suspicious IMPOSSIBLE_TRAVEL scenario for ClientId: {ClientId}", clientId);

        // 1st Transaction in São Paulo
        var tx1 = new Transaction
        {
            TransactionId = $"TXN-GEO1-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            ClientId = clientId,
            Amount = 150m,
            Timestamp = DateTime.UtcNow.AddSeconds(-2),
            DeviceId = $"DEV-SP-{clientId}",
            Location = SaoPaulo
        };
        await _queue.EnqueueAsync(tx1, stoppingToken);

        // 2nd Transaction in Tokyo or New York 1 second later
        var foreignLocation = _random.Next(2) == 0 ? Tokyo : NewYork;
        var tx2 = new Transaction
        {
            TransactionId = $"TXN-GEO2-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            ClientId = clientId,
            Amount = 300m,
            Timestamp = DateTime.UtcNow,
            DeviceId = $"DEV-FOREIGN-{clientId}",
            Location = foreignLocation
        };
        await _queue.EnqueueAsync(tx2, stoppingToken);
    }
}
