using Agenda.Ids;

namespace Agenda.API.Resources.Appointments.v1.Delete;

/// <summary>
/// Request to delete an appointment by its identifier
/// </summary>
/// <param name="Id">Identifier of the appointment to delete</param>
public record DeleteByIdRequest(AppointmentId Id);