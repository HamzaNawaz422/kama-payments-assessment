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
    
    private readonly ListProvidersHandler _listProvidersHandler;
    private readonly CreateProviderHandler _createProviderHandler;
    private readonly UpdateProviderHandler _updateProviderHandler;
    

    public ProvidersController(ListProvidersHandler _listProvidersHandler, 
        CreateProviderHandler _createProviderHandler, UpdateProviderHandler _updateProviderHandler)
    {
        this._listProvidersHandler = _listProvidersHandler;
        this._createProviderHandler= _createProviderHandler;
        this._updateProviderHandler= _updateProviderHandler;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProviderDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await this._listProvidersHandler.HandleAsync(ct));

    [HttpPost]
    [Authorize(Policy = Permissions.ProvidersManage)]
    [ProducesResponseType<ProviderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateProviderRequest request, CancellationToken ct)
    {
        var provider = await this._createProviderHandler.HandleAsync(request, ct);
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
        Guid id, UpdateProviderRequest request, CancellationToken ct)
        => Ok(await this._updateProviderHandler.HandleAsync(id, request, ct));
}