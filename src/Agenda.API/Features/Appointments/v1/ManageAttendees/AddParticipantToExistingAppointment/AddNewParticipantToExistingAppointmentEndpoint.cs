using Agenda.Objects;
using Candoumbe.DataAccess.Abstractions;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Optional;

namespace Agenda.API.Features.Appointments.v1.ManageAttendees.AddParticipantToExistingAppointment;

/// <summary>
/// Remove an attendee from an appointment
/// </summary>
public class AddNewParticipantToExistingAppointmentEndpoint : Endpoint<AddNewParticipantToExistingAppointmentRequest, Results<NoContent, Conflict, NotFound>>
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    /// <summary>
    /// Builds a new <see cref="AddNewParticipantToExistingAppointmentEndpoint"/> instance.
    /// </summary>
    /// <param name="unitOfWorkFactory"></param>
    public AddNewParticipantToExistingAppointmentEndpoint(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/appointments/{id}/attendees");
    }

    /// <inheritdoc />
    public override async Task<Results<NoContent, Conflict, NotFound>> ExecuteAsync(AddNewParticipantToExistingAppointmentRequest req, CancellationToken ct)
    {

        using IUnitOfWork unitOfWork = _unitOfWorkFactory.NewUnitOfWork();

        IFilterSpecification<Appointment> predicate = new FilterSpecification<Appointment>(a => a.Id == req.Id);
        Option<Appointment> maybeAppointment = await unitOfWork.Repository<Appointment>().SingleOrDefault(predicate, ct);
        return await maybeAppointment.Match(
            some: async (existingAppointment) =>
            {
                Results<NoContent, Conflict, NotFound> result;
                if (existingAppointment.Attendees.All(a => a.Id != req.Participant.Id))
                {
                    existingAppointment.AddAttendee(new Attendee(req.Participant.Id, req.Participant.Name, req.Participant.Email, req.Participant.PhoneNumber));
                    await unitOfWork.SaveChangesAsync(ct);

                    result = TypedResults.NoContent();
                }
                else
                {
                    result = TypedResults.Conflict();
                }

                return result;
            },
            none: () => Task.FromResult<Results<NoContent,Conflict, NotFound>>(TypedResults.NotFound())
        );
        return TypedResults.NotFound();
    }
}