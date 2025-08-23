using Agenda.Ids;
using JetBrains.Annotations;

namespace Agenda.API.Resources.Appointments.v1.GetById;


/// <summary>
/// Request to get an appointment by its identifier.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class GetByIdRequest
{
    /// <summary>
    /// Identifier of the appointment to get.
    /// </summary>
    public AppointmentId Id { get; init; }
}