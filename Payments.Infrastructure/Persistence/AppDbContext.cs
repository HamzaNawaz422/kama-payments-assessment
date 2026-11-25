using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Payments.Domain.PaymentAggregate;
using Payments.Domain.ProviderAggregate;
using Payments.Infrastructure.Identity;

namespace Payments.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentStatusChange> PaymentStatusChanges => Set<PaymentStatusChange>();
    public DbSet<PaymentProvider> PaymentProviders => Set<PaymentProvider>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    // All DateTime values are stored and returned as UTC
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();
    }
}

internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v,
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));