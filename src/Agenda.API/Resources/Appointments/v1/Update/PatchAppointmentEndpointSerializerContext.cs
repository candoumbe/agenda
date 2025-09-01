using System.Text.Json.Serialization;
using Agenda.Ids;
using FastEndpoints;

namespace Agenda.API.Resources.Appointments.v1.Update;

/// <summary>
/// Source generated serializer context used by <see cref="PatchAppointmentByIdEndpoint"/>.
/// </summary>
[JsonSerializable(typeof(PatchRequest<AppointmentId, PatchAppointmentRequest>))]
[JsonSerializable(typeof(ProblemDetails))]
public partial class PatchAppointmentEndpointSerializerContext : JsonSerializerContext;