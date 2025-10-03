using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features.v1.Appointments;
using Agenda.Objects;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.Repositories;
using Candoumbe.Forms;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NodaTime;
using Optional;

namespace Agenda.API.Features.Appointments.v1.GetById;

/// <summary>
/// Gets an appointment by its id
/// </summary>
public class GetAppointmentByIdEndpoint : Endpoint<GetByIdRequest, Results<Ok<Browsable<GetAppointmentByIdResponse>>, NotFound>>
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly LinkGenerator _linkGenerator;
    private readonly CurrentRequestMetadataInfoProvider _currentRequestMetadataInfoProvider;

    /// <summary>
    /// Builds a new <see cref="GetAppointmentByIdEndpoint"/> instance.
    /// </summary>
    /// <param name="unitOfWorkFactory"></param>
    /// <param name="linkGenerator"></param>
    /// <param name="currentRequestMetadataInfoProvider"></param>
    public GetAppointmentByIdEndpoint(IUnitOfWorkFactory unitOfWorkFactory, LinkGenerator linkGenerator, CurrentRequestMetadataInfoProvider currentRequestMetadataInfoProvider)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _linkGenerator = linkGenerator;
        _currentRequestMetadataInfoProvider = currentRequestMetadataInfoProvider;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Verbs(Http.GET, Http.HEAD);
        Routes("/appointments/{id}");

        AllowAnonymous();
    }


    /// <inheritdoc />
    public override async Task<Results<Ok<Browsable<GetAppointmentByIdResponse>>, NotFound>> ExecuteAsync(GetByIdRequest req, CancellationToken ct)
    {
        using IUnitOfWork unitOfWork = _unitOfWorkFactory.NewUnitOfWork();
        FilterSpecification<Appointment> predicate = new(x => x.Id == req.Id);
        Option<Appointment> mayBeAppointment = await unitOfWork.Repository<Appointment>()
                                                   .SingleOrDefault(predicate,
                                                                    includedProperties: [IncludeClause<Appointment>.Create(x => x.Attendees)],
                                                                    cancellationToken: ct)
                                                   .ConfigureAwait(false);

        return mayBeAppointment.Match<Results<Ok<Browsable<GetAppointmentByIdResponse>>, NotFound>>(some: entity =>
                                                                                                          {
                                                                                                              DateTimeZone zone = _currentRequestMetadataInfoProvider.GetCurrentDateTimeZone();
                                                                                                              GetAppointmentByIdResponse appointment = new()
                                                                                                              {
                                                                                                                  Id = entity.Id,
                                                                                                                  StartDate = entity.StartDate.InZone(zone),
                                                                                                                  EndDate = entity.EndDate.InZone(zone),
                                                                                                                  Subject = entity.Subject,
                                                                                                                  Location = entity.Location
                                                                                                              };

                                                                                                              Browsable<GetAppointmentByIdResponse> resource = new()
                                                                                                              {
                                                                                                                  Resource = appointment,
                                                                                                                  Links =
                                                                                                                  [
                                                                                                                      new Link
                                                                                                                      {
                                                                                                                          Href = _linkGenerator.GetPathByName(HttpContext, IEndpoint.GetName<GetAppointmentByIdEndpoint>(verb: Http.GET), new { req.Id }),
                                                                                                                          Method = "GET",
                                                                                                                          Relations = [ LinkRelation.Self ]
                                                                                                                      }
                                                                                                                  ]
                                                                                                              };

                                                                                                              return TypedResults.Ok(resource);
                                                                                                          },
                                                                                                    none: () => TypedResults.NotFound());
    }
}