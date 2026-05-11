using System;
using System.Collections.Generic;
using Agenda.Ids;
using NodaTime;
using Paramore.Brighter;

namespace Agenda.Events;

/// <summary>
/// Event raised when a new appointment is created.
/// </summary>
public class AppointmentCreated(AppointmentId appointmentId, Instant startDate, Instant endDate, string location, IReadOnlyList<Attendee> attendees, string creatorId)
#if NET10_0_OR_GREATER
    : Event(Guid.CreateVersion7())
#else
    : Event(Guid.NewGuid())
#endif
{
    /// <summary>
    /// Unique identifier of the appointment.
    /// </summary>
    public AppointmentId AppointmentId { get; } = appointmentId;

    /// <summary>
    /// Start date of the appointment in ISO 8601 format (UTC).
    /// </summary>
    public Instant StartDate { get; } = startDate;

    /// <summary>
    /// End date of the appointment in ISO 8601 format (UTC).
    /// </summary>
    public Instant EndDate { get; } = endDate;

    /// <summary>
    /// Location of the appointment.
    /// </summary>
    public string Location { get; } = location ?? throw new ArgumentNullException(nameof(location));

    /// <summary>
    /// Attendees of the appointment.
    /// </summary>
    public IReadOnlyList<Attendee> Attendees { get; } = attendees ?? throw new ArgumentNullException(nameof(attendees));

    /// <summary>
    /// Identifier of the user who created the appointment.
    /// </summary>
    public string CreatorId { get; } = creatorId ?? throw new ArgumentNullException(nameof(creatorId));
}
