using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payments.Domain.PaymentAggregate;
using Payments.Domain.ProviderAggregate;

namespace Payments.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.ComplexProperty(p => p.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsFixedLength().IsRequired();
        });

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(p => p.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(p => p.ProviderReference).HasMaxLength(100);
        builder.Property(p => p.FailureReason).HasMaxLength(500);

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasOne<PaymentProvider>()
            .WithMany()
            .HasForeignKey(p => p.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.StatusHistory)
            .WithOne()
            .HasForeignKey("PaymentId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.StatusHistory)
            .HasField("_statusHistory")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.DomainEvents);
        builder.Ignore(p => p.CanBeAutoRetried);

        builder.HasIndex(p => new { p.UserId, p.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_Payments_UserId_IdempotencyKey");

        builder.HasIndex(p => new { p.UserId, p.Status, p.CreatedAtUtc })
            .HasDatabaseName("IX_Payments_UserId_Status_CreatedAtUtc");

        builder.HasIndex(p => new { p.Status, p.UpdatedAtUtc })
            .HasDatabaseName("IX_Payments_Status_UpdatedAtUtc");

        builder.HasIndex(p => new { p.CreatedAtUtc, p.ProviderId })
            .HasDatabaseName("IX_Payments_CreatedAtUtc_ProviderId");
    }
}