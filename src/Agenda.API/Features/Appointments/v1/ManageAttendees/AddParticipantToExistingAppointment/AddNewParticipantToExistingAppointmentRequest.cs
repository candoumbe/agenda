using Agenda.Ids;

namespace Agenda.API.Features.Appointments.v1.ManageAttendees.AddParticipantToExistingAppointment;

/// <summary>
/// Request to add a new participant to an existing appointment.
/// </summary>
public record AddNewParticipantToExistingAppointmentRequest
{
    /// <summary>
    /// The new participant to add to the appointment.
    /// </summary>
    public required AttendeeInfo Participant { get; init; }

    /// <summary>
    /// Identifier of the appointment into which add a participant.
    /// </summary>
    public required AppointmentId Id { get; init; }
}