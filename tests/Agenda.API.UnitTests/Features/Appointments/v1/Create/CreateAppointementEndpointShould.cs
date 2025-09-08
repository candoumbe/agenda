using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.Appointments.v1.Create;
using Agenda.API.Features.Appointments.v1.Search;
using Agenda.API.Features.v1.Appointments;
using Agenda.Ids;
using Bogus;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.Forms;
using FakeItEasy;
using FastEndpoints;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Xunit;
using Xunit.Categories;

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
            _sut = Factory.Create<CreateAppointmentEndpoint>(_unitOfWorkFactory, _linkGenerator, _currentRequestMetadataInfoProvider);
        }

        public static TheoryData<NewAppointmentInfo, Expression<Func<AppointmentInfo, bool>>> CreateAppointmentWithValidRequestCases
        {
            get
            {
                TheoryData<NewAppointmentInfo, Expression<Func<AppointmentInfo, bool>>> cases = new();
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
                               resource => resource.Id == req.Id
                                                      && resource.Subject == req.Subject
                                                      && resource.Location == req.Location
                                                      && resource.StartDate == req.StartDate
                                                      && resource.EndDate == req.EndDate
                              );
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
                              resource => resource.Id != AppointmentId.Empty
                                                      && resource.Subject == req.Subject
                                                      && resource.Location == req.Location
                                                      && resource.StartDate == req.StartDate
                                                      && resource.EndDate == req.EndDate
                              );
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
                              resource => resource.Id != AppointmentId.Empty
                                                      && resource.Subject == req.Subject
                                                      && resource.Location == string.Empty
                                                      && resource.StartDate == req.StartDate
                                                      && resource.EndDate == req.EndDate
                              );
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
        public async Task Create_appointment_when_valid_request_is_received(NewAppointmentInfo req,
                                                                            Expression<Func<AppointmentInfo, bool>> responseExpectation)
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


            // Act
            CreatedAtRoute<Browsable<AppointmentInfo>> response = await _sut.ExecuteAsync(req, CancellationToken.None);

            // Assert
            response.RouteValues
                .Should().ContainKey("id");

            Browsable<AppointmentInfo> browsable = response.Value;
            browsable.Resource.Should().NotBeNull();

            AppointmentInfo resource = browsable.Resource;
            resource.Should().Match(responseExpectation);

            IEnumerable<Link> links = browsable.Links;
            links.Should()
                .OnlyContain(link => !string.IsNullOrWhiteSpace(link.Href))
                .And.OnlyContain(link => Uri.IsWellFormedUriString(link.Href, UriKind.Absolute), "all links must be absolute URIs")
                .And.OnlyContain(link => link.Relations.AtLeastOnce())
                .And.Contain(link => link.Relations.Once(rel => rel == LinkRelation.Self))
                .And.Contain(link => link.Relations.Once(rel => string.Equals(rel, "delete", StringComparison.OrdinalIgnoreCase)));
        }
    }
}