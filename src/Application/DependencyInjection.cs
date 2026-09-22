using System.Reflection;
using FraudMonitor.Application.Interfaces.Strategy;
using FraudMonitor.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FraudMonitor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 1. Fraud Engine Orchestrator
        services.AddSingleton<FraudEvaluationService>();

        // 2. Dynamic auto-discovery of all fraud rules via Reflection (Assembly Scanning - OCP)
        var ruleInterfaceType = typeof(IFraudRuleStrategy);
        var ruleTypes = typeof(IFraudRuleStrategy).Assembly
            .GetTypes()
            .Where(t => ruleInterfaceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var ruleType in ruleTypes)
        {
            services.AddSingleton(ruleInterfaceType, ruleType);
        }

        return services;
    }
}
