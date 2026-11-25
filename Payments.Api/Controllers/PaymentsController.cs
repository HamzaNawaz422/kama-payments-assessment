using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.Application.Common;
using Payments.Application.Payments;
using Payments.Domain.PaymentAggregate;

namespace Payments.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
[Produces("application/json")]
public sealed class PaymentsController : ControllerBase
{
    /// <summary>Creates a payment request. Requires an Idempotency-Key header.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.PaymentsCreate)]
    [ProducesResponseType<PaymentDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<PaymentDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromServices] CreatePaymentHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, idempotencyKey, ct);

        // A retried request with the same key returns the original payment (200) instead of creating a new one
        return result.IsReplay
            ? Ok(result.Payment)
            : CreatedAtAction(nameof(GetById), new { id = result.Payment.Id }, result.Payment);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.PaymentsView)]
    [ProducesResponseType<PaymentDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, [FromServices] GetPaymentHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(id, ct));

    /// <summary>Users see their own payments; admins see all (optionally filtered by userId).</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.PaymentsView)]
    [ProducesResponseType<PagedResult<PaymentSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? userId,
        [FromQuery] PaymentStatus? status,
        [FromServices] ListPaymentsHandler handler,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await handler.HandleAsync(userId, status, page, pageSize, ct));

    /// <summary>Admin only: manually re-triggers a failed payment.</summary>
    [HttpPost("{id:guid}/retry")]
    [Authorize(Policy = Permissions.PaymentsProcess)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retry(Guid id, [FromServices] RetryPaymentHandler handler, CancellationToken ct)
    {
        await handler.HandleAsync(id, ct);
        return Accepted();
    }
}