using System.Net.Http;
using Agenda.API.IntegrationTests.Fixtures;
using AwesomeAssertions;
using Xunit;
using Xunit.OpenCategories.V3;


namespace Agenda.API.IntegrationTests;

[IntegrationTest]
[Collection("AgendaApplication")]
public class AppHostShould
{
    private readonly HttpClient _client;

    public AppHostShould(AgendaApplicationFixture fixture)
    {
        _client = fixture.ApiClient;
    }

    [Fact]
    public void Expose_api_client_when_fixture_is_initialized()
    {
        _client.Should().NotBeNull();
        _client.BaseAddress.Should().NotBeNull();
    }
}