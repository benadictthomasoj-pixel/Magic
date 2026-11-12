namespace Magic.Domain.Common;

/// <summary>
/// Base exception thrown when an authoritative business invariant is violated in the domain layer.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string errorCode, string message, int httpStatusCode = 422)
        : base(message)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }

    public DomainException(string errorCode, string message, Exception innerException, int httpStatusCode = 422)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }

    public string ErrorCode { get; }
    public int HttpStatusCode { get; }
}

