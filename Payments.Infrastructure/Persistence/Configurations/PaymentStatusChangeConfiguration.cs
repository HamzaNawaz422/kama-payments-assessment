using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payments.Domain.PaymentAggregate;

namespace Payments.Infrastructure.Persistence.Configurations;

public sealed class PaymentStatusChangeConfiguration : IEntityTypeConfiguration<PaymentStatusChange>
{
    public void Configure(EntityTypeBuilder<PaymentStatusChange> builder)
    {
        builder.ToTable("PaymentStatusChanges");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.ToStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.ChangedBy).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Note).HasMaxLength(500);
    }
}