using System.Security.Claims;
using Payments.Application.Abstractions;
using Payments.Application.Common;

namespace Payments.Api.Security;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User ?? throw new InvalidOperationException("No active HTTP context.");

    public Guid UserId =>
        Guid.TryParse(Principal.FindFirstValue(AppClaims.UserId), out var id)
            ? id
            : throw new UnauthorizedAccessException("User id claim is missing.");

    public bool IsAdmin => Principal.IsInRole(Roles.Admin);

    public bool HasPermission(string permission) => Principal.HasClaim(Permissions.ClaimType, permission);
}