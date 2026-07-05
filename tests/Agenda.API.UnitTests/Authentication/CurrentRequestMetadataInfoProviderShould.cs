using System;
using System.Collections.Generic;
using System.Security.Claims;
using AwesomeAssertions;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.OpenCategories.V3;
using static Moq.MockBehavior;

namespace Agenda.API.UnitTests.Authentication;

[UnitTest]
public class CurrentRequestMetadataInfoProviderShould
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<CurrentRequestMetadataInfoProvider>> _loggerMock;
    private readonly CurrentRequestMetadataInfoProvider _sut;

    private static readonly Faker s_faker = new();

    public CurrentRequestMetadataInfoProviderShould()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>(Strict);
        _loggerMock = new Mock<ILogger<CurrentRequestMetadataInfoProvider>>();
        _sut = new CurrentRequestMetadataInfoProvider(_httpContextAccessorMock.Object, _loggerMock.Object);
    }

    private void GivenUser(params IReadOnlyList<Claim> claims)
    {
        ClaimsIdentity identity = new(claims, "TestAuth");
        ClaimsPrincipal principal = new(identity);
        DefaultHttpContext httpContext = new() { User = principal };
        _httpContextAccessorMock.Setup(mock => mock.HttpContext).Returns(httpContext);
    }

    public static TheoryData<Claim[], string, string> GetCurrentUsernameCases
    {
        get
        {
            TheoryData<Claim[], string, string> cases = new()
            {
                {
                    [   ],
                    string.Empty,
                    "No claim provided"
                }
            };
            {
                string username = s_faker.Internet.UserName();
                cases.Add([new Claim(ClaimTypes.Name, username)], username, "HTTP request only contains 'name' claim");
            }
            {
                string username =  s_faker.Internet.UserName();
                cases.Add([new Claim("preferred-name", username)], username, "HTTP request only contains 'preferred-name' claim");
            }
            {
                string username =  s_faker.Internet.UserName();
                cases.Add([new Claim(ClaimTypes.Email, username)], username, $"HTTP request only contains '{ClaimTypes.Email}' claim");
            }
            {
                string username = s_faker.Internet.UserName();
                cases.Add([new Claim(ClaimTypes.GivenName, username)], username, $"HTTP request only contains '{ClaimTypes.GivenName}' claim");
            }
            {
                string username = s_faker.Internet.UserName();
                cases.Add(
                    [
                        new Claim("preferred-name", $"{Guid.CreateVersion7()}"),
                        new Claim(ClaimTypes.Email, username),
                    ], 
                    username,
                     "Email claim is used when both 'preferred-name' and 'email' claims are present");
            }
            {
                string username = s_faker.Internet.UserName();
                cases.Add(
                    [
                        new Claim(ClaimTypes.Email, username),
                        new Claim(ClaimTypes.Name, $"{Guid.CreateVersion7()}"),
                    ], 
                    username,
                    $"'{ClaimTypes.Email}' claim takes precedence over '{ClaimTypes.Name}' claim");
            }
            
        
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(GetCurrentUsernameCases))]
    public void Returns_expected_username_When_httpRequest_has_expected_value(IReadOnlyList<Claim> claims, string expectedUsername, string reason)
    {
        // Arrange
        GivenUser(claims);

        // Act
        string actualUsername = _sut.GetCurrentUserName();

        // Assert
        actualUsername.Should().Be(expectedUsername, because: reason);
    }
}
