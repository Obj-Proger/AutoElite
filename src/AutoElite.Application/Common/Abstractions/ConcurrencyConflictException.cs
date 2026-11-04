namespace AutoElite.Application.Common.Abstractions;

/// <summary>
/// Thrown by <see cref="IUnitOfWork.SaveChangesAsync"/> when another request changed or deleted a record
/// after this request loaded it. The pipeline turns it into a conflict error, so the client can reload and retry.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    /// <summary>Initializes a new instance with a default message.</summary>
    public ConcurrencyConflictException()
        : base("The record was changed by another request after it was loaded.")
    {
    }

    /// <summary>Initializes a new instance with the specified message.</summary>
    /// <param name="message">The message that describes the conflict.</param>
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with the specified message and the exception that caused it.</summary>
    /// <param name="message">The message that describes the conflict.</param>
    /// <param name="innerException">The data access exception that reported the conflict.</param>
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
