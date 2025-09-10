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
using Agenda.API.UnitTests.Helpers;
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
using Xunit.OpenCategories.V3;

namespace Agenda.API.UnitTests.Features.Appointments.v1.Search;

[UnitTest]
[Feature(nameof(Agenda))]
[Feature(nameof(Appointments))]
public class SearchAppointmentsEndpointShould
{
    private readonly ITestOutputHelper _outputHelper;
    private static readonly Faker s_faker;
    private static readonly Faker<Appointment> s_appointementFaker;
    private readonly SearchAppointmentsEndpoint _sut;
    private readonly IRepository<Appointment> _appointmentRepository;

    static SearchAppointmentsEndpointShould()
    {
        s_faker = new Faker();
        s_appointementFaker = new Faker<Appointment>()
            .CustomInstantiator(f =>
                                {
                                    Appointment appointement =  new Appointment(AppointmentId.New(),
                                                                                f.Lorem.Sentence(),
                                                                                f.Address.FullAddress(),
                                                                                f.Noda().Instant.Past(),
                                                                                f.Noda().Instant.Future());

                                    for (int i = 0; i < Random.Shared.Next(1, 10); i++)
                                    {
                                        appointement.AddAttendee(new Attendee(AttendeeId.New(), f.Name.FullName(), f.Internet.Email(), f.Phone.PhoneNumber()));
                                    }

                                    return appointement;
                                });
    }

