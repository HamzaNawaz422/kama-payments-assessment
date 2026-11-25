using System.Text.Json.Serialization;
using Payments.Api.ErrorHandling;
using Payments.Api.OpenApi;
using Payments.Api.Security;
using Payments.Application;
using Payments.Infrastructure;
using Payments.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiSecurity(builder.Configuration);

// API
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();   // 401/403 responses also become ProblemDetails

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Kama Payments API v1");
        options.EnablePersistAuthorization();
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Applies migrations (development only) and seeds the test users
await DatabaseInitializer.InitializeAsync(app.Services, applyMigrations: app.Environment.IsDevelopment());

app.Run();