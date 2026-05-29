using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Agenda.API.Authentication;
using AwesomeAssertions;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.UnitTests.Authentication
{
    [UnitTest]
    public class RealmRolesClaimsTransformationShould
    {
        private readonly RealmRolesClaimsTransformation _sut = new();

        private static ClaimsPrincipal BuildPrincipal(params Claim[] claims)
        {
            ClaimsIdentity identity = new(claims, authenticationType: "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public async Task Flattens_realm_access_roles_into_role_claims()
        {
            // Arrange
            ClaimsPrincipal principal = BuildPrincipal(
                new Claim("realm_access", """{"roles":["agenda-user","agenda-admin"]}"""));

            // Act
            ClaimsPrincipal transformed = await _sut.TransformAsync(principal);

            // Assert
            string[] roles = [.. transformed.FindAll(ClaimTypes.Role).Select(c => c.Value)];
            roles.Should().BeEquivalentTo("agenda-user", "agenda-admin");
        }

        [Fact]
        public async Task Is_idempotent_when_role_claims_already_present()
        {
            // Arrange
            ClaimsPrincipal principal = BuildPrincipal(
                new Claim("realm_access", """{"roles":["agenda-user"]}"""),
                new Claim(ClaimTypes.Role, "agenda-user"));

            // Act
            ClaimsPrincipal transformed = await _sut.TransformAsync(principal);

            // Assert
            transformed.FindAll(ClaimTypes.Role).Should().ContainSingle()
                .Which.Value.Should().Be("agenda-user");
        }

        [Fact]
        public async Task Is_noop_when_realm_access_claim_missing()
        {
            // Arrange
            ClaimsPrincipal principal = BuildPrincipal(new Claim("sub", "alice"));

            // Act
            ClaimsPrincipal transformed = await _sut.TransformAsync(principal);

            // Assert
            transformed.FindAll(ClaimTypes.Role).Should().BeEmpty();
        }

        [Fact]
        public async Task Is_noop_when_realm_access_has_no_roles_array()
        {
            // Arrange
            ClaimsPrincipal principal = BuildPrincipal(
                new Claim("realm_access", """{"other":"value"}"""));

            // Act
            ClaimsPrincipal transformed = await _sut.TransformAsync(principal);

            // Assert
            transformed.FindAll(ClaimTypes.Role).Should().BeEmpty();
        }
    }
}
