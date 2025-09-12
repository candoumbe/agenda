using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Agenda.API.Features;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.Appointments.v1.Search;
using Agenda.API.UnitTests.Fixtures;
using Agenda.API.UnitTests.Helpers;
using Agenda.DataStores;
using Agenda.Ids;
using Agenda.Objects;
using Bogus;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.EFStore;
using Candoumbe.Types.Numerics;
using FakeItEasy;
using FastEndpoints;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.UnitTests.Features.Appointments.v1.Search;

[UnitTest]
[Feature(nameof(Agenda))]
[Feature(nameof(Appointments))]
public sealed class SearchAppointmentsEndpointShould : IClassFixture<PostgresSqlFixture>, IAsyncLifetime
{
    private readonly ITestOutputHelper _outputHelper;
    private static readonly Faker s_faker;
    private static readonly Faker<Appointment> s_appointementFaker;
    private readonly SearchAppointmentsEndpoint _sut;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IClock _clock;

    static SearchAppointmentsEndpointShould()
    {
        s_faker = new Faker();
        s_appointementFaker = new Faker<Appointment>()
            .CustomInstantiator(f =>
                                {
                                    Appointment appointment = new Appointment(AppointmentId.New(),
                                                                              f.Lorem.Sentence(),
                                                                              f.Address.FullAddress(),
                                                                              f.Noda().Instant.Past(),
                                                                              f.Noda().Instant.Future());

                                    for (int i = 0; i < Random.Shared.Next(1, 3); i++)
                                    {
                                        appointment.AddAttendee(new Attendee(AttendeeId.New(), f.Name.FullName(), f.Internet.Email(), f.Phone.PhoneNumber()));
                                    }

                                    return appointment;
                                });
    }

    public SearchAppointmentsEndpointShould(ITestOutputHelper outputHelper, PostgresSqlFixture fixture)
    {
        _outputHelper = outputHelper;
        _clock = A.Fake<IClock>();

        DbContextOptionsBuilder<AgendaDataStore> optionsBuilder = new();
        optionsBuilder.UseNpgsql(fixture.ConnectionString, options => options.UseNodaTime()
                                                               .EnableRetryOnFailure(3));

        _unitOfWorkFactory = new EntityFrameworkUnitOfWorkFactory<AgendaDataStore>(optionsBuilder.Options,
                                                                                   options =>
                                                                                   {
                                                                                       AgendaDataStore store = new AgendaDataStore(options, _clock);
                                                                                       store.Database.EnsureCreated();
                                                                                       return store;
                                                                                   },
                                                                                   new AgendaRepositoryFactory());

        LinkGenerator linkGenerator = A.Fake<LinkGenerator>();
        IHttpContextAccessor httpContextAccessor = A.Fake<IHttpContextAccessor>();
        CurrentRequestMetadataInfoProvider currentRequestMetadataInfoProvider = A.Fake<CurrentRequestMetadataInfoProvider>();
        _sut = Factory.Create<SearchAppointmentsEndpoint>(_unitOfWorkFactory, httpContextAccessor, linkGenerator, currentRequestMetadataInfoProvider);
    }


