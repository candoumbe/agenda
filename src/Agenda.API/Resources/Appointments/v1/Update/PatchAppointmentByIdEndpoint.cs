using FastEndpoints;
using Microsoft.AspNetCore.JsonPatch.Operations;

namespace Agenda.API.Resources.Appointments.v1.Update;

/// <summary>
/// Updates an appointment based on a PATCH document
/// </summary>
public class PatchAppointmentByIdEndpoint : Endpoint<List<Operation<PatchAppointmentRequest>>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/appointments/{id}");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override Task HandleAsync(List<Operation<PatchAppointmentRequest>> req, CancellationToken ct)
        => Task.FromResult(TypedResults.StatusCode(StatusCodes.Status501NotImplemented));
}