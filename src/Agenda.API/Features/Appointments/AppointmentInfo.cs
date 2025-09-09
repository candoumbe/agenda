using System.Collections.Generic;
using Agenda.API.Features.v1.Appointments;
using Agenda.Ids;
using NodaTime;

namespace Agenda.API.Features.Appointments;

/// <summary>
/// An appointment between two or more people
/// </summary>
public record AppointmentInfo : Resource<AppointmentId>
{
    /// <summary>
    /// Location of the appointment
    /// </summary>
    public string Location { get; init; }

    /// <summary>
    /// Subject of the appointment
    /// </summary>
    public string Subject { get; init; }

    /// <summary>
    /// Start date of the appointment
    /// </summary>
    public OffsetDateTime StartDate { get; init; }

    /// <summary>
    /// End date of the appointment
    /// </summary>
    public OffsetDateTime EndDate { get; init; }

    /// <summary>
    /// Defines who initiated the appointment
    /// </summary>
    public AttendeeInfo Iniator { get; init; }

    /// <summary>
    /// Participants
    /// </summary>
    public IEnumerable<AttendeeInfo> Attendees { get; init; }
}