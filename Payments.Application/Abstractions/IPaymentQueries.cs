using Payments.Application.Common;
using Payments.Application.Payments;
using Payments.Domain.PaymentAggregate;

namespace Payments.Application.Abstractions;


public interface IPaymentQueries
{
    Task<PaymentDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<PagedResult<PaymentSummaryDto>> ListAsync(
        Guid? userId, PaymentStatus? status, int page, int pageSize, CancellationToken ct);
}