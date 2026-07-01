using System;
using System.Security.Claims;
using Agenda.Objects;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.OpenCategories.V3;
using static Moq.MockBehavior;

namespace Agenda.API.UnitTests.Authentication
{
    [UnitTest]
    public class CurrentRequestMetadataInfoProviderShould
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<ILogger<CurrentRequestMetadataInfoProvider>> _loggerMock;
        private readonly CurrentRequestMetadataInfoProvider _sut;

        public CurrentRequestMetadataInfoProviderShould()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>(Strict);
            _loggerMock = new Mock<ILogger<CurrentRequestMetadataInfoProvider>>();
            _sut = new CurrentRequestMetadataInfoProvider(_httpContextAccessorMock.Object, _loggerMock.Object);
        }

        private void GivenUser(params Claim[] claims)
        {
            ClaimsIdentity identity = new(claims, "TestAuth");
            ClaimsPrincipal principal = new(identity);
            DefaultHttpContext httpContext = new() { User = principal };
            _httpContextAccessorMock.Setup(mock => mock.HttpContext).Returns(httpContext);
        }

        [Fact]
        public void GetCurrentUserId_returns_parsed_guid_when_sub_is_a_guid()
        {
            // Arrange
            Guid expected = Guid.NewGuid();
            GivenUser(new Claim("sub", expected.ToString()));

            // Act
            Guid? actual = _sut.GetCurrentUserId();

            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public void GetCurrentUserId_returns_null_when_sub_is_missing()
        {
            // Arrange
            GivenUser(new Claim("preferred_username", "alice"));

            // Act
            Guid? actual = _sut.GetCurrentUserId();

            // Assert
            actual.Should().BeNull();
        }

        [Fact]
        public void GetCurrentUserId_returns_null_when_sub_is_not_a_guid()
        {
            // Arrange
            GivenUser(new Claim("sub", "not-a-guid"));

            // Act
            Guid? actual = _sut.GetCurrentUserId();

            // Assert
            actual.Should().BeNull();
        }

        [Fact]
        public void GetCurrentUserName_returns_preferred_username_claim()
        {
            // Arrange
            GivenUser(new Claim("preferred_username", "alice"));

            // Act
            string actual = _sut.GetCurrentUserName();

            // Assert
            actual.Should().Be("alice");
        }

        [Fact]
        public void GetCurrentUserName_returns_empty_when_preferred_username_missing()
        {
            // Arrange
            GivenUser(new Claim("sub", Guid.NewGuid().ToString()));

            // Act
            Username actual = _sut.GetCurrentUserName();

            // Assert
            actual.Should().Be(Username.Empty);
        }

        [Fact]
        public void GetCurrentUserName_uses_only_preferred_username_not_name_claim()
        {
            // Arrange
            GivenUser(
                new Claim(ClaimTypes.Name, "fallback-name"),
                new Claim("name", "another-name"));

            // Act
            string actual = _sut.GetCurrentUserName();

            // Assert
            actual.Should().BeEmpty();
        }
    }
}
