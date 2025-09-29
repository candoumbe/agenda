using Agenda.Ids;
using Agenda.Objects;
using Candoumbe.DataAccess.Abstractions;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using NodaTime;
using Optional;
using SystemTextJsonPatch.Operations;
using Ultimately.Collections;

namespace Agenda.API.Features.Appointments.v1.Update;

/// <summary>
/// Updates an appointment based on a PATCH document
/// </summary>
public class PatchAppointmentByIdEndpoint : Endpoint<PatchRequest<AppointmentId, PatchAppointmentRequest>, Results<NoContent, NotFound, ProblemDetails>>
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly CurrentRequestMetadataInfoProvider _currentRequestMetadataInfoProvider;

    public PatchAppointmentByIdEndpoint(IUnitOfWorkFactory unitOfWorkFactory, CurrentRequestMetadataInfoProvider currentRequestMetadataInfoProvider)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _currentRequestMetadataInfoProvider = currentRequestMetadataInfoProvider;
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
                Ultimately.Option<Operation<PatchAppointmentRequest>> maybeSubjectOperation = req.Operations.SingleOrNone(op => op.Path!.Equals($"/{nameof(PatchAppointmentRequest.Subject)}", StringComparison.OrdinalIgnoreCase));
                maybeSubjectOperation.MatchSome(newSubjectOperation => existingAppointment.ChangeSubjectTo(newSubjectOperation.From));

                Ultimately.Option<Operation<PatchAppointmentRequest>> maybeLocationOperation = req.Operations.SingleOrNone(op => op.Path!.Equals($"/{nameof(PatchAppointmentRequest.Location)}", StringComparison.OrdinalIgnoreCase));
                maybeLocationOperation.MatchSome(newLocationOperation => existingAppointment.RelocateTo(newLocationOperation.From));

                Ultimately.Option<Operation<PatchAppointmentRequest>> maybeStartDateOperation = req.Operations.SingleOrNone(op => op.Path!.Equals($"/{nameof(PatchAppointmentRequest.StartDate)}", StringComparison.OrdinalIgnoreCase));
                maybeStartDateOperation.MatchSome(newStartDateOperation =>
                {
                    DateTimeZone dateTimeZone = _currentRequestMetadataInfoProvider.GetCurrentDateTimeZone();
                    ZonedDateTime newStartDate = ZonedDateTime.FromDateTimeOffset(DateTimeOffset.Parse(newStartDateOperation.From!));
                    existingAppointment.Reschedule(newStartDate, existingAppointment.EndDate.InZone(dateTimeZone));
                });

                Ultimately.Option<Operation<PatchAppointmentRequest>> maybeEndDateOperation = req.Operations.SingleOrNone(op => op.Path!.Equals($"/{nameof(PatchAppointmentRequest.EndDate)}", StringComparison.OrdinalIgnoreCase));
                maybeEndDateOperation.MatchSome(newEndDateOperation =>
                {
                    DateTimeZone dateTimeZone = _currentRequestMetadataInfoProvider.GetCurrentDateTimeZone();
                    ZonedDateTime newEndDate = ZonedDateTime.FromDateTimeOffset(DateTimeOffset.Parse(newEndDateOperation.From!));
                    existingAppointment.Reschedule(existingAppointment.StartDate.InZone(dateTimeZone), newEndDate);
                });

                await unitOfWork.SaveChangesAsync(ct);

                return TypedResults.NoContent();
            },
            none: () => Task.FromResult((Results<NoContent, NotFound, ProblemDetails>)TypedResults.NotFound()));
    }
}