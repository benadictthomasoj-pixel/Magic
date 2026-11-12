using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Magic.Contracts.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Magic.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Magic.Api";
    public string Audience { get; set; } = "Magic.Client";
    public string SecretKey { get; set; } = "MAGIC_ENTERPRISE_DEV_SECRET_KEY_REPLACE_IN_PRODUCTION_0123456789";
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 7;
}

public interface IJwtTokenService : ITokenService
{
    string IssueAccessToken(
        Guid userId,
        string username,
        string email,
        Guid companyId,
        Guid? branchId,
        Guid? terminalId,
        IEnumerable<string> roles,
        IEnumerable<string> permissions);

    (string RawToken, string TokenHash, DateTime ExpiresAtUtc) GenerateRefreshToken();
    string HashToken(string rawToken);
    TokenValidationParameters GetValidationParameters();
}

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public bool IsConfigured => true;

    public string IssueAccessToken(
        Guid userId,
        string username,
        string email,
        Guid companyId,
        Guid? branchId,
        Guid? terminalId,
        IEnumerable<string> roles,
        IEnumerable<string> permissions)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("company_id", companyId.ToString())
        };

        if (branchId.HasValue && branchId.Value != Guid.Empty)
        {
            claims.Add(new("branch_id", branchId.Value.ToString()));
        }

        if (terminalId.HasValue && terminalId.Value != Guid.Empty)
        {
            claims.Add(new("terminal_id", terminalId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new(ClaimTypes.Role, role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new("permission", permission));
        }

        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = credentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public Task<TokenValidationOutcome> ValidateAccessTokenAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return Task.FromResult(new TokenValidationOutcome
            {
                IsValid = false,
                FailureReason = "Token is missing or empty."
            });
        }

        try
        {
            var parameters = GetValidationParameters();
            var principal = _tokenHandler.ValidateToken(rawToken, parameters, out var validatedToken);

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Task.FromResult(new TokenValidationOutcome
                {
                    IsValid = false,
                    FailureReason = "Token sub claim is missing or invalid GUID."
                });
            }

            var username = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                ?? principal.FindFirst(ClaimTypes.Name)?.Value
                ?? string.Empty;

            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList();
            var permissions = principal.FindAll("permission").Select(c => c.Value).Distinct().ToList();

            return Task.FromResult(new TokenValidationOutcome
            {
                IsValid = true,
                UserId = userId,
                Username = username,
                Roles = roles,
                Permissions = permissions
            });
        }
        catch (SecurityTokenExpiredException)
        {
            return Task.FromResult(new TokenValidationOutcome
            {
                IsValid = false,
                FailureReason = "Token has expired."
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TokenValidationOutcome
            {
                IsValid = false,
                FailureReason = $"Token validation failed: {ex.Message}"
            });
        }
    }

    public (string RawToken, string TokenHash, DateTime ExpiresAtUtc) GenerateRefreshToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
        string rawToken = Convert.ToBase64String(randomBytes);
        string tokenHash = HashToken(rawToken);
        DateTime expiresAtUtc = DateTime.UtcNow.AddDays(_options.RefreshTokenLifetimeDays);

        return (rawToken, tokenHash, expiresAtUtc);
    }

    public string HashToken(string rawToken)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(rawToken);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public TokenValidationParameters GetValidationParameters()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }
}

