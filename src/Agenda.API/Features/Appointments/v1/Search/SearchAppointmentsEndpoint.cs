using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features.Appointments.v1.Delete;
using Agenda.API.Features.Appointments.v1.GetById;
using Agenda.Objects;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.Repositories;
using Candoumbe.Forms;
using DataFilters;
using DataFilters.Expressions;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Agenda.API.Features.Appointments.v1.Search;

/// <summary>
/// Gets people that are part of an appointment
/// </summary>
public class SearchAppointmentsEndpoint : Endpoint<SearchAppointmentRequest, Ok<PageOf<Browsable<AppointmentInfo>>>>
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly CurrentRequestMetadataInfoProvider _currentRequestMetadataInfo;

    /// <summary>
    /// Builds a new <see cref="SearchAppointmentsEndpoint"/> instance
    /// </summary>
    /// <param name="unitOfWorkFactory">Gives access to the underlying datastore</param>
    /// <param name="httpContextAccessor"></param>
    /// <param name="linkGenerator">Helper to generate links between resources.</param>
    /// <param name="currentRequestMetadataInfo"></param>
    public SearchAppointmentsEndpoint(IUnitOfWorkFactory unitOfWorkFactory,
                                      IHttpContextAccessor httpContextAccessor,
                                      LinkGenerator linkGenerator,
                                      CurrentRequestMetadataInfoProvider currentRequestMetadataInfo)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _currentRequestMetadataInfo = currentRequestMetadataInfo;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Verbs(Http.GET, Http.HEAD);
        Routes("/appointments");
        AllowAnonymous();
        Summary(s =>
                {
                    s.Summary = "Search appointments";
                    s.Description = "Returns a page of appointments matching optional filters";
                });
    }

    /// <inheritdoc />
    public override async Task<Ok<PageOf<Browsable<AppointmentInfo>>>> ExecuteAsync(SearchAppointmentRequest search, CancellationToken ct)
    {
        Logger.LogInformation("Searching appointments");
        DateTimeZone zone = _currentRequestMetadataInfo.GetCurrentDateTimeZone();

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

        IOrder<Appointment> order = new Order<Appointment>(nameof(Appointment.StartDate));

        using IUnitOfWork unitOfWork = _unitOfWorkFactory.NewUnitOfWork();

        Expression<Func<Appointment, bool>> predicate = ( filters.Count switch
                                                            {
                                                                1   => filters.Single(),
                                                                > 1 => new MultiFilter { Logic = FilterLogic.And, Filters = filters },
                                                                _   => Filter.True
                                                            } ).ToExpression<Appointment>(NullableValueBehavior.AddNullCheck);

        Page<Appointment> pageOfAppointments = await unitOfWork.Repository<Appointment>()
                                                   .Where(predicate,
                                                          order,
                                                          PageSize.From(search.PageSize),
                                                          PageIndex.From(search.Page),
                                                          cancellationToken: ct);

        HttpContext http = _httpContextAccessor.HttpContext;

        IReadOnlyList<Appointment> entries = [.. pageOfAppointments.Entries];
        int count = entries.Count;

        PageOf<Browsable<AppointmentInfo>> content = new()
        {
            Page = search.Page,
            Total = pageOfAppointments.Total,
            Count = count,
            Items =
            [
                .. entries.Select(x => new Browsable<AppointmentInfo>
                {
                    Resource = new AppointmentInfo
                    {
                        Id = x.Id,
                        Location = x.Location,
                        StartDate = x.StartDate.InZone(zone).ToOffsetDateTime(),
                        EndDate = x.EndDate.InZone(zone).ToOffsetDateTime()
                    },
                    Links =
                    [
                        new Link
                        {
                            Href = _linkGenerator.GetUriByRouteValues(http, nameof(GetAppointmentByIdEndpoint), new { x.Id }),
                            Relations = [LinkRelation.Self]
                        },
                        new Link
                        {
                            Href = _linkGenerator.GetUriByRouteValues(http, nameof(DeleteEndpoint), new { x.Id }),
                            Relations = ["delete"]
                        }
                    ]
                })
            ]
        };

        return TypedResults.Ok(content);
    }
}