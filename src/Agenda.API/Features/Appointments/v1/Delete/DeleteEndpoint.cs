using System.Threading;
using System.Threading.Tasks;
using Agenda.Objects;
using Candoumbe.DataAccess.Abstractions;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Agenda.API.Features.Appointments.v1.Delete;

/// <summary>
/// Deletes an appointment by its identifier
/// </summary>
public class DeleteEndpoint : Endpoint<DeleteByIdRequest, Results<NoContent, NotFound>>
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

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/appointments/{id}");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task<Results<NoContent, NotFound>> ExecuteAsync(DeleteByIdRequest req, CancellationToken ct)
    {
        using IUnitOfWork unitOfWork = _unitOfWorkFactory.NewUnitOfWork();

        IRepository<Appointment> repository = unitOfWork.Repository<Appointment>();
        Results<NoContent, NotFound> result;

        FilterSpecification<Appointment> filter = new(x => x.Id == req.Id);

        if (await repository.Any(filter, ct))
        {
            await repository.Delete(filter, ct);
            await unitOfWork.SaveChangesAsync(ct);

            result = TypedResults.NoContent();
        }
        else
        {
            result = TypedResults.NotFound();
        }

        return result;
    }
}