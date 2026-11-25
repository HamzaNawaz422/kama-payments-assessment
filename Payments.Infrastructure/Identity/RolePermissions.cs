using Payments.Application.Common;

namespace Payments.Infrastructure.Identity;

public static class RolePermissions
{
    private static readonly Dictionary<string, string[]> Map = new()
    {
        [Roles.Admin] = Permissions.All,
        [Roles.User] = [Permissions.PaymentsCreate, Permissions.PaymentsView]
    };

    public static IReadOnlyList<string> For(string role)
        => Map.TryGetValue(role, out var permissions) ? permissions : [];
}