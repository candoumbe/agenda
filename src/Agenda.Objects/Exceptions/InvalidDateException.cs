
using System;

namespace Agenda.Objects.Exceptions;
/// <summary>
/// Exception thrown when passing invalid start/end date when creating/modifying an appointment
/// </summary>
public class InvalidDateException : Exception
{
    ///<inheritdoc/>
    public InvalidDateException(string message) : base(message)
    {

    }

    ///<inheritdoc/>
    public InvalidDateException()
    {
    }

    ///<inheritdoc/>
    public InvalidDateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}