using System;

namespace Agenda.Objects;

/// <summary>
/// Exception thrown when an email is invalid.
/// </summary>
public class InvalidEmailException : Exception
{
    /// <inheritdoc />
    public InvalidEmailException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public InvalidEmailException() : base()
    {
    }

    /// <inheritdoc />
    public InvalidEmailException(string message, Exception innerException) : base(message, innerException)
    {
    }
}