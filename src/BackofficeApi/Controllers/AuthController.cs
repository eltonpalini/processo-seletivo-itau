using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FraudMonitor.BackofficeApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace FraudMonitor.BackofficeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Generates a JWT token for fraud analysts to access protected endpoints.
    /// Default credentials for testing in PoC: analyst / itau@2026 or admin / admin123
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public IActionResult GenerateToken([FromBody] LoginRequest request)
    {
        var isValidUser = (request.Username == "analyst" && request.Password == "itau@2026") ||
                          (request.Username == "admin" && request.Password == "admin123");

        if (!isValidUser)
        {
            _logger.LogWarning("[AUTH_FAILED] Invalid credentials attempt for username: {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var secretKey = _configuration["JwtSettings:SecretKey"] ?? "Itau_FraudMonitor_Secret_Key_Super_Secure_2026_JWT_Token_Key!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "FraudMonitor.BackofficeApi";
        var audience = _configuration["JwtSettings:Audience"] ?? "FraudMonitor.Backoffice";
        var expirationMinutes = int.TryParse(_configuration["JwtSettings:ExpirationInMinutes"], out var exp) ? exp : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.Role, "FraudAnalyst")
        };

        var expires = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenString = tokenHandler.WriteToken(token);

        _logger.LogInformation("[AUTH_SUCCESS] JWT issued for user: {Username} with role FraudAnalyst", request.Username);

        return Ok(new LoginResponse(
            AccessToken: tokenString,
            TokenType: "Bearer",
            ExpiresInSeconds: expirationMinutes * 60,
            Username: request.Username,
            Role: "FraudAnalyst"
        ));
    }
}
