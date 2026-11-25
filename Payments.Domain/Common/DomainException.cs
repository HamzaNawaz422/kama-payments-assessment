namespace Payments.Domain.Common;

/// <summary>
/// Thrown when a business rule is broken. The API maps it to a ProblemDetails response.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}