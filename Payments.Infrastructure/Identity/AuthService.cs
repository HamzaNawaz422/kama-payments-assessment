using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Payments.Application.Abstractions;
using Payments.Application.Auth;
using Payments.Application.Common;
using Payments.Infrastructure.Persistence;

namespace Payments.Infrastructure.Identity;

public sealed class AuthService(AppDbContext db, IPasswordHasher<AppUser> passwordHasher, JwtOptions jwtOptions)
    : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
            return null;

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        var permissions = RolePermissions.For(user.Role);
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(AppClaims.UserId, user.Id.ToString()),
            new(AppClaims.Email, user.Email),
            new(AppClaims.Role, user.Role)
        };
        claims.AddRange(permissions.Select(p => new Claim(Permissions.ClaimType, p)));

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        });

        return new LoginResponse(token, expiresAt, user.Role, permissions);
    }
}