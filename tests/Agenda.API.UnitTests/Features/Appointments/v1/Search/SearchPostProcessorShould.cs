using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.Appointments.v1.Search;
using Bogus;
using Candoumbe.Forms;
using FakeItEasy;
using FastEndpoints;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Xunit;
using Xunit.Categories;

namespace Agenda.API.UnitTests.Features.Appointments.v1.Search
{
    [UnitTest]
    public class AddLinkHeaderPostProcessorShould
    {
        private readonly ILogger<AddLinkHeaderPostProcessor> _logger;
        private readonly AddLinkHeaderPostProcessor _sut;
        private static readonly Faker s_faker = new();

        public AddLinkHeaderPostProcessorShould()
        {
            _logger = A.Fake<ILogger<AddLinkHeaderPostProcessor>>();
            _sut = new AddLinkHeaderPostProcessor(_logger);
        }

        [Fact]
        public void Should_be_a_post_processor()
        {
            typeof(AddLinkHeaderPostProcessor)
                .Should()
                .BeAssignableTo<IPostProcessor<SearchAppointmentRequest, Ok<PageOf<Browsable<AppointmentInfo>>>>>()
                .And.HaveConstructor([typeof(ILogger<AddLinkHeaderPostProcessor>)]);
        }

        [Fact]
        public void Throw_ArgumentNullException_When_logger_is_null()
        {
            // Act
            Action creatingInstanceWithLoggerNull = () => _ = new AddLinkHeaderPostProcessor(null);

            // Assert
            creatingInstanceWithLoggerNull.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task Given_a_response_with_an_empty_page_When_post_processing_Then_return_expected_response()
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
            HttpResponse fakeResponse = A.Fake<HttpResponse>(x => x.Strict());

            A.CallTo(() => fakeHttpContext.Response).Returns(fakeResponse);
            Captured<Func<Task>> capturedOnStartingCallback = A.Captured<Func<Task>>();
            A.CallTo(() => fakeResponse.OnStarting(capturedOnStartingCallback._)).Invokes(() => {});
            A.CallTo(() => fakeResponse.Headers).Returns(new HeaderDictionary());

            IPostProcessorContext<SearchAppointmentRequest, Ok<PageOf<Browsable<AppointmentInfo>>>> context = A.Fake<IPostProcessorContext<SearchAppointmentRequest, Ok<PageOf<Browsable<AppointmentInfo>>>>>(x => x.Strict());
            A.CallTo(() => context.HttpContext).Returns(fakeHttpContext);
            A.CallTo(() => context.Response).Returns(response);


            // Act
            await _sut.PostProcessAsync(context, CancellationToken.None);

            // Assert
            using AssertionScope _ = new();

            A.CallTo(() => fakeResponse.OnStarting(capturedOnStartingCallback._)).MustHaveHappenedOnceExactly();

            await capturedOnStartingCallback.GetLastValue().Invoke(); // <-- this forces the OnStarting callback to be called

            IHeaderDictionary headers = context.HttpContext.Response.Headers;
            StringValues links = headers.Link;
            links.Should().HaveCount(2)
                .And.ContainSingle(link => link.Like("""
                                                     <*>; rel="first"
                                                     """))
                .And.ContainSingle(link => link.Like("""
                                                     <*>; rel="last"
                                                     """));

        }
    }
}