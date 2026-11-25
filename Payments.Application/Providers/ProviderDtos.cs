namespace Payments.Application.Providers;

public sealed record ProviderDto(Guid Id, string Name, string Code, bool IsActive, DateTime CreatedAtUtc);

public sealed record CreateProviderRequest(string Name, string Code);

public sealed record UpdateProviderRequest(string Name, bool IsActive);