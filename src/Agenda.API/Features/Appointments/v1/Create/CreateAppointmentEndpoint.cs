using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features.Appointments.v1.Delete;
using Agenda.API.Features.Appointments.v1.GetById;
using Agenda.API.Features.v1.Appointments;
using Agenda.Events;
using Agenda.Objects;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.Forms;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using NodaTime;
using Paramore.Brighter;
using Attendee = Agenda.Objects.Attendee;

namespace Agenda.API.Features.Appointments.v1.Create;

/// <summary>
/// Creates new appointment
/// </summary>
public partial class CreateAppointmentEndpoint : Endpoint<NewAppointmentInfo, CreatedAtRoute<Browsable<AppointmentInfo>>>
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly LinkGenerator _linkGenerator;
    private readonly CurrentRequestMetadataInfoProvider _currentRequestMetadataInfoProvider;
    private readonly IAmACommandProcessor _commandProcessor;


    /// <summary>
    /// Builds a new <see cref="CreateAppointmentEndpoint"/>
    /// </summary>
    /// <param name="unitOfWorkFactory"></param>
    /// <param name="linkGenerator"></param>
    /// <param name="currentRequestMetadataInfoProvider"></param>
    /// <param name="commandProcessor"></param>
    public CreateAppointmentEndpoint(IUnitOfWorkFactory unitOfWorkFactory, LinkGenerator linkGenerator, CurrentRequestMetadataInfoProvider currentRequestMetadataInfoProvider, IAmACommandProcessor commandProcessor)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _linkGenerator = linkGenerator;
        _currentRequestMetadataInfoProvider = currentRequestMetadataInfoProvider;
        _commandProcessor = commandProcessor;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/appointments");
        AllowAnonymous();
        SerializerContext<CreateAppointmentSerializerContext>();
        Validator<NewAppointmentInfoValidator>();
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

        AppointmentScheduled appointmentScheduledEvent = new(newAppointment.Id,
                                                             newAppointment.Subject,
                                                             newAppointment.Location,
                                                             [
                                                                 ..newAppointment.Attendees.Select(a => new Agenda.Events.Attendee()
                                                                            {
                                                                                Id = a.Id.Value,
                                                                                FirstName = a.Name,
                                                                                LastName = a.Email
                                                                            })
                                                             ]);

        await _commandProcessor.DepositPostAsync(appointmentScheduledEvent, cancellationToken: ct);
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
                    Href = _linkGenerator.GetUriByName(HttpContext, IEndpoint.GetName<GetAppointmentByIdEndpoint>(verb: Http.GET), new { newAppointment.Id }),
                    Method = nameof(Get),
                    Relations = [LinkRelation.Self]
                },
                new Link
                {
                    Href = _linkGenerator.GetUriByName(HttpContext, IEndpoint.GetName<DeleteEndpoint>(), new { newAppointment.Id }),
                    Method = nameof(Delete),
                    Relations = ["delete"]
                }
            ]
        };

        LogAppointmentCreated(Logger, browsable);

        return TypedResults.CreatedAtRoute(browsable,
                                           IEndpoint.GetName<GetAppointmentByIdEndpoint>(verb: Http.GET),
                                           new { newAppointment.Id });
    }

    [LoggerMessage(LogLevel.Trace, "Appointment created: {appointment}")]
    static partial void LogAppointmentCreated(ILogger logger, Browsable<AppointmentInfo> appointment);
}