using Payments.Application.Abstractions;
using Payments.Application.Common;
using Payments.Domain.PaymentAggregate;

namespace Payments.Application.Payments;

public sealed class ListPaymentsHandler(IPaymentQueries queries, ICurrentUser currentUser)
{
    private const int MaxPageSize = 100;

    public Task<PagedResult<PaymentSummaryDto>> HandleAsync(
        Guid? userIdFilter, PaymentStatus? status, int page, int pageSize, CancellationToken ct)
    {
        
        var userId = currentUser.IsAdmin ? userIdFilter : currentUser.UserId;

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        return queries.ListAsync(userId, status, page, pageSize, ct);
    }
}