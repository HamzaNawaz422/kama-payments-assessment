using Payments.Application.Abstractions;
using Payments.Application.Common;

namespace Payments.Application.Payments;

public sealed class GetPaymentHandler(IPaymentQueries queries, ICurrentUser currentUser)
{
    public async Task<PaymentDetailsDto> HandleAsync(Guid id, CancellationToken ct)
    {
        var payment = await queries.GetByIdAsync(id, ct);

        // Another user's payment returns 404 (not 403), so we don't even reveal that it exists
        if (payment is null || (!currentUser.IsAdmin && payment.UserId != currentUser.UserId))
            throw new NotFoundException("Payment not found.");

        return payment;
    }
}