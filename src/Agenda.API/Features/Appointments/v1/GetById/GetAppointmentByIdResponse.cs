using Agenda.API.Features.Appointments;
using Agenda.Ids;
using NodaTime;

namespace Agenda.API.Features.v1.Appointments;

/// <summary>
/// An appointment between two or more <see cref="AttendeeInfo">attendees</see>.
/// </summary>
public record GetAppointmentByIdResponse : Resource<AppointmentId>
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
    public ZonedDateTime StartDate { get; init; }

    /// <summary>
    /// End date of the appointment
    /// </summary>
    public ZonedDateTime EndDate { get; init; }

    /// <summary>
    /// Defines who initiated the appointment
    /// </summary>
    public AttendeeInfo Iniator { get; init; }
}