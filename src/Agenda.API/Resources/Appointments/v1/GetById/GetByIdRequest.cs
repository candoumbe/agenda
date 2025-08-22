using Agenda.Ids;
namespace Agenda.API.Resources.Appointments.v1.GetById;


public sealed class GetByIdRequest
{
    public AppointmentId Id { get; set; }
}