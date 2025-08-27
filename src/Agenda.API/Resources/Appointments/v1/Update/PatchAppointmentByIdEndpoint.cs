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
        Patch("/appointments/id");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(List<Operation<PatchAppointmentRequest>> req, CancellationToken ct)
        => TypedResults.NotFound();
}