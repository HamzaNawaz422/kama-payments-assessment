namespace Payments.Application.Abstractions;

/// <summary>The authenticated caller. Implemented in the API layer from the JWT claims.</summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsAdmin { get; }
    bool HasPermission(string permission);
}