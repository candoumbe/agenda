using Agenda.API.Resources.Appointments.v1.Delete;
using Agenda.API.Resources.Appointments.v1.GetById;
using Agenda.API.Resources.v1.Appointments;
using Agenda.Objects;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.Forms;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using NodaTime;

namespace Agenda.API.Resources.Appointments.v1.Create;

/// <summary>
/// Creates new appointment
/// </summary>
public class CreateAppointmentEndpoint : Endpoint<NewAppointmentInfo, CreatedAtRoute<Browsable<AppointmentInfo>>>
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly LinkGenerator _linkGenerator;
    private readonly CurrentRequestMetadataInfoProvider _currentRequestMetadataInfoProvider;


    /// <summary>
    /// Builds a new <see cref="CreateAppointmentEndpoint"/>
    /// </summary>
    /// <param name="unitOfWorkFactory"></param>
    /// <param name="linkGenerator"></param>
    /// <param name="currentRequestMetadataInfoProvider"></param>
    public CreateAppointmentEndpoint(IUnitOfWorkFactory unitOfWorkFactory, LinkGenerator linkGenerator, CurrentRequestMetadataInfoProvider currentRequestMetadataInfoProvider)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _linkGenerator = linkGenerator;
        _currentRequestMetadataInfoProvider = currentRequestMetadataInfoProvider;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/appointments");
        AllowAnonymous();
        SerializerContext<CreateAppointmentSerializerContext>();
    }



    /// <inheritdoc />
    public override async Task<CreatedAtRoute<Browsable<AppointmentInfo>>> ExecuteAsync(NewAppointmentInfo req, CancellationToken ct)
    {

        using IUnitOfWork unitOfWork = _unitOfWorkFactory.NewUnitOfWork();

        Appointment newAppointment = new(req.Id, req.Subject, req.Location, req.StartDate.ToInstant(), req.EndDate.ToInstant());
        foreach (AttendeeInfo attendee in req.Attendees)
        {
            newAppointment.AddAttendee(new Attendee(attendee.Id, attendee.Name, attendee.Email, attendee.PhoneNumber));
        }

        await unitOfWork.Repository<Appointment>().Create(newAppointment, ct);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        DateTimeZone zone = _currentRequestMetadataInfoProvider.GetCurrentDateTimeZone();
        AppointmentInfo appointmentInfo = new()
        {
            Id = newAppointment.Id,
            Location = newAppointment.Location,
            StartDate = newAppointment.StartDate.InZone(zone).ToOffsetDateTime(),
            EndDate = newAppointment.EndDate.InZone(zone).ToOffsetDateTime(),
            Subject = newAppointment.Subject,
            Attendees = req.Attendees
        };

        Browsable<AppointmentInfo> browsable = new()
        {
            Resource = appointmentInfo,
            Links =
            [
                new Link
                {
                    Href = _linkGenerator.GetUriByName(HttpContext, nameof(GetAppointmentByIdEndpoint), new { newAppointment.Id }),
                    Method = nameof(Get),
                    Relations = [LinkRelation.Self]
                },
                new Link
                {
                    Href = _linkGenerator.GetUriByName(HttpContext, nameof(DeleteEndpoint), new { newAppointment.Id }),
                    Method = nameof(Delete),
                    Relations = ["delete"]
                }
            ]
        };

        Logger.LogInformation("Appointment created: {Appointment}", browsable);

        return TypedResults.CreatedAtRoute(browsable, GetAppointmentByIdEndpoint.RouteName, new { newAppointment.Id });
    }
}