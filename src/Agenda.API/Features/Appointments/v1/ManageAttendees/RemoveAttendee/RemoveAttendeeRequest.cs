using Agenda.Ids;

namespace Agenda.API.Features.Appointments.v1.ManageAttendees.RemoveAttendee
{
    public record RemoveAttendeeRequest
    {
        /// <summary>
        /// Identifier of the attendee to remove.
        /// </summary>
        public required AttendeeId AttendeeId { get; init; }

        /// <summary>
        /// Identifier of the appointment from which to remove the attendee.
        /// </summary>
        public required AppointmentId Id { get; init; }
    }
}