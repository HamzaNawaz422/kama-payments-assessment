namespace Payments.Application.Common;

public sealed class NotFoundException(string message) : Exception(message);        // → 404

public sealed class ConflictException(string message) : Exception(message);        // → 409

public sealed class ValidationException(string message) : Exception(message);      // → 400

public sealed class ConcurrencyException(string message) : Exception(message);     // → 409 / skipped in background