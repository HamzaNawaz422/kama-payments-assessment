using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


var processed = new ConcurrentDictionary<Guid, ChargeResponse>();

app.MapPost("/api/charges", async (ChargeRequest request, HttpRequest http, ILogger<Program> logger, CancellationToken ct) =>
{
    var key = Guid.TryParse(http.Headers["Idempotency-Key"], out var headerKey) ? headerKey : request.PaymentId;

    if (processed.TryGetValue(key, out var previous))
    {
        logger.LogInformation("Idempotent replay for {Key}", key);
        return Results.Ok(previous);
    }

    await Task.Delay(Random.Shared.Next(200, 1200), ct);   

    if (request.Amount == 503m)
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);   

    if (request.Amount == 408m)
        await Task.Delay(TimeSpan.FromSeconds(30), ct);                       

    var roll = Random.Shared.Next(100);

    if (roll < 20)
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);   

    var response = request.Amount > 10_000m
        ? new ChargeResponse("Declined", null, "Amount exceeds provider limit")
        : roll < 30
            ? new ChargeResponse("Declined", null, "Card declined")
            : new ChargeResponse("Approved", $"FP-{Guid.NewGuid():N}"[..15], null);

    processed[key] = response;
    logger.LogInformation("Charge {Key}: {Status}", key, response.Status);
    return Results.Ok(response);
});

app.Run();

public sealed record ChargeRequest(Guid PaymentId, string ProviderCode, decimal Amount, string Currency);

public sealed record ChargeResponse(string Status, string? Reference, string? Reason);