using System;
using System.Collections.Generic;
using Agenda.Ids;
using Paramore.Brighter;

namespace Agenda.Events;

/// <summary>
/// Event raised when an appointment is scheduled.
/// </summary>
public class AppointmentScheduled : Event
{
    /// <summary>
    /// Builds a new <see cref="AppointmentScheduled"/> instance.
    /// </summary>
    /// <param name="appointmentId">Unique identifier of the appointment</param>
    /// <param name="subject"></param>
    /// <param name="location"></param>
    /// <param name="participants"></param>
    public AppointmentScheduled(AppointmentId appointmentId, string subject, string location, IReadOnlyList<Attendee> participants) : base(Guid.NewGuid())
    {
        AppointmentId = appointmentId;
        Subject = subject;
        Location = location;
        Participants = participants;
    }

    /// <summary>
    /// Id of the appointment.
    /// </summary>
    public AppointmentId AppointmentId { get; init; }

    /// <summary>
    /// Subject of the appointment.
    /// </summary>
    public string Subject { get; init; }

    /// <summary>
    /// Location of the appointment.
    /// </summary>
    public string Location { get; init; }

    /// <summary>
    /// Participants of the appointment.
    /// </summary>
    public IReadOnlyList<Attendee> Participants { get; init; }
}