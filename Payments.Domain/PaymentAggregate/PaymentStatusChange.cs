using Payments.Domain.Common;

namespace Payments.Domain.PaymentAggregate;


public sealed class PaymentStatusChange : Entity
{
    public PaymentStatus? FromStatus { get; private set; }
    public PaymentStatus ToStatus { get; private set; }
    public string ChangedBy { get; private set; } = default!;
    public string? Note { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }

    private PaymentStatusChange() { } 

    internal PaymentStatusChange(PaymentStatus? from, PaymentStatus to, string changedBy, string? note)
    {
        FromStatus = from;
        ToStatus = to;
        ChangedBy = changedBy;
        Note = note;
        ChangedAtUtc = DateTime.UtcNow;
    }
}