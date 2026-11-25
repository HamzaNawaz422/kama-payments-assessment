namespace Payments.Application.Abstractions;

public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsAdmin { get; }
    bool HasPermission(string permission);
}