using Payments.Application.Abstractions;
using Payments.Application.Common;
using Payments.Domain.Common;
using Payments.Domain.PaymentAggregate;

namespace Payments.Application.Payments;

public sealed class RetryPaymentHandler(
    IPaymentRepository payments,
    IUnitOfWork unitOfWork,
    IPaymentQueue queue,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(Guid paymentId, CancellationToken ct)
    {
        var payment = await payments.GetByIdAsync(paymentId, ct)
                      ?? throw new NotFoundException("Payment not found.");

        try
        {
            payment.RetryManually(currentUser.UserId);
        }
        catch (DomainException ex)
        {
            throw new ConflictException(ex.Message);   // e.g. payment is not Failed → 409
        }

        await unitOfWork.SaveChangesAsync(ct);
        await queue.EnqueueAsync(payment.Id, ct);
    }
}