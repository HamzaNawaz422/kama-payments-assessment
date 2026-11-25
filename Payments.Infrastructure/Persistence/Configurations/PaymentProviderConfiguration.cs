using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payments.Domain.ProviderAggregate;

namespace Payments.Infrastructure.Persistence.Configurations;

public sealed class PaymentProviderConfiguration : IEntityTypeConfiguration<PaymentProvider>
{
    public static readonly Guid SeededProviderId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public void Configure(EntityTypeBuilder<PaymentProvider> builder)
    {
        builder.ToTable("PaymentProviders");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();

        builder.HasIndex(p => p.Code).IsUnique().HasDatabaseName("UX_PaymentProviders_Code");

        builder.Ignore(p => p.DomainEvents);

        builder.HasData(new
        {
            Id = SeededProviderId,
            Name = "Fake Pay",
            Code = "FAKEPAY",
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}