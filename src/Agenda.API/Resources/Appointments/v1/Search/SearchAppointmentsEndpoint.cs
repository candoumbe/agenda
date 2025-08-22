namespace Agenda.API.Resources.Appointments.v1.Participation;

using Agenda.API.Resources.Appointments.v1.Delete;
using Agenda.API.Resources.Appointments.v1.GetById;
using Agenda.API.Resources.Appointments.v1.Search;
using Agenda.API.Resources.v1.Appointments;
using Agenda.Objects;

using FastEndpoints;

using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.Repositories;
using Candoumbe.Forms;

using DataFilters;

using Microsoft.AspNetCore.Mvc;

using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Gets people that are part of an appointment
/// </summary>
public class SearchAppointmentsEndpoint : FastEndpoints.Endpoint<SearchAppointmentRequest, PageOf<Browsable<AppointmentInfo>>>
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IHttpContextAccessor _httpContext;
    private readonly LinkGenerator _linkGenerator;
    private readonly CurrentRequestMetadataInfoProvider _currentRequestMetadataInfo;

    /// <summary>
    /// Builds a new <see cref="SearchAppointmentsEndpoint"/> instance
    /// </summary>
    /// <param name="unitOfWorkFactory">Gives access to the underlying datastore</param>
    /// <param name="httpContext"></param>
    /// <param name="linkGenerator">Helper to generate links between resources.</param>
    /// <param name="currentRequestMetadataInfo"></param>
    public SearchAppointmentsEndpoint(IUnitOfWorkFactory unitOfWorkFactory, IHttpContextAccessor httpContext, LinkGenerator linkGenerator, CurrentRequestMetadataInfoProvider currentRequestMetadataInfo)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _httpContext = httpContext;
        _linkGenerator = linkGenerator;
        _currentRequestMetadataInfo = currentRequestMetadataInfo;
    }

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

    public override async Task HandleAsync(SearchAppointmentRequest search, CancellationToken ct)
    {
        NodaTime.DateTimeZone zone = _currentRequestMetadataInfo.GetCurrentDateTimeZone();

        IList<IFilter> filters = new List<IFilter>();
        if (search.From is not null || search.To is not null)
        {
            filters.Add((search.From, search.To) switch
            {
                ({ }, { }) => new MultiFilter
                {
                    Logic = FilterLogic.And,
                    Filters = new[]
                    {
                        new Filter(nameof(Appointment.StartDate), FilterOperator.GreaterThanOrEqual, search.From.Value.ToInstant()),
                        new Filter(nameof(Appointment.EndDate), FilterOperator.LessThanOrEqualTo, search.To.Value.ToInstant())
                    }
                },
                ({ }, null) => new Filter(nameof(Appointment.StartDate), FilterOperator.GreaterThanOrEqual, search.From.Value.ToInstant()),
                (null, { }) => new Filter(nameof(Appointment.EndDate), FilterOperator.LessThanOrEqualTo, search.To.Value.ToInstant()),
            });
        }

        string subject = search.Subject?.Trim();
        if (!string.IsNullOrWhiteSpace(subject))
        {
            filters.Add($"{nameof(Appointment.Subject)}={subject}".ToFilter<Appointment>());
        }
        IOrder<Appointment> order = new Order<Appointment>(nameof(Appointment.StartDate));

        using IUnitOfWork unitOfWork = _unitOfWorkFactory.NewUnitOfWork();

        Expression<Func<Appointment, bool>> predicate = (filters.Count switch
        {
            1 => filters.Single(),
            > 1 => new MultiFilter { Logic = FilterLogic.And, Filters = filters },
            _ => Filter.True
        }).ToExpression<Appointment>(DataFilters.Expressions.NullableValueBehavior.AddNullCheck);

        Page<Appointment> pageOfAppointments = await unitOfWork.Repository<Appointment>()
            .Where(predicate,
                page: PageIndex.From(search.Page),
                pageSize: PageSize.From(search.PageSize),
                orderBy: order,
                cancellationToken: ct)
            .ConfigureAwait(false);

        HttpContext http = _httpContext.HttpContext;

        IEnumerable<Appointment> entries = pageOfAppointments.Entries;
        int count = entries.TryGetNonEnumeratedCount(out count) ? count : entries.Count();

        PageOf<Browsable<AppointmentInfo>> content = new()
        {
            Page = search.Page,
            PageSize = search.PageSize,
            Total = pageOfAppointments.Total,
            Count = count,
            Items = entries.Select(x => new Browsable<AppointmentInfo>()
            {
                Resource = new AppointmentInfo
                {
                    Id = x.Id,
                    Location = x.Location,
                    StartDate = x.StartDate.InZone(zone).ToOffsetDateTime(),
                    EndDate = x.EndDate.InZone(zone).ToOffsetDateTime()
                },
                Links = new[]
                {
                    new Link
                    {
                        Href = _linkGenerator.GetUriByRouteValues(http, nameof(GetAppointmentByIdEndpoint), new { x.Id }),
                        Relations = new []{ LinkRelation.Self }
                    },
                    new Link
                    {
                        Href = _linkGenerator.GetUriByRouteValues(http, nameof(DeleteEndpoint), new { x.Id }),
                        Relations = new []{ "delete" }
                    }
                }
            }).ToArray()
        };

        if (content.Total > content.Count)
        {
            await SendAsync(content, StatusCodes.Status206PartialContent, ct);
        }
        else
        {
            await SendOkAsync(content, ct);
        }
    }
}