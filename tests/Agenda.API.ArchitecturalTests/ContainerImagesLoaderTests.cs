#if NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Agenda.AppHost;
using AwesomeAssertions;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.ArchitecturalTests
{
    [UnitTest]
    public class ContainerImagesLoaderShould
    {
        private static Stream JsonStream(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));

        [Fact]
        public void Parse_a_valid_manifest_into_pinned_images()
        {
            // Arrange
            const string json = """
                {
                  "images": {
                    "postgres": { "registry": "docker.io", "image": "library/postgres", "tag": "17-alpine" },
                    "keycloak": { "registry": "quay.io",   "image": "keycloak/keycloak", "tag": "26.5" }
                  }
                }
                """;

            // Act
            IReadOnlyDictionary<string, PinnedContainerImage> images = ContainerImages.Parse(JsonStream(json));

            // Assert
            images.Should().HaveCount(2);
            images["postgres"].Should().Be(new PinnedContainerImage("docker.io", "library/postgres", "17-alpine"));
            images["keycloak"].Should().Be(new PinnedContainerImage("quay.io", "keycloak/keycloak", "26.5"));
        }

        [Fact]
        public void Throw_when_the_manifest_is_empty()
        {
            // Arrange
            const string json = """{ "images": {} }""";

            // Act
            Action act = () => ContainerImages.Parse(JsonStream(json));

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*empty*");
        }

        [Theory]
        [InlineData("""{ "images": { "postgres": { "registry": "",          "image": "library/postgres", "tag": "17" } } }""")]
        [InlineData("""{ "images": { "postgres": { "registry": "docker.io", "image": "",                 "tag": "17" } } }""")]
        [InlineData("""{ "images": { "postgres": { "registry": "docker.io", "image": "library/postgres", "tag": ""   } } }""")]
        [InlineData("""{ "images": { "postgres": { "registry": "docker.io", "image": "library/postgres"              } } }""")]
        public void Throw_when_an_entry_is_incomplete(string json)
        {
            // Act
            Action act = () => ContainerImages.Parse(JsonStream(json));

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*incomplete*");
        }

        [Fact]
        public void Reject_a_null_stream()
        {
            // Act
            Action act = () => ContainerImages.Parse(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }

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