    [Fact]
    public void Have_expected_definition()
    {
        // Assert
        EndpointDefinition endpointDefinition = _sut.Definition;
        using AssertionScope _ = new();


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
                SearchAppointmentRequest request = new() { Page = NonNegativeInteger.One, PageSize = PositiveInteger.From(10), Attendees = "e*" };
                cases.Add(data,
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

                SearchAppointmentRequest request = new() { Page = NonNegativeInteger.One, PageSize = PositiveInteger.From(10) };
                cases.Add(data,
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
                SearchAppointmentRequest request = new() { Page = NonNegativeInteger.From(3), PageSize = PositiveInteger.From(10) };
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

            // Search with a filter over subject and attendees
            {
                Appointment jobInterview = s_appointementFaker.Generate();
                jobInterview.ChangeSubjectTo("Should Shazam be a member of the JLA ?");
                jobInterview.AddAttendee(new Attendee(AttendeeId.New(), "Superman", s_faker.Internet.Email(firstName: "clark", lastName: "kent"), s_faker.Phone.PhoneNumber()));
                jobInterview.AddAttendee(new Attendee(AttendeeId.New(), "Wonder Woman", s_faker.Internet.Email(firstName: "diana", lastName: "prince"), s_faker.Phone.PhoneNumber()));
                jobInterview.AddAttendee(new Attendee(AttendeeId.New(), "Flash", s_faker.Internet.Email(firstName: "barry", lastName: "allen"), s_faker.Phone.PhoneNumber()));
                jobInterview.AddAttendee(new Attendee(AttendeeId.New(), "Batman", s_faker.Internet.Email(firstName: "bruce", lastName: "wayne"), s_faker.Phone.PhoneNumber()));
                jobInterview.AddAttendee(new Attendee(AttendeeId.New(), "Aquaman", s_faker.Internet.Email(firstName: "arthur", lastName: "curry"), s_faker.Phone.PhoneNumber()));
                jobInterview.AddAttendee(new Attendee(AttendeeId.New(), "Martian Manhunter", s_faker.Internet.Email(firstName: "J'onn", lastName: " J'onzz"), s_faker.Phone.PhoneNumber()));

                Appointment adventure = s_appointementFaker.Generate();
                adventure.ChangeSubjectTo("The brave and the bold");
                adventure.AddAttendee(new Attendee(AttendeeId.New(), "Batman", s_faker.Internet.Email(firstName: "bruce", lastName: "wayne"), s_faker.Phone.PhoneNumber()));
                adventure.AddAttendee(new Attendee(AttendeeId.New(), "Superman", s_faker.Internet.Email(firstName: "clark", lastName: "kent"), s_faker.Phone.PhoneNumber()));


                List<Appointment> data = [jobInterview, adventure,];
                SearchAppointmentRequest request = new() { Page = NonNegativeInteger.From(1), PageSize = PositiveInteger.From(10), Subject = "*brave*", Attendees = "*man" };

                cases.Add(data,
                          request,
                          new XunitSerializableExpression<PageOf<Browsable<AppointmentInfo>>>
                          {
                              Value = pageOfAppointments => pageOfAppointments.Total == 1
                                                            && pageOfAppointments.Count == 1
                                                            && pageOfAppointments.Items != null
                                                            && pageOfAppointments.Items.Exactly(1)
                                                            && pageOfAppointments.Items.All(a => a.Resource.Id == adventure.Id)
                                                            && pageOfAppointments.Page == 1
                                                            && pageOfAppointments.Links != null
                                                            && pageOfAppointments.Links.First != null
                                                            && pageOfAppointments.Links.Previous == null
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
        _outputHelper.WriteLine($"Request : {request.Value.Jsonify(new JsonSerializerOptions() { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })}");
        using IUnitOfWork unitOfWork = _unitOfWorkFactory.NewUnitOfWork();
        IRepository<Appointment> repository = unitOfWork.Repository<Appointment>();

        foreach (Appointment appointment in appointments.Value)
        {
            await repository.Create(appointment, TestContext.Current.CancellationToken);
        }

        await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        Ok<PageOf<Browsable<AppointmentInfo>>> response = await _sut.ExecuteAsync(request.Value, TestContext.Current.CancellationToken);

        // Assert
        PageOf<Browsable<AppointmentInfo>> page = response.Value;
        _outputHelper.WriteLine($"Response : {new
            {
                page.Count,
                page.Page, page.Total, page.Links, Items = page.Items.Select(item => new
                {
                    item.Resource.Id,
                    item.Resource.Subject,
                    Attendees = item.Resource.Attendees.Select(attendee => new { attendee.Id, attendee.Name })
                }) }
            .Jsonify(new JsonSerializerOptions() { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })}");
        page.Should().Match(responseExpectation.Value);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Clean up the database
        using IUnitOfWork uow = _unitOfWorkFactory.NewUnitOfWork();
        await uow.Repository<Attendee>().Clear().ConfigureAwait(false);
        await uow.Repository<Appointment>().Clear().ConfigureAwait(false);
        await uow.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;
}