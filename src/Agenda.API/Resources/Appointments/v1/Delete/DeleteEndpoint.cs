namespace Agenda.API.Resources.Appointments.v1.Delete;

using Agenda.Ids;
using Agenda.Objects;

using Ardalis.ApiEndpoints;

using Candoumbe.DataAccess.Abstractions;

using Microsoft.AspNetCore.Mvc;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Deletes an appointment by its identifier
/// </summary>
public class DeleteEndpoint : EndpointBaseAsync.WithRequest<AppointmentId>
    .WithActionResult
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    /// <summary>
    /// Builds a new <see cref="DeleteEndpoint"/> instance.
    /// </summary>
    /// <param name="unitOfWorkFactory"></param>
    public DeleteEndpoint(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    /// <summary>
    /// Deletes an appointment
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpDelete("/appointments/{id}", Name = nameof(DeleteEndpoint))]
    public override async Task<ActionResult> HandleAsync([FromRoute] AppointmentId id, CancellationToken ct)
    {
        using IUnitOfWork unitOfWork = _unitOfWorkFactory.NewUnitOfWork();

        IRepository<Appointment> repository = unitOfWork.Repository<Appointment>();
        ActionResult actionResult;
        if (await repository.Any(appointment => appointment.Id == id, ct))
        {
            await repository.Delete(appointment => appointment.Id == id, ct);
            await unitOfWork.SaveChangesAsync(ct);
            actionResult = new NoContentResult();
        }
        else
        {
            actionResult = new NotFoundResult();
        }

        return actionResult;
    }
}