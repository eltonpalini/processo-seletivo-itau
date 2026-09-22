using System.Diagnostics;
using FraudMonitor.Application.Interfaces.Queue;
using FraudMonitor.Application.Interfaces.Repository;
using FraudMonitor.Application.Services;
using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraudMonitor.Worker;

public class SqsConsumerWorker : BackgroundService
{
    private readonly ITransactionQueue _queue;
    private readonly FraudEvaluationService _fraudEvaluationService;
    private readonly ITransactionWriteRepository _writeRepository;
    private readonly ILogger<SqsConsumerWorker> _logger;

    public SqsConsumerWorker(
        ITransactionQueue queue,
        FraudEvaluationService fraudEvaluationService,
        ITransactionWriteRepository writeRepository,
        ILogger<SqsConsumerWorker> logger)
    {
        _queue = queue;
        _fraudEvaluationService = fraudEvaluationService;
        _writeRepository = writeRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(">>> SqsConsumerWorker started. Listening for incoming queue messages (SQS)...");

        var stopwatch = new Stopwatch();

        await foreach (var transaction in _queue.ReadAllAsync(stoppingToken))
        {
            stopwatch.Restart();

            try
            {
                // 1. Evaluate fraud risk via Strategy engine
                var riskLevel = await _fraudEvaluationService.EvaluateTransactionAsync(transaction);

                // 2. Persist analysis outcome and update customer activity (CQRS Write Model)
                await _writeRepository.SaveTransactionRiskAsync(transaction, riskLevel);

                stopwatch.Stop();
                var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

                // 3. Structured transactional logs evidencing latency compliance (< 1s)
                if (transaction.Status == TransactionStatus.Flagged)
                {
                    var logLevel = riskLevel == RiskLevel.High ? LogLevel.Error : LogLevel.Warning;
                    _logger.Log(
                        logLevel,
                        "[FRAUD_ALERT_TRIGGERED] TransactionId: {TxnId} | ClientId: {ClientId} | RiskLevel: {RiskLevel} | Amount: ${Amount:N2} | TriggeredRules: {TriggeredRules} | Reasons: {Reason} | LatencyMs: {Elapsed:N2} ms",
                        transaction.TransactionId,
                        transaction.ClientId,
                        transaction.RiskLevel,
                        transaction.Amount,
                        string.Join(", ", transaction.TriggeredRules),
                        transaction.RiskReason,
                        elapsedMs
                    );
                }
                else
                {
                    _logger.LogInformation(
                        "[TRANSACTION_APPROVED] TransactionId: {TxnId} | ClientId: {ClientId} | Amount: ${Amount:N2} | LatencyMs: {Elapsed:N2} ms",
                        transaction.TransactionId,
                        transaction.ClientId,
                        transaction.Amount,
                        elapsedMs
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[TRANSACTION_PROCESSING_FAILED] TransactionId: {TxnId} | ClientId: {ClientId} | Forwarding to Dead Letter Queue (DLQ)...",
                    transaction.TransactionId,
                    transaction.ClientId
                );
            }
        }

        _logger.LogInformation(">>> SqsConsumerWorker stopped.");
    }
}
