using Microsoft.AspNetCore.Http;

namespace Agenda.API.Resources.Appointments.v1.Update;

using FastEndpoints;

using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Updates an appointment based on a PATCH document
/// </summary>
public class PatchAppointmentByIdEndpoint : FastEndpoints.Endpoint<JsonPatchDocument<PatchAppointmentRequest>>
{
    public override void Configure()
    {
        Patch("/appointments/id");
        AllowAnonymous();
    }

    public override async Task HandleAsync(JsonPatchDocument<PatchAppointmentRequest> req, CancellationToken ct)
    {
        await SendNotFoundAsync(ct);
    }
}