#if NET9_0_OR_GREATER
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Agenda.AppHost;
using AwesomeAssertions;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.ArchitecturalTests
{
    [UnitTest]
    public class ContainerImagesArchitectureTests
    {
        private const string AppHostResourceName = "Agenda.API.ArchitecturalTests.AppHostSources.AppHost.cs";

        private static string ReadAppHostSource()
        {
            using Stream stream = typeof(ContainerImagesArchitectureTests).Assembly.GetManifestResourceStream(AppHostResourceName);

            stream.Should().NotBeNull("AppHost.cs must be embedded as a resource so the architecture test can scan it");

            using StreamReader reader = new(stream!);
            return reader.ReadToEnd();
        }

        [Fact]
        public void Manifest_should_be_loadable_and_contain_every_required_image()
        {
            // Arrange
            string[] expectedKeys = [ContainerImages.PostgresKey, ContainerImages.RabbitMqKey, ContainerImages.KeycloakKey];

            // Act
            IReadOnlyDictionary<string, PinnedContainerImage> images = ContainerImages.All;

            // Assert
            images.Keys
                .Should()
                .BeEquivalentTo(expectedKeys,
                    "every image consumed by the AppHost must be declared in container-images.json");

            foreach (KeyValuePair<string, PinnedContainerImage> entry in images)
            {
                entry.Value.Registry.Should().NotBeNullOrWhiteSpace();
                entry.Value.Image.Should().NotBeNullOrWhiteSpace();
                entry.Value.Tag.Should().NotBeNullOrWhiteSpace();
                entry.Value.Tag.Should().NotBe("latest", "pinned tags must be reproducible");
            }
        }

        [Fact]
        public void AppHost_should_resolve_every_container_image_from_the_manifest()
        {
            // Arrange
            string source = ReadAppHostSource();

            // Act + Assert
            foreach (string accessor in new[] { nameof(ContainerImages.Postgres), nameof(ContainerImages.RabbitMq), nameof(ContainerImages.Keycloak) })
            {
                source.Should().Contain($"ContainerImages.{accessor}",
                    $"AppHost.cs must reference ContainerImages.{accessor} so the runtime image matches the pinned manifest entry");
            }
        }

        [Fact]
        public void AppHost_should_not_hard_code_container_image_literals()
        {
            // Arrange
            string source = ReadAppHostSource();

            // An image-like literal looks like "name:tag" where the tag contains at least one digit
            // (e.g. "library/postgres:17-alpine"). The digit requirement discriminates image tags
            // from unrelated colon-separated literals such as npm scripts ("start:dev").
            MatchCollection matches = Regex.Matches(source, "\"[a-z0-9][A-Za-z0-9._/-]*:[A-Za-z0-9._-]*\\d[A-Za-z0-9._-]*\"");

            // Act
            IEnumerable<string> imageLikeLiterals = matches.Select(match => match.Value);

            // Assert
            imageLikeLiterals
                .Should()
                .BeEmpty("AppHost.cs must resolve images via ContainerImages — hard-coded image references drift from container-images.json");
        }
    }
}
#endif
