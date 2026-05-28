using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agenda.API.Features;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.Appointments.v1.Search;
using Agenda.Ids;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Bogus;
using Candoumbe.Forms;
using FakeItEasy;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Xunit;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Agenda.API.UnitTests.Features.Appointments.v1.Search;

public class AddLinkHeaderResponseInterceptorShould
{
    private readonly ILogger<AddLinkHeaderResponseInterceptor> _logger;
    private readonly AddLinkHeaderResponseInterceptor _sut;
    private static readonly Faker s_faker = new();

    public AddLinkHeaderResponseInterceptorShould()
    {
        _logger = A.Fake<ILogger<AddLinkHeaderResponseInterceptor>>();
        _sut = new AddLinkHeaderResponseInterceptor(_logger);
    }

    [Fact]
    public void Should_be_a_response_processor()
    {
        typeof(AddLinkHeaderResponseInterceptor)
            .Should()
            .BeAssignableTo<IResponseInterceptor>()
            .And.HaveConstructor([typeof(ILogger<AddLinkHeaderResponseInterceptor>)]);
    }

    [Fact]
    public void Throw_ArgumentNullException_When_logger_is_null()
    {
        // Act
        Action creatingInstanceWithLoggerNull = () => _ = new AddLinkHeaderResponseInterceptor(null);

        // Assert
        creatingInstanceWithLoggerNull.Should().Throw<ArgumentNullException>();
    }

    [Theory]
     [InlineData("GET")]
     [InlineData("HEAD")]
     [InlineData("POST")]
    public async Task Given_a_response_with_an_empty_page_When_post_processing_Then_return_expected_response(string method)
    {
        // Arrange
        Link firstPageLink = new() { Href = s_faker.Internet.Url(), Relations = [LinkRelation.First] };
        Link lastPageLink = new() { Href = s_faker.Internet.Url(), Relations = [LinkRelation.Last] };
        PageOf<Browsable<AppointmentInfo>> emptyPage = new()
        {
            Links = new PageLinks(firstPageLink, lastPageLink)
        };
        Ok<PageOf<Browsable<AppointmentInfo>>> response = TypedResults.Ok(emptyPage);
        HttpContext fakeHttpContext = A.Fake<HttpContext>(x => x.Strict());
        HttpRequest fakeRequest = A.Fake<HttpRequest>(x => x.Strict());
        A.CallTo(() => fakeHttpContext.Request).Returns(fakeRequest);
        A.CallTo(() => fakeRequest.Method).Returns(method);

        HttpResponse fakeResponse = A.Fake<HttpResponse>(x => x.Strict());
        A.CallTo(() => fakeHttpContext.Response).Returns(fakeResponse);
        Captured<Func<Task>> capturedOnStartingCallback = A.Captured<Func<Task>>();
        A.CallTo(() => fakeResponse.OnStarting(capturedOnStartingCallback._)).Invokes(() => {});
        A.CallTo(() => fakeResponse.Headers).Returns(new HeaderDictionary());


        // Act
        await _sut.InterceptResponseAsync(response, Status200OK, fakeHttpContext, [], TestContext.Current.CancellationToken);

        // Assert
        using AssertionScope _ = new();

        IHeaderDictionary headers = fakeHttpContext.Response.Headers;
        StringValues links = headers.Link;
        links.Should().HaveCount(2)
            .And.ContainSingle(link => link.Like("""
                                                 <*>; rel="first"
                                                 """))
            .And.ContainSingle(link => link.Like("""
                                                 <*>; rel="last"
                                                 """));

        headers.Should().ContainKey("total")
            .WhoseValue.Should().ContainSingle("0");
        headers.Should().ContainKey("totalCount")
            .WhoseValue.Should().ContainSingle("0");
        headers.Should().ContainKey("count")
            .WhoseValue.Should().ContainSingle("0");

    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("HEAD")]
    public async Task Given_a_response_with_a_browsable_resource_When_post_processing_Then_add_link_header(string method)
    {
        // Arrange
        Browsable<AppointmentInfo> browsable = new()
        {
            Resource = new AppointmentInfo { Id = AppointmentId.New(), Subject = s_faker.Lorem.Sentence(), Attendees = [] },
            Links = [new Link { Href = s_faker.Internet.Url(), Relations = [LinkRelation.Self] }]
        };

        Ok<Browsable<AppointmentInfo>> response = TypedResults.Ok(browsable);
        HttpContext fakeHttpContext = A.Fake<HttpContext>(x => x.Strict());
        HttpRequest fakeRequest = A.Fake<HttpRequest>(x => x.Strict());
        A.CallTo(() => fakeRequest.Method).Returns(method);

        HttpResponse fakeResponse = A.Fake<HttpResponse>(x => x.Strict());
        A.CallTo(() => fakeHttpContext.Request).Returns(fakeRequest);
        A.CallTo(() => fakeHttpContext.Response).Returns(fakeResponse);
        Captured<Func<Task>> capturedOnStartingCallback = A.Captured<Func<Task>>();
        A.CallTo(() => fakeResponse.OnStarting(capturedOnStartingCallback._)).Invokes(() => { });
        A.CallTo(() => fakeResponse.Headers).Returns(new HeaderDictionary());

        // Act
        await _sut.InterceptResponseAsync(response, Status200OK, fakeHttpContext, [], TestContext.Current.CancellationToken);

        // Assert
        IHeaderDictionary headers = fakeHttpContext.Response.Headers;
        headers.Should().ContainKey("Link");
        IEnumerable<string> links = headers["Link"].AsEnumerable();
        links.Should().ContainSingle(link => link.Like("""
                                                       <*>; rel="self"
                                                       """));

    }
}