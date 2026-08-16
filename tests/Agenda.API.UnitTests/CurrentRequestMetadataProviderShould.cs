using AwesomeAssertions;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using NodaTime;
using Xunit;
using Xunit.OpenCategories.V3;
using static Moq.MockBehavior;

namespace Agenda.API.UnitTests
{
    [UnitTest]
    public class CurrentRequestMetadataProviderShould
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<ILogger<CurrentRequestMetadataInfoProvider>> _loggerMock;
        private readonly CurrentRequestMetadataInfoProvider _sut;
        private static readonly Faker s_faker = new();

        public CurrentRequestMetadataProviderShould()
        {
            _httpContextAccessorMock = new(Strict);
            _loggerMock = new Mock<ILogger<CurrentRequestMetadataInfoProvider>>();
            _sut = new CurrentRequestMetadataInfoProvider(_httpContextAccessorMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void Returns_utc_when_no_DateTimeZone_information_can_be_found_in_the_current_http_request()
        {
            // Arrange
            DefaultHttpContext httpContext = new DefaultHttpContext();
            _httpContextAccessorMock.Setup(mock => mock.HttpContext)
                .Returns(httpContext);

            // Act
            DateTimeZone dateTimeZone = _sut.GetCurrentDateTimeZone();

            // Assert
            dateTimeZone.Should().Be(DateTimeZone.Utc, "The current http context does not contains any information on the user time zone");
            _loggerMock.Verify();
        }

        [Fact]
        public void Returns_the_timezone_found_in_the_request_When_the_request_contains_a_DateTimeZone_information()
        {
            // Arrange
            DefaultHttpContext httpContext = new();

            DateTimeZone expected = s_faker.Noda().DateTimeZone();
            httpContext.Request.Headers.Append(CurrentRequestMetadataInfoProvider.TimeZoneHeaderName, new StringValues(expected.Id));

            _httpContextAccessorMock.Setup(mock => mock.HttpContext)
                .Returns(httpContext);

            // Act
            DateTimeZone actual = _sut.GetCurrentDateTimeZone();

            // Assert
            actual.Should().Be(expected, $"The current http context contains exactly one header named {CurrentRequestMetadataInfoProvider.TimeZoneHeaderName}");
        }
    }
}