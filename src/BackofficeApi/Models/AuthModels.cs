namespace FraudMonitor.BackofficeApi.Models;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresInSeconds,
    string Username,
    string Role
);
