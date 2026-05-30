using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Agenda.UnitTests.Helpers.Authentication;
using AwesomeAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.UnitTests.Authentication;

[UnitTest]
public class TokenFactoryShould
{
    private static readonly JwtSecurityTokenHandler s_handler = new() { MapInboundClaims = false };
    private readonly TokenFactory _sut = new();

    [Fact]
    public void Mint_a_valid_token_with_expected_audience_issuer_and_roles()
    {
        // Arrange
        string[] roles = ["agenda-user"];

        // Act
        string token = _sut.ValidFor("alice", roles);
        JwtSecurityToken parsed = s_handler.ReadJwtToken(token);

        // Assert
        parsed.Audiences.Should().ContainSingle().Which.Should().Be(TokenFactory.DefaultAudience);
        parsed.Issuer.Should().Be(_sut.Issuer);
        parsed.Subject.Should().Be("alice");
        parsed.Claims.Should().Contain(c => c.Type == "realm_access" && c.Value.Contains("agenda-user"));
        parsed.SignatureAlgorithm.Should().Be(SecurityAlgorithms.RsaSha256);
        parsed.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Mint_an_expired_token_when_using_Expired_preset()
    {
        // Act
        string token = _sut.Expired();
        JwtSecurityToken parsed = s_handler.ReadJwtToken(token);

        // Assert
        parsed.ValidTo.Should().BeBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Mint_a_token_with_an_unexpected_audience_when_using_WrongAudience_preset()
    {
        // Act
        string token = _sut.WrongAudience();
        JwtSecurityToken parsed = s_handler.ReadJwtToken(token);

        // Assert
        parsed.Audiences.Should().ContainSingle().Which.Should().NotBe(TokenFactory.DefaultAudience);
    }

    [Fact]
    public void Mint_a_token_with_an_unexpected_issuer_when_using_WrongIssuer_preset()
    {
        // Act
        string token = _sut.WrongIssuer();
        JwtSecurityToken parsed = s_handler.ReadJwtToken(token);

        // Assert
        parsed.Issuer.Should().NotBe(_sut.Issuer);
    }

    [Fact]
    public void Mint_a_token_whose_signature_does_not_match_the_published_key()
    {
        // Arrange
        TokenValidationParameters validationParameters = new()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            IssuerSigningKeys = _sut.GetJwks().Keys
        };

        // Act
        string token = _sut.UnsignedOrTampered();
        Action validate = () => s_handler.ValidateToken(token, validationParameters, out _);

        // Assert
        validate.Should().Throw<SecurityTokenException>();
    }

    [Fact]
    public void Publish_a_jwks_containing_the_signing_key()
    {
        // Act
        JsonWebKeySet jwks = _sut.GetJwks();

        // Assert
        jwks.Keys.Should().HaveCount(1);
        JsonWebKey key = jwks.Keys.Single();
        key.Kty.Should().Be("RSA");
        key.Alg.Should().Be(SecurityAlgorithms.RsaSha256);
        key.Use.Should().Be("sig");
        key.Kid.Should().NotBeNullOrWhiteSpace();
        key.N.Should().NotBeNullOrWhiteSpace();
        key.E.Should().NotBeNullOrWhiteSpace();
    }
}
