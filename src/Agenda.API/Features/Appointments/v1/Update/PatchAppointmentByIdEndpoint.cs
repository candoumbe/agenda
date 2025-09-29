using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Agenda.Ids;
using Agenda.Objects;
using Candoumbe.DataAccess.Abstractions;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Optional;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace Agenda.API.Features.Appointments.v1.Update;

/// <summary>
/// Updates an appointment based on a PATCH document
/// </summary>
public class PatchAppointmentByIdEndpoint : Endpoint<PatchRequest<AppointmentId, PatchAppointmentRequest>, Results<NoContent, NotFound, ProblemDetails>>
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public PatchAppointmentByIdEndpoint(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/appointments/{id}");
        AllowAnonymous();
        Validator<PatchAppointmentInfoRequestValidator>();
        SerializerContext<PatchAppointmentEndpointSerializerContext>();
    }

    /// <inheritdoc />
    public override async Task<Results<NoContent, NotFound, ProblemDetails>> ExecuteAsync(PatchRequest<AppointmentId, PatchAppointmentRequest> req, CancellationToken ct)
    {
        IFilterSpecification<Appointment> predicate = new FilterSpecification<Appointment>(a => a.Id == req.Id);
        using IUnitOfWork unitOfWork = _unitOfWorkFactory.NewUnitOfWork();
        Option<Appointment> maybeAppointment = await unitOfWork.Repository<Appointment>().SingleOrDefault(predicate, ct);

        return await maybeAppointment.Match<Task<Results<NoContent, NotFound, ProblemDetails>>>(some: async (existingAppointment) =>
            {
                Operation subjectOperation = req.Operations.Single(op => op.Path!.Equals("/subject", StringComparison.OrdinalIgnoreCase));
                existingAppointment.ChangeSubjectTo(subjectOperation.From);

                await unitOfWork.SaveChangesAsync(ct);

                return TypedResults.NoContent();
            },
            none: () => Task.FromResult((Results<NoContent, NotFound, ProblemDetails>)TypedResults.NotFound()));
    }
}