using System;

namespace Agenda.Events;

/// <summary>
/// Attendee of an appointment.
/// </summary>
public record Attendee
{
    /// <summary>
    /// Id of the attendee.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// First name of the attendee.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// Last name of the attendee.
    /// </summary>
    public required string LastName { get; init; }
}