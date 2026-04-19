using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Woody.Application.Constants;
using Woody.Application.Interfaces.Security;
using Woody.Application.Mapping;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Subscription;

namespace Woody.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _jwtOptions = options.Value;
    }

    public string GenerateToken(User user, UserSubscription? subscription)
    {
        var utcNow = DateTime.UtcNow;
        var effectivePlan = SubscriptionEntitlement.HasActiveProBenefits(subscription, utcNow) ? "pro" : "free";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(WoodyClaims.Plan, effectivePlan),
            new(WoodyClaims.SubscriptionStatus, SubscriptionDtoMapper.ToApiStatus(subscription?.Status ?? SubscriptionStatus.Active)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
