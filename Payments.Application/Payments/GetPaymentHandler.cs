using Payments.Application.Abstractions;
using Payments.Application.Common;

namespace Payments.Application.Payments;

public sealed class GetPaymentHandler(IPaymentQueries queries, ICurrentUser currentUser)
{
    public async Task<PaymentDetailsDto> HandleAsync(Guid id, CancellationToken ct)
    {
        var payment = await queries.GetByIdAsync(id, ct);

        
        if (payment is null || (!currentUser.IsAdmin && payment.UserId != currentUser.UserId))
            throw new NotFoundException("Payment not found.");

        return payment;
    }
}