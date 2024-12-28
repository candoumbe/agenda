using Microsoft.AspNetCore.Http;

namespace Agenda.API.Resources.Appointments.v1.Update;

using Ardalis.ApiEndpoints;

using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Updates an appointment based on a PATCH document
/// </summary>
public class PatchAppointmentByIdEndpoint : EndpointBaseAsync.WithRequest<JsonPatchDocument<PatchAppointmentRequest>>
    .WithActionResult
{
    ///<inheritdoc/>
    [HttpPatch("/appointments/id")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public override Task<ActionResult> HandleAsync(JsonPatchDocument<PatchAppointmentRequest> req, CancellationToken ct)
    {
        return Task.FromResult<ActionResult>(new NotFoundResult());
    }
}