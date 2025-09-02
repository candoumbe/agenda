using Agenda.Ids;

namespace Agenda.API.Features.Appointments.v1.GetById;


/// <summary>
/// Request to get an appointment by its identifier.
/// </summary>
public sealed class GetByIdRequest
{
    /// <summary>
    /// Identifier of the appointment to get.
    /// </summary>
    public AppointmentId Id { get; init; }
}