using System.Linq.Expressions;
using Agenda.API.Features.Appointments.v1.Delete;
using Agenda.API.Features.Appointments.v1.GetById;
using Agenda.Ids;
using Agenda.Objects;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.Repositories;
using Candoumbe.Forms;
using DataFilters;
using DataFilters.Casing;
using DataFilters.Expressions;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using NodaTime;

namespace Agenda.API.Features.Appointments.v1.Search;

/// <summary>
/// Gets people that are part of an appointment
/// </summary>
public class SearchAppointmentsEndpoint : Endpoint<SearchAppointmentRequest, Ok<PageOf<Browsable<AppointmentInfo>>>>
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly LinkGenerator _linkGenerator;
    private readonly CurrentRequestMetadataInfoProvider _currentRequestMetadataInfo;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Builds a new <see cref="SearchAppointmentsEndpoint"/> instance
    /// </summary>
    /// <param name="unitOfWorkFactory">Gives access to the underlying datastore</param>
    /// <param name="linkGenerator">Helper to generate links between resources.</param>
    /// <param name="currentRequestMetadataInfo"></param>
    /// <param name="loggerFactory"></param>

    public SearchAppointmentsEndpoint(IUnitOfWorkFactory unitOfWorkFactory,
                                      LinkGenerator linkGenerator,
                                      CurrentRequestMetadataInfoProvider currentRequestMetadataInfo,
                                      ILoggerFactory loggerFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _linkGenerator = linkGenerator;
        _currentRequestMetadataInfo = currentRequestMetadataInfo;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Verbs(Http.GET, Http.HEAD);
        Routes("/appointments");
        AllowAnonymous();
        ResponseInterceptor(new AddLinkHeaderResponseInterceptor(_loggerFactory.CreateLogger<AddLinkHeaderResponseInterceptor>()));
    }

    /// <inheritdoc />
    public override async Task<Ok<PageOf<Browsable<AppointmentInfo>>>> ExecuteAsync(SearchAppointmentRequest request, CancellationToken ct)
    {
        Logger.LogInformation("Searching appointments");
        DateTimeZone zone = _currentRequestMetadataInfo.GetCurrentDateTimeZone();

        IFilter searchFilter = ComputeFilter(request);
        IOrderSpecification<AppointmentDto> order = new SingleOrderSpecification<AppointmentDto>(x => x.StartDate);


        Expression<Func<AppointmentDto, bool>> filterExpression = searchFilter.ToExpression<AppointmentDto>(NullableValueBehavior.AddNullCheck);
        FilterSpecification<AppointmentDto> predicate = new(filterExpression);

        using IUnitOfWork unitOfWork = _unitOfWorkFactory.NewUnitOfWork();
        SelectSpecification<Appointment, AppointmentDto> selector = new(app => new AppointmentDto
        {
            Id = app.Id,
            StartDate = app.StartDate,
            EndDate = app.EndDate,
            Subject = app.Subject,
            Location = app.Location,
            Attendees = app.Attendees.Select(attendee => new AttendeeDto { Id = attendee.Id, Name = attendee.Name, Email = attendee.Email, PhoneNumber = attendee.PhoneNumber })
        });
        Page<AppointmentDto> pageOfAppointments = await unitOfWork.Repository<Appointment>()
                                                      .Where(selector,
                                                             predicate,
                                                             order,
                                                             PageSize.From(request.PageSize),
                                                             PageIndex.From(request.Page),
                                                             cancellationToken: ct);

        IReadOnlyList<AppointmentInfo> entries =
        [
            .. pageOfAppointments.Entries.Select(x => new AppointmentInfo
            {
                Id = x.Id,
                EndDate = x.EndDate.InZone(zone).ToOffsetDateTime(),
                StartDate = x.StartDate.InZone(zone).ToOffsetDateTime(),
                Subject = x.Subject,
                Attendees = [..x.Attendees.Select(attendee => new AttendeeInfo { Id = attendee.Id, Email = attendee.Email, Name = attendee.Name, PhoneNumber = attendee.PhoneNumber })],
                Location = x.Location,
            })
        ];
        int count = entries.Count;

        Link firstPageLink = ComputeLinkToFirstPage(request, HttpContext!);
        Link lastPageLink = ComputeLinkToLastPage(request, HttpContext, pageOfAppointments);

        PageOf<Browsable<AppointmentInfo>> content = new()
        {
            Page = request.Page,
            Total = pageOfAppointments.Total,
            Count = count,
            Items =
            [
                .. entries.Select(x => new Browsable<AppointmentInfo>
                {
                    Resource = new AppointmentInfo
                    {
                        Id = x.Id,
                        EndDate = x.EndDate.InZone(zone).ToOffsetDateTime(),
                        StartDate = x.StartDate.InZone(zone).ToOffsetDateTime(),
                        Subject = x.Subject,
                        Attendees = [.. x.Attendees.Select(attendee => new AttendeeInfo { Id = attendee.Id, Email = attendee.Email, Name = attendee.Name, PhoneNumber = attendee.PhoneNumber })],
                        Location = x.Location,
                    },
                    Links =
                    [
                        new Link { Href = _linkGenerator.GetPathByName(HttpContext!, IEndpoint.GetName<GetAppointmentByIdEndpoint>(verb: Http.GET), new { x.Id }), Relations = [LinkRelation.Self] },
                        new Link { Href = _linkGenerator.GetPathByName(HttpContext!, IEndpoint.GetName<DeleteEndpoint>(), new { x.Id }), Relations = ["delete"] }
                    ]
                })
            ],
            Links = new PageLinks(First: firstPageLink,
                                  Last: lastPageLink,
                                  Previous: ComputeLinkToPreviousPage(request, pageOfAppointments, HttpContext),
                                  Next: ComputeLinkToNextPage(request, pageOfAppointments, HttpContext))
        };

        return TypedResults.Ok(content);

        IFilter ComputeFilter(SearchAppointmentRequest search)
        {
            List<IFilter> filters = [];
            if (search.From is not null || search.To is not null)
            {
                filters.Add((search.From, search.To) switch
                {
                    (not null, not null) => new MultiFilter
                    {
                        Logic = FilterLogic.And,
                        Filters =
                        [
                            new Filter(nameof(Appointment.StartDate), FilterOperator.GreaterThanOrEqual, search.From.Value.ToInstant()),
                            new Filter(nameof(Appointment.EndDate), FilterOperator.LessThanOrEqualTo, search.To.Value.ToInstant())
                        ]
                    },
                    (not null, null) => new Filter(nameof(Appointment.StartDate), FilterOperator.GreaterThanOrEqual, search.From.Value.ToInstant()),
                    (null, not null) => new Filter(nameof(Appointment.EndDate), FilterOperator.LessThanOrEqualTo, search.To.Value.ToInstant()),
                });
            }

            string subject = search.Subject?.Trim();
            if (!string.IsNullOrWhiteSpace(subject))
            {
                filters.Add($"{nameof(Appointment.Subject)}={subject}".ToFilter<Appointment>());
            }

            if (!string.IsNullOrWhiteSpace(search.Attendees))
            {
                filters.Add($"""{nameof(Appointment.Attendees)}["{nameof(Attendee.Name)}"]={search.Attendees}""".ToFilter<Appointment>());
            }

            return filters.Count switch
            {
                1   => filters.Single(),
                > 1 => new MultiFilter { Logic = FilterLogic.And, Filters = filters },
                _   => Filter.True
            };
        }

        Link ComputeLinkToPreviousPage(SearchAppointmentRequest localSearch, Page<AppointmentDto> page, HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            return (page.Count, localSearch.Page.Value) switch
            {
                (> 1, > 1) => new Link
                {
                    Href = _linkGenerator.GetPathByName(httpContext,
                                                      IEndpoint.GetName<SearchAppointmentsEndpoint>(Http.GET),
                                                      new
                                                      {
                                                          Page = localSearch.Page - 1,
                                                          localSearch.PageSize,
                                                          localSearch.Subject,
                                                          localSearch.Attendees,
                                                          localSearch.From,
                                                          localSearch.To,
                                                          localSearch.Sort
                                                      }),
                },
                _ => null
            };
        }

        Link ComputeLinkToNextPage(SearchAppointmentRequest searchAppointmentRequest, Page<AppointmentDto> page, HttpContext httpContext)
        {
            return searchAppointmentRequest.Page < page.Count
                       ? new Link
                       {
                           Href = _linkGenerator.GetPathByName(httpContext,
                                                                     IEndpoint.GetName<SearchAppointmentsEndpoint>(Http.GET),
                                                                     new
                                                                     {
                                                                         Page = searchAppointmentRequest.Page + 1,
                                                                         searchAppointmentRequest.PageSize,
                                                                         searchAppointmentRequest.Subject,
                                                                         searchAppointmentRequest.Attendees,
                                                                         searchAppointmentRequest.From,
                                                                         searchAppointmentRequest.To,
                                                                         searchAppointmentRequest.Sort
                                                                     }),
                       }
                       : null;
        }

        Link ComputeLinkToFirstPage(SearchAppointmentRequest localSearch, HttpContext httpContext)
        {
            return new Link()
            {
                Href = _linkGenerator.GetPathByName(httpContext!,
                                                          IEndpoint.GetName<SearchAppointmentsEndpoint>(verb: Http.GET),
                                                          new
                                                          {
                                                              Page = 1,
                                                              localSearch.PageSize,
                                                              localSearch.Subject,
                                                              localSearch.Attendees,
                                                              localSearch.From,
                                                              localSearch.To,
                                                              localSearch.Sort
                                                          }),
                Relations = [LinkRelation.First]
            };
        }

        Link ComputeLinkToLastPage(SearchAppointmentRequest localSearch, HttpContext httpContext, Page<AppointmentDto> page)
        {
            return new()
            {
                Href = _linkGenerator.GetPathByName(httpContext!,
                                                    IEndpoint.GetName<SearchAppointmentsEndpoint>(verb: Http.GET),
                                                    new
                                                    {
                                                        Page = (int)page.Total,
                                                        localSearch.PageSize,
                                                        localSearch.Subject,
                                                        localSearch.Attendees,
                                                        localSearch.From,
                                                        localSearch.To,
                                                        localSearch.Sort
                                                    }),
                Relations = [LinkRelation.Last]
            };
        }
    }
}

file record AppointmentDto
{
    public required AppointmentId Id { get; init; }
    public required Instant StartDate { get; init; }
    public required Instant EndDate { get; init; }
    public IEnumerable<AttendeeDto> Attendees { get; init; }

    public string Subject { get; init; }
    public string Location { get; init; }
}

file record AttendeeDto
{
    public required AttendeeId Id { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
    public string PhoneNumber { get; init; }
}