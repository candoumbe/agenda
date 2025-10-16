using System;
using System.Collections.Generic;
using Agenda.Ids;
using Paramore.Brighter;

namespace Agenda.Events;

/// <summary>
/// Event raised when an appointment is scheduled.
/// </summary>
/// <summary>
/// Builds a new <see cref="AppointmentScheduled"/> instance.
/// </summary>
/// <param name="appointmentId">Unique identifier of the appointment</param>
/// <param name="subject"></param>
/// <param name="location"></param>
/// <param name="participants"></param>
public class AppointmentScheduled(AppointmentId appointmentId, string subject, string location, IReadOnlyList<Attendee> participants)  : Event(Guid.NewGuid())
{
    /// <summary>
    /// Unique identifier of the appointment.
    /// </summary>
    public AppointmentId AppointmentId { get; } = appointmentId;

    /// <summary>
    /// Subject of the appointment.
    /// </summary>
    public string Subject { get; } = subject ?? throw new ArgumentNullException(nameof(subject));

    /// <summary>
    /// Location of the appointment.
    /// </summary>
    public string Location { get; } = location ?? throw new ArgumentNullException(nameof(location));

    /// <summary>
    /// Participants of the appointment.
    /// </summary>
    public IReadOnlyList<Attendee> Participants { get; } = participants ?? throw new ArgumentNullException(nameof(participants));
}