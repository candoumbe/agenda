using Agenda.API.Features.Appointments.v1.Create;
using Agenda.API.Features.Appointments.v1.Search;
using Agenda.API.Features.v1.Appointments;
using Agenda.Ids;
using Bogus;
using Candoumbe.DataAccess.Abstractions;
using FakeItEasy;
using FastEndpoints;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Agenda.API.UnitTests.Features.Appointments.v1.Search;

public class SearchAppointmentsEndpointShould
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly CurrentRequestMetadataInfoProvider _currentRequestMetadataInfoProvider;
    private static readonly Faker s_faker;
    private static readonly Faker<AttendeeInfo> s_attendeeFaker;
    private readonly SearchAppointmentsEndpoint _sut;

    static SearchAppointmentsEndpointShould()
    {
        s_faker = new Faker();
        s_attendeeFaker = new Faker<AttendeeInfo>();
        s_attendeeFaker.RuleFor(attendee => attendee.Id, AttendeeId.New)
            .RuleFor(attendee => attendee.Name, s_faker.Name.FullName())
            .RuleFor(attendee => attendee.Email, s_faker.Internet.Email())
            .RuleFor(attendee => attendee.PhoneNumber, s_faker.Phone.PhoneNumber())
            ;
    }

    public SearchAppointmentsEndpointShould()
    {
        _unitOfWorkFactory = A.Fake<IUnitOfWorkFactory>();
        _linkGenerator = A.Fake<LinkGenerator>();
        _httpContextAccessor = A.Fake<IHttpContextAccessor>();
        _currentRequestMetadataInfoProvider = A.Fake<CurrentRequestMetadataInfoProvider>();
        _sut = Factory.Create<SearchAppointmentsEndpoint>(_unitOfWorkFactory, _httpContextAccessor, _linkGenerator, _currentRequestMetadataInfoProvider);
    }


    [Fact]
    public void Have_expected_configuration()
    {
        // Assert
        using AssertionScope _ = new ();

        string[] routes = _sut.Definition.Routes;
        routes.Should()
            .HaveCount(1)
            .And
            .ContainSingle("/appointments");

        string[] methods = _sut.Definition.Verbs;
        methods.Should()
            .HaveCount(2)
            .And.ContainSingle(method => method == "GET")
            .And.ContainSingle(method => method == "HEAD");
    }
}