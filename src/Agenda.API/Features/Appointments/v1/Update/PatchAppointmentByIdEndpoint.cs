using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Agenda.Ids;
using FastEndpoints;
using Fluxera.StronglyTypedId;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SystemTextJsonPatch;

namespace Agenda.API.Features.Appointments.v1.Update;

/// <summary>
/// Updates an appointment based on a PATCH document
/// </summary>
public class PatchAppointmentByIdEndpoint : Endpoint<PatchRequest<AppointmentId ,PatchAppointmentRequest>, Results<NoContent, ProblemDetails>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/appointments/{id}");
        AllowAnonymous();
        Validator<PatchAppointmentInfoRequestValidator>();
        SerializerContext<PatchAppointmentEndpointSerializerContext>();

    }

    /// <inheritdoc />
    public override async Task<Results<NoContent, ProblemDetails>> ExecuteAsync(PatchRequest<AppointmentId, PatchAppointmentRequest> req, CancellationToken ct)
        => TypedResults.NoContent();
}