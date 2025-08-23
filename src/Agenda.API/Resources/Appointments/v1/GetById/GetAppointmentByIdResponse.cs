using Agenda.Ids;
using NodaTime;

namespace Agenda.API.Resources.v1.Appointments;

/// <summary>
/// An appointment between two or more <see cref="AttendeeInfo">attendees</see>.
/// </summary>
public class GetAppointmentByIdResponse : Resource<AppointmentId>
{
    /// <summary>
    /// Location of the appointment
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// Subject of the appointment
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// Start date of the appointment
    /// </summary>
    public ZonedDateTime StartDate { get; set; }

    /// <summary>
    /// End date of the appointment
    /// </summary>
    public ZonedDateTime EndDate { get; set; }

    /// <summary>
    /// Defines who initiated the appointment
    /// </summary>
    public AttendeeInfo Iniator { get; set; }
}