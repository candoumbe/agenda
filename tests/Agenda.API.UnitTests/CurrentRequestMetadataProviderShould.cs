using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using FakeItEasy;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Agenda.API.UnitTests
{
    [UnitTest]
    public class CurrentRequestMetadataProviderShould
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CurrentRequestMetadataInfoProvider> _logger;
        private readonly CurrentRequestMetadataInfoProvider _sut;
        private static readonly Faker s_faker = new();

        public CurrentRequestMetadataProviderShould()
        {
            _httpContextAccessor = A.Fake<IHttpContextAccessor>(x => x.Strict());
            _logger = A.Fake<ILogger<CurrentRequestMetadataInfoProvider>>();
            _sut = new CurrentRequestMetadataInfoProvider(_httpContextAccessor, _logger);
        }

        [Fact]
        public void Returns_utc_when_no_DateTimeZone_information_can_be_found_in_the_current_http_request()
        {
            // Arrange
            DefaultHttpContext httpContext = new ();
            A.CallTo(() => _httpContextAccessor.HttpContext)
                .Returns(httpContext);

            // Act
            DateTimeZone dateTimeZone = _sut.GetCurrentDateTimeZone();

            // Assert
            dateTimeZone.Should().Be(DateTimeZone.Utc, "The current http context does not contains any information on the user time zone");
        }

        [Fact]
        public void Returns_the_timezone_found_in_the_request_When_the_request_contains_a_DateTimeZone_information()
        {
            // Arrange
            DefaultHttpContext httpContext = new();

            DateTimeZone expected = s_faker.Noda().DateTimeZone();
            httpContext.Request.Headers.Append(CurrentRequestMetadataInfoProvider.TimeZoneHeaderName, new StringValues(expected.Id));

            A.CallTo(() => _httpContextAccessor.HttpContext)
                .Returns(httpContext);

            // Act
            DateTimeZone actual = _sut.GetCurrentDateTimeZone();

            // Assert
            actual.Should().Be(expected, $"The current http context contains exactly one header named {CurrentRequestMetadataInfoProvider.TimeZoneHeaderName}");
        }
    }
}