    public SearchAppointmentsEndpointShould(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;

        IUnitOfWorkFactory unitOfWorkFactory = A.Fake<IUnitOfWorkFactory>(x => x.Strict().Named("unitOfWorkFactory"));
        IUnitOfWork unitOfWork = A.Fake<IUnitOfWork>(x => x.Strict().Named("unitOfWork"));
        A.CallTo(() => unitOfWork.Dispose()).DoesNothing();
        _appointmentRepository = A.Fake<IRepository<Appointment>>(x => x.Strict().Named("repository"));

        A.CallTo(() => unitOfWorkFactory.NewUnitOfWork()).Returns(unitOfWork);
        A.CallTo(() => unitOfWork.Repository<Appointment>()).Returns(_appointmentRepository);

        LinkGenerator linkGenerator = A.Fake<LinkGenerator>();
        IHttpContextAccessor httpContextAccessor = A.Fake<IHttpContextAccessor>();
        CurrentRequestMetadataInfoProvider currentRequestMetadataInfoProvider = A.Fake<CurrentRequestMetadataInfoProvider>();
        _sut = Factory.Create<SearchAppointmentsEndpoint>(unitOfWorkFactory, httpContextAccessor, linkGenerator, currentRequestMetadataInfoProvider);
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

    public static TheoryData<GenericSerializable<IReadOnlyList<Appointment>>, GenericSerializable<SearchAppointmentRequest>, XunitSerializableExpression<PageOf<Browsable<AppointmentInfo>>>> RequestCases
    {
        get
        {
            TheoryData<GenericSerializable<IReadOnlyList<Appointment>>, GenericSerializable<SearchAppointmentRequest>, XunitSerializableExpression<PageOf<Browsable<AppointmentInfo>>>> cases = [];

            // No data in the database
            {
                List<Appointment> data = [];
                SearchAppointmentRequest request = new()
                {
                    Page = NonNegativeInteger.One,
                    PageSize = PositiveInteger.From(10),
                    Attendees = "e*"
                };
                cases.Add( data ,
                          request,
                          new XunitSerializableExpression<PageOf<Browsable<AppointmentInfo>>>
                          {
                              Value = pageOfAppointments => pageOfAppointments.Total == 0
                                                            && pageOfAppointments.Count == 0
                                                            && pageOfAppointments.Items != null
                                                            && pageOfAppointments.Items.Exactly(0)
                                                            && pageOfAppointments.Page == 1
                                                            && pageOfAppointments.Links != null
                                                            && pageOfAppointments.Links.First != null
                                                            && pageOfAppointments.Links.Previous == null
                                                            && pageOfAppointments.Links.Next == null
                                                            && pageOfAppointments.Links.Last != null
                          });
            }

            // Search with no filter, first page and there are 3 pages of 10 items
            {
                List<Appointment> data = s_appointementFaker.Generate(30);

                SearchAppointmentRequest request = new() {Page = NonNegativeInteger.One, PageSize = PositiveInteger.From(10) };
                cases.Add( data ,
                          request,
                          new XunitSerializableExpression<PageOf<Browsable<AppointmentInfo>>>
                          {
                              Value = pageOfAppointments => pageOfAppointments.Total == 30
                                                            && pageOfAppointments.Count == 10
                                                            && pageOfAppointments.Items != null
                                                            && pageOfAppointments.Items.Exactly(10)
                                                            && pageOfAppointments.Page == 1
                                                            && pageOfAppointments.Links != null
                                                            && pageOfAppointments.Links.First != null
                                                            && pageOfAppointments.Links.Previous == null
                                                            && pageOfAppointments.Links.Next != null
                                                            && pageOfAppointments.Links.Last != null
                          });
            }

            // Search with no filter, second page and there are 3 pages of 10 items
            {
                List<Appointment> data = s_appointementFaker.Generate(30);
                SearchAppointmentRequest request = new() { Page = NonNegativeInteger.From(2), PageSize = PositiveInteger.From(10), };
                cases.Add(data,
                          request,
                          new XunitSerializableExpression<PageOf<Browsable<AppointmentInfo>>>()
                          {
                              Value = pageOfAppointments => pageOfAppointments.Total == 30
                                                            && pageOfAppointments.Count == 10
                                                            && pageOfAppointments.Items != null
                                                            && pageOfAppointments.Items.Exactly(10)
                                                            && pageOfAppointments.Page == 2
                                                            && pageOfAppointments.Links != null
                                                            && pageOfAppointments.Links.First != null
                                                            && pageOfAppointments.Links.Previous != null
                                                            && pageOfAppointments.Links.Next != null
                                                            && pageOfAppointments.Links.Last != null
                          });

            }

            // Search with no filter, last page and there are 3 pages of 10 items
            {
                List<Appointment> data = s_appointementFaker.Generate(30);
                SearchAppointmentRequest request = new (){ Page = NonNegativeInteger.From(3), PageSize = PositiveInteger.From(10) };
                cases.Add(data,
                          request,
                          new XunitSerializableExpression<PageOf<Browsable<AppointmentInfo>>>()
                          {
                              Value = pageOfAppointments => pageOfAppointments.Total == 30
                                                            && pageOfAppointments.Count == 10
                                                            && pageOfAppointments.Items != null
                                                            && pageOfAppointments.Items.Exactly(10)
                                                            && pageOfAppointments.Page == 3
                                                            && pageOfAppointments.Links != null
                                                            && pageOfAppointments.Links.First != null
                                                            && pageOfAppointments.Links.Previous != null
                                                            && pageOfAppointments.Links.Next == null
                                                            && pageOfAppointments.Links.Last != null
                          });
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(RequestCases))]
    public async Task Have_expected_response(GenericSerializable<IReadOnlyList<Appointment>> appointments,
                                             GenericSerializable<SearchAppointmentRequest> request,
                                             XunitSerializableExpression<PageOf<Browsable<AppointmentInfo>>> responseExpectation)
    {
        // Arrange
        Expression<Func<Appointment, bool>> capturedExpression = null;
        A.CallTo(() => _appointmentRepository.Where(An<Expression<Func<Appointment, bool>>>._,
                                                                 A<IOrder<Appointment>>._,
                                                                 A<PageSize>._,
                                                                 A<PageIndex>._,
                                                                 A.Dummy<CancellationToken>()))
            .WithAnyArguments()
            .Invokes((Expression<Func<Appointment, bool>> predicate, IOrder<Appointment> order, PageSize pageSize, PageIndex pageIndex, CancellationToken _) =>
                     {
                         capturedExpression = predicate;
                     })
            .ReturnsLazily((Expression<Func<Appointment, bool>> predicate, IOrder<Appointment> order, PageSize pageSize, PageIndex pageIndex, CancellationToken _)
                               =>
                           {
                               Func<Appointment, bool> compiledPredicate = predicate.Compile();
                               long count = appointments.Value.Count(compiledPredicate);
                               List<Appointment> results = appointments.Value.AsQueryable()
                                   .Where(predicate)
                                   .OrderBy(order)
                                   .Skip((pageIndex - 1)* pageSize)
                                   .Take(pageSize)
                                   .ToList();

                               Page<Appointment> page = new(results, count, pageSize);

                               return Task.FromResult(page);
                           });

        // Act
        Ok<PageOf<Browsable<AppointmentInfo>>> response = await _sut.ExecuteAsync(request.Value, TestContext.Current.CancellationToken);

        // Assert
        _outputHelper.WriteLine($"Expression was : {capturedExpression}");
        PageOf<Browsable<AppointmentInfo>> page = response.Value;
        _outputHelper.WriteLine($"Response : {new{ page.Count, page.Page, page.Total, page.Links }.Jsonify()}");
        page.Should().Match(responseExpectation.Value);

    }
}