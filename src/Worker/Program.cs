using FraudMonitor.Application;
using FraudMonitor.Infrastructure;
using FraudMonitor.Worker;
using FraudMonitor.Worker.Simulation;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting FraudMonitor Worker host...");

    var builder = Host.CreateApplicationBuilder(args);

    // Integrates Serilog into Microsoft Logging pipeline
    builder.Services.AddSerilog();

    // 1. Infrastructure layer (InMemory NoSQL Store, Fast Cache and Channels SQS Queue)
    builder.Services.AddInfrastructureServices();

    // 2. Application layer (Evaluation Engine + Dynamic Rules Auto-Discovery via Reflection - OCP)
    builder.Services.AddApplicationServices();

    // 3. Hosted Background Services (Consumer Worker + Real-Time Transaction Stream Simulator)
    builder.Services.AddHostedService<SqsConsumerWorker>();
    builder.Services.AddHostedService<TransactionStreamSimulator>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "FraudMonitor Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
