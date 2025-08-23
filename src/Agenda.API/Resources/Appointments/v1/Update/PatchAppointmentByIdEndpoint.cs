using FastEndpoints;
using Microsoft.AspNetCore.JsonPatch;

namespace Agenda.API.Resources.Appointments.v1.Update;

/// <summary>
/// Updates an appointment based on a PATCH document
/// </summary>
public class PatchAppointmentByIdEndpoint : Endpoint<JsonPatchDocument<PatchAppointmentRequest>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/appointments/id");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(JsonPatchDocument<PatchAppointmentRequest> req, CancellationToken ct)
        => TypedResults.NotFound();
}