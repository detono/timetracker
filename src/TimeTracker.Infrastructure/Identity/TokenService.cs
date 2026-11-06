using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TimeTracker.Application.Common.Interfaces;
using TimeTracker.Application.Common.Security;
using TimeTracker.Domain.Entities;

namespace TimeTracker.Infrastructure.Identity;

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly IDateTimeProvider _clock;

    public TokenService(IOptions<JwtSettings> settings, IDateTimeProvider clock)
    {
        _settings = settings.Value;
        _clock = clock;
    }

    public IssuedRefreshToken CreateRefreshToken(User user, Guid familyId)
    {
        // 256 random bits: far too many to guess, so a fast hash is enough to store it safely.
        var token = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var entity = RefreshToken.Issue(
            user, HashRefreshToken(token), familyId, _clock.UtcNow, TimeSpan.FromDays(_settings.RefreshTokenDays));
        return new IssuedRefreshToken(token, entity);
    }

    public string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(SessionClaimTypes.SessionVersion, user.SessionVersion.ToString(), ClaimValueTypes.Integer32)
        };

        if (user.MustChangePassword)
        {
            claims.Add(new Claim(SessionClaimTypes.MustChangePassword, "true"));
        }

        if (user.TenantId.HasValue)
        {
            claims.Add(new Claim(TenantClaimTypes.TenantId, user.TenantId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
