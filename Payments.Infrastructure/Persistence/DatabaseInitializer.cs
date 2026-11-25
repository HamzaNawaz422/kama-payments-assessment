using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payments.Application.Common;
using Payments.Infrastructure.Identity;

namespace Payments.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static readonly Guid AdminId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid UserId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    public static readonly Guid User2Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");

    public static async Task InitializeAsync(IServiceProvider services, bool applyMigrations)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

        if (applyMigrations)
            await db.Database.MigrateAsync();

        await EnsureUserAsync(db, hasher, AdminId, "admin@kama.test", "Admin@123", Roles.Admin);
        await EnsureUserAsync(db, hasher, UserId, "user@kama.test", "User@123", Roles.User);
        await EnsureUserAsync(db, hasher, User2Id, "user2@kama.test", "User@123", Roles.User);

        await db.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(
        AppDbContext db, IPasswordHasher<AppUser> hasher, Guid id, string email, string password, string role)
    {
        if (await db.Users.AnyAsync(u => u.Email == email))
            return;

        var user = new AppUser { Id = id, Email = email, Role = role };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
    }
}