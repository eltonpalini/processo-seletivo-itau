using FraudMonitor.Application.Interfaces.Provider;
using FraudMonitor.Application.Interfaces.Queue;
using FraudMonitor.Application.Interfaces.Repository;
using FraudMonitor.Infrastructure.Messaging;
using FraudMonitor.Infrastructure.Persistence;
using FraudMonitor.Infrastructure.Repositories;
using FraudMonitor.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FraudMonitor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Singleton in-memory store simulating DynamoDB and Redis
        services.AddSingleton<InMemoryTransactionStore>();

        // SQS simulated asynchronous queue
        services.AddSingleton<ITransactionQueue, InMemoryTransactionQueue>();

        // Contracts registration
        services.AddSingleton<ICustomerHistoryProvider, CustomerHistoryProvider>();
        services.AddSingleton<ITransactionWriteRepository, TransactionWriteRepository>();
        services.AddSingleton<ITransactionReadRepository, TransactionReadRepository>();

        return services;
    }
}
