using System.Text;
using FraudMonitor.BackofficeApi.Queries;
using FraudMonitor.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting FraudMonitor Backoffice API host...");

    var builder = WebApplication.CreateBuilder(args);

    // Configures Serilog for Web Host
    builder.Host.UseSerilog();

    // 1. JWT Authentication Configuration
    var secretKey = builder.Configuration["JwtSettings:SecretKey"] ?? "Itau_FraudMonitor_Secret_Key_Super_Secure_2026_JWT_Token_Key!";
    var issuer = builder.Configuration["JwtSettings:Issuer"] ?? "FraudMonitor.BackofficeApi";
    var audience = builder.Configuration["JwtSettings:Audience"] ?? "FraudMonitor.Backoffice";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();

    // 2. Add Controllers and Swagger with Bearer Support
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "FraudMonitor Backoffice API",
            Version = "v1",
            Description = "Backoffice API for Fraud Analysts with JWT Authentication (Bearer)."
        });

        // Add JWT Bearer definition to Swagger UI
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Enter your JWT token obtained from POST /api/auth/token.\nExample: Bearer eyJhbGciOi...",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // 3. Infrastructure Layer (Repositories & Persistence)
    builder.Services.AddInfrastructureServices();

    // 4. CQRS Query Handlers
    builder.Services.AddScoped<GetFraudAlertsQueryHandler>();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "FraudMonitor Backoffice API v1");
        });
    }

    app.UseHttpsRedirection();

    // Authentication & Authorization middlewares
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "FraudMonitor Backoffice API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
