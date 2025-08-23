using Agenda.API.Resources.v1.Appointments;
using NodaTime;

namespace Agenda.API.Resources.Appointments.v1.Update;

/// <summary>
/// Request to partially modify an <see cref="AppointmentInfo"/>
/// </summary>
public record PatchAppointmentRequest
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
    /// When the appointment starts
    /// </summary>
    public ZonedDateTime? StartDate { get; init; }

    /// <summary>
    /// When the appointment ends
    /// </summary>
    public ZonedDateTime? EndDate { get; init; }
}