using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.Appointments.v1.Create;
using Agenda.API.Features.Appointments.v1.Search;
using Agenda.API.Features.v1.Appointments;
using Agenda.API.UnitTests.Helpers;
using Agenda.Events;
using Agenda.Ids;
using Agenda.Objects;
using Agenda.UnitTests.Helpers;
using AwesomeAssertions;
using Bogus;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.Forms;
using FakeItEasy;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Paramore.Brighter;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.UnitTests.Features.Appointments.v1.Create
{
    [UnitTest]
    public class CreateAppointementEndpointShould
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private readonly LinkGenerator _linkGenerator;
        private readonly CurrentRequestMetadataInfoProvider _currentRequestMetadataInfoProvider;
        private static readonly Faker s_faker;
        private static readonly Faker<AttendeeInfo> s_attendeeFaker;
        private readonly CreateAppointmentEndpoint _sut;
        private readonly IAmACommandProcessor _commandProcessor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<Appointment> _repository;

        static CreateAppointementEndpointShould()
        {
            s_faker = new Faker();
            s_attendeeFaker = new Faker<AttendeeInfo>();
            s_attendeeFaker.RuleFor(attendee => attendee.Id, new AttendeeId())
                .RuleFor(attendee => attendee.Name, s_faker.Name.FullName())
                .RuleFor(attendee => attendee.Email, s_faker.Internet.Email())
                .RuleFor(attendee => attendee.PhoneNumber, s_faker.Phone.PhoneNumber())
                ;
        }

        public CreateAppointementEndpointShould()
        {
            _unitOfWorkFactory = A.Fake<IUnitOfWorkFactory>();
            _linkGenerator = A.Fake<LinkGenerator>();
            _currentRequestMetadataInfoProvider = A.Fake<CurrentRequestMetadataInfoProvider>();
            _commandProcessor = A.Fake<IAmACommandProcessor>(x => x.Strict());
            _unitOfWork = A.Fake<IUnitOfWork>(x => x.Strict());
            _repository = A.Fake<IRepository<Appointment>>(x => x.Strict());

            A.CallTo(() => _unitOfWorkFactory.NewUnitOfWork()).Returns(_unitOfWork);
            A.CallTo(() => _unitOfWork.Repository<Appointment>()).Returns(_repository);
            A.CallTo(() => _repository.Create(An<Appointment>._, A<CancellationToken>._))
                .ReturnsLazily((FakeItEasy.Core.IFakeObjectCall call) => Task.FromResult((Appointment)call.Arguments[0]));
            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .ReturnsLazily((CancellationToken _) => Task.CompletedTask);
            A.CallTo(() => _unitOfWork.Dispose()).DoesNothing();

            _sut = Factory.Create<CreateAppointmentEndpoint>(_unitOfWorkFactory,
                                                             _linkGenerator,
                                                             _currentRequestMetadataInfoProvider,
                                                             _commandProcessor);
        }

        public static TheoryData<GenericSerializable<NewAppointmentInfo>, XunitSerializableExpression<AppointmentInfo>> CreateAppointmentWithValidRequestCases
        {
            get
            {
                TheoryData<GenericSerializable<NewAppointmentInfo>, XunitSerializableExpression<AppointmentInfo>> cases = new();
                // Request with valid data and client side generated id
                {
                    NewAppointmentInfo req = new()
                    {
                        Id = AppointmentId.New(),
                        Subject = s_faker.Lorem.Sentence(),
                        Location = s_faker.Address.FullAddress(),
                        StartDate = s_faker.Noda().ZonedDateTime.Past().ToOffsetDateTime(),
                        EndDate = s_faker.Noda().ZonedDateTime.Future().ToOffsetDateTime(),
                        Attendees = s_attendeeFaker.Generate(2),
                    };

                    cases.Add(req,
                              new XunitSerializableExpression<AppointmentInfo>()
                              {
                                  Value = resource => resource.Id == req.Id
                                                      && resource.Subject == req.Subject
                                                      && resource.Location == req.Location
                                                      && resource.StartDate == req.StartDate
                                                      && resource.EndDate == req.EndDate
                              });
                }
                // Request with valid data and server side generated id
                {
                    NewAppointmentInfo req = new()
                    {
                        Subject = s_faker.Lorem.Sentence(),
                        Location = s_faker.Address.FullAddress(),
                        StartDate = s_faker.Noda().ZonedDateTime.Past().ToOffsetDateTime(),
                        EndDate = s_faker.Noda().ZonedDateTime.Future().ToOffsetDateTime(),
                        Attendees = s_attendeeFaker.Generate(2),
                    };

                    cases.Add(req,
                              new XunitSerializableExpression<AppointmentInfo>()
                              {
                                  Value = resource => resource.Id != AppointmentId.Empty
                                                      && resource.Subject == req.Subject
                                                      && resource.Location == req.Location
                                                      && resource.StartDate == req.StartDate
                                                      && resource.EndDate == req.EndDate
                              });
                }

                // Request with no location
                {
                    NewAppointmentInfo req = new()
                    {
                        Subject = s_faker.Lorem.Sentence(),
                        StartDate = s_faker.Noda().ZonedDateTime.Past().ToOffsetDateTime(),
                        EndDate = s_faker.Noda().ZonedDateTime.Future().ToOffsetDateTime(),
                        Attendees = s_attendeeFaker.Generate(2),
                    };

                    cases.Add(req,
                              new XunitSerializableExpression<AppointmentInfo>()
                              {
                                  Value = resource => resource.Id != AppointmentId.Empty
                                                      && resource.Subject == req.Subject
                                                      && resource.Location == string.Empty
                                                      && resource.StartDate == req.StartDate
                                                      && resource.EndDate == req.EndDate
                              });
                }

                // Request with no attendees initialized by the client
                {
                    NewAppointmentInfo req = new()
                    {
                        Subject = s_faker.Lorem.Sentence(),
                        Location = s_faker.Address.FullAddress(),
                        StartDate = s_faker.Noda().ZonedDateTime.Past().ToOffsetDateTime(),
                        EndDate = s_faker.Noda().ZonedDateTime.Future().ToOffsetDateTime(),
                        Attendees = null,
                    };

                    cases.Add(req,
                              new XunitSerializableExpression<AppointmentInfo>()
                              {
                                  Value = resource => resource.Id != AppointmentId.Empty
                                                      && resource.Subject == req.Subject
                                                      && resource.Location == req.Location
                                                      && resource.StartDate == req.StartDate
                                                      && resource.EndDate == req.EndDate
                                                      && resource.Attendees != null
                                                      && !resource.Attendees.AtLeastOnce()
                              });
                }

                return cases;
            }
        }


        [Fact]
        public void Have_expected_definition()
        {
            // Assert
            EndpointDefinition endpointDefinition = _sut.Definition;
            string[] routes = endpointDefinition.Routes;
            routes.Should()
                .HaveCount(1)
                .And
                .ContainSingle("/appointments");

            string[] methods = endpointDefinition.Verbs;
            methods.Should().HaveCount(1)
                .And.ContainSingle("POST");

            Type validatorType = endpointDefinition.ValidatorType;
            validatorType.Should().Be<NewAppointmentInfoValidator>();
        }

        [Theory]
        [MemberData(nameof(CreateAppointmentWithValidRequestCases))]
        public async Task Create_appointment_when_valid_request_is_received(GenericSerializable<NewAppointmentInfo> req,
                                                                            XunitSerializableExpression<AppointmentInfo> responseExpectation)
        {
            // Arrange
            A.CallTo(() => _linkGenerator.GetUriByAddress(A<HttpContext>.Ignored,
                                                          A<string>.Ignored,
                                                          A<RouteValueDictionary>.Ignored,
                                                          A<RouteValueDictionary>.Ignored,
                                                          A<string>.Ignored,
                                                          A<HostString>.Ignored,
                                                          A<PathString>.Ignored,
                                                          A<FragmentString>.Ignored,
                                                          A<LinkOptions>.Ignored))
                .WithAnyArguments()
                .Returns(s_faker.Internet.Url());

            A.CallTo(() => _commandProcessor.DepositPostAsync(An<AppointmentScheduled>._, A<RequestContext>._, A<Dictionary<string, object>>._, A<bool>._, A<CancellationToken>._))
                .ReturnsLazily((AppointmentScheduled evt, RequestContext _, Dictionary<string, object> _,  bool _, CancellationToken _) => evt.Id);

            // Act
            CreatedAtRoute<Browsable<AppointmentInfo>> response = await _sut.ExecuteAsync(req, CancellationToken.None);

            // Assert
            response.RouteValues
                .Should().ContainKey("id");

            Browsable<AppointmentInfo> browsable = response.Value;
            browsable.Resource.Should().NotBeNull();

            AppointmentInfo resource = browsable.Resource;
            resource.Should().Match(responseExpectation.Value);

            IEnumerable<Link> links = browsable.Links;
            links.Should()
                .OnlyContain(link => !string.IsNullOrWhiteSpace(link.Href))
                .And.OnlyContain(link => Uri.IsWellFormedUriString(link.Href, UriKind.Absolute), "all links must be absolute URIs")
                .And.OnlyContain(link => link.Relations.AtLeastOnce())
                .And.Contain(link => link.Relations.Once(rel => rel == LinkRelation.Self))
                .And.Contain(link => link.Relations.Once(rel => string.Equals(rel, "delete", StringComparison.OrdinalIgnoreCase)));

            A.CallTo(() => _commandProcessor.DepositPostAsync(An<AppointmentScheduled>._, A<RequestContext>._, A<Dictionary<string, object>>._, A<bool>._, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _repository.Create(An<Appointment>._, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

        }

        [Fact]
        public async Task Persist_appointment_and_publish_expected_event_when_request_is_valid()
        {
            // Arrange
            NewAppointmentInfo req = new()
            {
                Id = AppointmentId.New(),
                Subject = s_faker.Lorem.Sentence(),
                Location = s_faker.Address.FullAddress(),
                StartDate = s_faker.Noda().ZonedDateTime.Past().ToOffsetDateTime(),
                EndDate = s_faker.Noda().ZonedDateTime.Future().ToOffsetDateTime(),
                Attendees = s_attendeeFaker.Generate(2),
            };

            A.CallTo(() => _linkGenerator.GetUriByAddress(A<HttpContext>.Ignored,
                                                          A<string>.Ignored,
                                                          A<RouteValueDictionary>.Ignored,
                                                          A<RouteValueDictionary>.Ignored,
                                                          A<string>.Ignored,
                                                          A<HostString>.Ignored,
                                                          A<PathString>.Ignored,
                                                          A<FragmentString>.Ignored,
                                                          A<LinkOptions>.Ignored))
                .WithAnyArguments()
                .Returns(s_faker.Internet.Url());

            A.CallTo(() => _commandProcessor.DepositPostAsync(An<AppointmentScheduled>._,
                                                              A<RequestContext>._,
                                                              A<Dictionary<string, object>>._,
                                                              A<bool>._,
                                                              A<CancellationToken>._))
                .ReturnsLazily((AppointmentScheduled evt, RequestContext _, Dictionary<string, object> _, bool _, CancellationToken _) => evt.Id);

            // Act
            await _sut.ExecuteAsync(req, CancellationToken.None);

            // Assert
            A.CallTo(() => _repository.Create(A<Appointment>.That.Matches(appointment =>
                    appointment.Id == req.Id
                    && appointment.Subject == req.Subject
                    && appointment.Location == req.Location
                    && appointment.Attendees.Count == req.Attendees.Count),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => _commandProcessor.DepositPostAsync(A<AppointmentScheduled>.That.Matches(evt =>
                    evt.AppointmentId == req.Id
                    && evt.Subject == req.Subject
                    && evt.Location == req.Location
                    && evt.Participants.Count == req.Attendees.Count
                    && req.Attendees.All(attendee => evt.Participants.Any(participant =>
                    participant.FirstName == attendee.Name
                        && participant.LastName == attendee.Email))),
                A<RequestContext>._,
                A<Dictionary<string, object>>._,
                A<bool>._,
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }
    }
}