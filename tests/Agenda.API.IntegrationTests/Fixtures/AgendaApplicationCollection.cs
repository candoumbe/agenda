using Xunit;

namespace Agenda.API.IntegrationTests.Fixtures;

[CollectionDefinition("AgendaApplication")]
public sealed class AgendaApplicationCollection : ICollectionFixture<AgendaApplicationFixture>
{
}