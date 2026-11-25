using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.Application.Abstractions;
using Payments.Application.Auth;

namespace Payments.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);

        return result is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password")
            : Ok(result);
    }
}