#if NET9_0_OR_GREATER
using Agenda.AppHost;
using AwesomeAssertions;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.ArchitecturalTests
{
    [UnitTest]
    public class PinnedContainerImageShould
    {
        [Fact]
        public void Omit_the_docker_io_registry_in_the_full_reference()
        {
            // Arrange
            PinnedContainerImage image = new("docker.io", "library/postgres", "17-alpine");

            // Act + Assert
            image.IsDockerHub.Should().BeTrue();
            image.FullReference.Should().Be("library/postgres:17-alpine");
        }

        [Fact]
        public void Detect_docker_hub_case_insensitively()
        {
            // Arrange
            PinnedContainerImage image = new("DOCKER.IO", "library/rabbitmq", "4.2-management");

            // Act + Assert
            image.IsDockerHub.Should().BeTrue();
        }

        [Fact]
        public void Prepend_a_non_docker_hub_registry_to_the_full_reference()
        {
            // Arrange
            PinnedContainerImage image = new("quay.io", "keycloak/keycloak", "26.5");

            // Act + Assert
            image.IsDockerHub.Should().BeFalse();
            image.FullReference.Should().Be("quay.io/keycloak/keycloak:26.5");
        }
    }
}
#endif
