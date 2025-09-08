using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.Appointments.v1.Create;
using Agenda.API.Features.Appointments.v1.Search;
using Agenda.API.Features.v1.Appointments;
using Agenda.Ids;
using Agenda.Objects;
using Bogus;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.Repositories;
using Candoumbe.Types.Numerics;
using DataFilters;
using FakeItEasy;
using FastEndpoints;
using FluentAssertions;
using FluentAssertions.Common;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NodaTime.Extensions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Agenda.API.UnitTests.Features.Appointments.v1.Search;

[UnitTest]
[Feature(nameof(Agenda))]
[Feature(nameof(Appointments))]
public class SearchAppointmentsEndpointShould
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly CurrentRequestMetadataInfoProvider _currentRequestMetadataInfoProvider;
    private static readonly Faker s_faker;
    private static readonly Faker<AttendeeInfo> s_attendeeFaker;
    private readonly SearchAppointmentsEndpoint _sut;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Appointment> _appointmentRepository;

    static SearchAppointmentsEndpointShould()
    {
        s_faker = new Faker();
        s_attendeeFaker = new Faker<AttendeeInfo>();
        s_attendeeFaker.RuleFor(attendee => attendee.Id, new AttendeeId())
            .RuleFor(attendee => attendee.Name, s_faker.Name.FullName())
            .RuleFor(attendee => attendee.Email, s_faker.Internet.Email())
            .RuleFor(attendee => attendee.PhoneNumber, s_faker.Phone.PhoneNumber())
            ;
    }

    public SearchAppointmentsEndpointShould(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;

        _unitOfWorkFactory = A.Fake<IUnitOfWorkFactory>(x => x.Strict().Named("unitOfWorkFactory"));
        _unitOfWork = A.Fake<IUnitOfWork>(x => x.Strict().Named("unitOfWork"));
        A.CallTo(() => _unitOfWork.Dispose()).DoesNothing();
        _appointmentRepository = A.Fake<IRepository<Appointment>>(x => x.Strict().Named("repository"));

        A.CallTo(() => _unitOfWorkFactory.NewUnitOfWork()).Returns(_unitOfWork);
        A.CallTo(() => _unitOfWork.Repository<Appointment>()).Returns(_appointmentRepository);

        _linkGenerator = A.Fake<LinkGenerator>();
        _httpContextAccessor = A.Fake<IHttpContextAccessor>();
        _currentRequestMetadataInfoProvider = A.Fake<CurrentRequestMetadataInfoProvider>();
        _sut = Factory.Create<SearchAppointmentsEndpoint>(_unitOfWorkFactory, _httpContextAccessor, _linkGenerator, _currentRequestMetadataInfoProvider);
    }


    [Fact]
    public void Have_expected_definition()
    {
        // Assert
        EndpointDefinition endpointDefinition = _sut.Definition;
        using AssertionScope _ = new ();


        string[] routes = endpointDefinition.Routes;
        routes.Should()
            .HaveCount(1)
            .And
            .ContainSingle("/appointments");

        string[] methods = endpointDefinition.Verbs;
        methods.Should()
            .HaveCount(2)
            .And.ContainSingle(method => method == "GET")
            .And.ContainSingle(method => method == "HEAD");

        endpointDefinition.PostProcessorsList.Should()
            .NotBeEmpty()
            .And.Contain(processor => processor is AddLinkHeaderPostProcessor);
    }

    public static TheoryData<IReadOnlyList<Appointment>, SearchAppointmentRequest, Expression<Func<PageOf<Browsable<AppointmentInfo>>, bool>>> RequestCases
    {
        get
        {
            TheoryData<IReadOnlyList<Appointment>, SearchAppointmentRequest, Expression<Func<PageOf<Browsable<AppointmentInfo>>, bool>>> cases = new();

            // No data in the database
            {
                IReadOnlyList<Appointment> data = [];
                SearchAppointmentRequest request = new()
                {
                    Page = NonNegativeInteger.One,
                    PageSize = PositiveInteger.From(10),
                    Attendees = "e*"
                };
                cases.Add([],
                          request,
                          pageOfAppointments => pageOfAppointments.Total == 0
                                                && pageOfAppointments.Count == 0
                                                && pageOfAppointments.Items != null
                                                && pageOfAppointments.Items.Exactly(0)
                                                && pageOfAppointments.Page == 1
                                                && pageOfAppointments.Links != null
                                                && pageOfAppointments.Links.First != null
                                                && pageOfAppointments.Links.Previous == null
                                                && pageOfAppointments.Links.Next == null
                                                && pageOfAppointments.Links.Last != null
                          );
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(RequestCases))]
    public async Task Have_expected_response(IReadOnlyList<Appointment> appointments,
                                             SearchAppointmentRequest request,
                                             Expression<Func<PageOf<Browsable<AppointmentInfo>>, bool>> responseExpectation)
    {
        // Arrange
        Captured<Expression<Func<Appointment, bool>>> capturedExpression = A.Captured<Expression<Func<Appointment,bool>>>();
        A.CallTo(() => _appointmentRepository.Where(A<Expression<Func<Appointment, bool>>>._,
                                                                 A<IOrder<Appointment>>._,
                                                                 A<PageSize>._,
                                                                 A<PageIndex>._,
                                                                 A.Dummy<CancellationToken>()))
            .WithAnyArguments()
            .ReturnsLazily((Expression<Func<Appointment, bool>> predicate, IOrder<Appointment> order, PageSize pageSize, PageIndex pageIndex, CancellationToken _)
                               =>
                           {
                               Func<Appointment, bool> compiledPredicate = predicate.Compile();
                               long count = appointments.Count(compiledPredicate);
                               List<Appointment> results = appointments.AsQueryable()
                                   .Where(predicate)
                                   .OrderBy(order)
                                   .Skip(pageIndex * pageSize)
                                   .Take(pageSize)
                                   .ToList();

                               Page<Appointment> page = new(results, count, pageSize);

                               return Task.FromResult(page);
                           });

        // Act
        Ok<PageOf<Browsable<AppointmentInfo>>> response = await _sut.ExecuteAsync(request, CancellationToken.None);

        // Assert
        _outputHelper.WriteLine($"Expression was : {capturedExpression.Values.FirstOrDefault()}");
        response.Value.Should().NotBeNull();
        response.Value.Should().Match(responseExpectation);

    }
}