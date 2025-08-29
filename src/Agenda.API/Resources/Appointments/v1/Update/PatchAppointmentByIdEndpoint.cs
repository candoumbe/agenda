using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch.Operations;

namespace Agenda.API.Resources.Appointments.v1.Update;

/// <summary>
/// Updates an appointment based on a PATCH document
/// </summary>
public class PatchAppointmentByIdEndpoint : Endpoint<List<Operation<PatchAppointmentRequest>>, NoContent>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/appointments/{id}");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override Task<NoContent> ExecuteAsync(List<Operation<PatchAppointmentRequest>> req, CancellationToken ct)
        => Task.FromResult(TypedResults.NoContent());
}