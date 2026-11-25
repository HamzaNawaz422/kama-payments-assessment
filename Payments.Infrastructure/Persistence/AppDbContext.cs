using Microsoft.EntityFrameworkCore;
using Payments.Domain.PaymentAggregate;
using Payments.Domain.ProviderAggregate;
using Payments.Infrastructure.Identity;
using System.Reflection.Emit;

namespace Payments.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentStatusChange> PaymentStatusChanges => Set<PaymentStatusChange>();
    public DbSet<PaymentProvider> PaymentProviders => Set<PaymentProvider>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}