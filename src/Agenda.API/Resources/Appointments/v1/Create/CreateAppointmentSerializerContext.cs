using System.Text.Json.Serialization;
using FastEndpoints;

namespace Agenda.API.Resources.Appointments.v1.Create;

/// <summary>
/// Source generated serializer context for <see cref="NewAppointmentInfo"/>, <see cref="Browsable{T}"/> and <see cref="ProblemDetails"/>.
/// </summary>
[JsonSerializable(typeof(NewAppointmentInfo))]
[JsonSerializable(typeof(Browsable<AppointmentInfo>))]
[JsonSerializable(typeof(ProblemDetails))]
public partial class CreateAppointmentSerializerContext : JsonSerializerContext
{

}