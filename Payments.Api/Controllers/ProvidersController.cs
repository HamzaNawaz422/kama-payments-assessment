using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.Application.Common;
using Payments.Application.Providers;

namespace Payments.Api.Controllers;

[ApiController]
[Route("api/v1/providers")]
[Authorize]
[Produces("application/json")]
public sealed class ProvidersController : ControllerBase
{
    /// <summary>Any authenticated user can list providers (needed to choose one when paying).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProviderDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List([FromServices] ListProvidersHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(ct));

    [HttpPost]
    [Authorize(Policy = Permissions.ProvidersManage)]
    [ProducesResponseType<ProviderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateProviderRequest request, [FromServices] CreateProviderHandler handler, CancellationToken ct)
    {
        var provider = await handler.HandleAsync(request, ct);
        return Created($"/api/v1/providers/{provider.Id}", provider);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.ProvidersManage)]
    [ProducesResponseType<ProviderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, UpdateProviderRequest request, [FromServices] UpdateProviderHandler handler, CancellationToken ct)
        => Ok(await handler.HandleAsync(id, request, ct));
}