using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Agenda.Ids;
using FastEndpoints;

namespace Agenda.API.Features.Appointments.v1.Update;

/// <summary>
/// Source generated serializer context used by <see cref="PatchAppointmentByIdEndpoint"/>.
/// </summary>
[JsonSerializable(typeof(PatchRequest<AppointmentId, PatchAppointmentRequest>))]
[JsonSerializable(typeof(ProblemDetails))]
[ExcludeFromCodeCoverage]
public partial class PatchAppointmentEndpointSerializerContext : JsonSerializerContext;