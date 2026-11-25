using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.Application.Abstractions;
using Payments.Application.Common;
using Payments.Application.Reports;

namespace Payments.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public sealed class ReportsController : ControllerBase
{
    private readonly GetDailyReportHandler _getDailyReportHandler;

    public ReportsController(GetDailyReportHandler _getDailyReportHandler)
    {
        this._getDailyReportHandler = _getDailyReportHandler;
    }

    [HttpGet("daily-payments")]
    [ProducesResponseType<IReadOnlyList<DailyPaymentReportRow>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DailyPayments(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(await this._getDailyReportHandler.HandleAsync(from, to, ct));
}