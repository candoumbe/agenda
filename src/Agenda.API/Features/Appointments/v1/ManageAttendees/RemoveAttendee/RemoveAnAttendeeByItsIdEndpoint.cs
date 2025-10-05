using Agenda.Objects;
using Candoumbe.DataAccess.Abstractions;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Optional;

namespace Agenda.API.Features.Appointments.v1.ManageAttendees.RemoveAttendee;

/// <summary>
/// Remove an attendee from an appointment
/// </summary>
public class RemoveAnAttendeeByItsIdEndpoint : Endpoint<RemoveAttendeeRequest, Results<NoContent, NotFound>>
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    /// <summary>
    /// Builds a new <see cref="RemoveAnAttendeeByItsIdEndpoint"/> instance.
    /// </summary>
    /// <param name="unitOfWorkFactory"></param>
    public RemoveAnAttendeeByItsIdEndpoint(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/appointments/{id}/attendees/{attendeeId}");
    }

    /// <inheritdoc />
    public override async Task<Results<NoContent, NotFound>> ExecuteAsync(RemoveAttendeeRequest req, CancellationToken ct)
    {

        using IUnitOfWork unitOfWork = _unitOfWorkFactory.NewUnitOfWork();

        IFilterSpecification<Appointment> predicate = new FilterSpecification<Appointment>(a => a.Id == req.Id);
        Option<Appointment> maybeAppointment = await unitOfWork.Repository<Appointment>().SingleOrDefault(predicate, ct);
        return await maybeAppointment.Match(
            some: async (existingAppointment) =>
            {
                Results<NoContent, NotFound> result;
                if (existingAppointment.Attendees.Any(a => a.Id == req.AttendeeId))
                {
                    existingAppointment.RemoveAttendee(req.AttendeeId);
                    await unitOfWork.SaveChangesAsync(ct);

                    result = TypedResults.NoContent();
                }
                else
                {
                    result = TypedResults.NotFound();
                }

                return result;
            },
            none: () => Task.FromResult<Results<NoContent, NotFound>>(TypedResults.NotFound())
        );
        return TypedResults.NotFound();
    }
}