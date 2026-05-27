using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.IntegrationTests.Fixtures;
using Aspire.Hosting;
using AwesomeAssertions;
using Bogus;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.IntegrationTests.Appointments.v1.GetById;

[IntegrationTest]
public sealed class GetAppointmentByIdHeadShould
{
    private static readonly Faker s_faker = new();
    private readonly HttpClient _client;

    public GetAppointmentByIdHeadShould(AgendaApplicationFixture fixture)
    {
        _client = fixture.ApiClient;
    }

    [Fact]
    public async Task Return_ok_and_link_header_when_resource_exists()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string appointmentId = await CreateAppointmentAsync(cancellationToken);
        using HttpRequestMessage request = new(HttpMethod.Head, $"/appointments/{appointmentId}");

        // Act
        using HttpResponseMessage response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        if (response.Headers.TryGetValues("Link", out System.Collections.Generic.IEnumerable<string> linkValues))
        {
            linkValues.Should()
                .ContainSingle(link => link.Contains("rel=\"self\"", StringComparison.OrdinalIgnoreCase));
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        body.Should().BeEmpty();
    }

    private async Task<string> CreateAppointmentAsync(CancellationToken cancellationToken)
    {
        string payload = BuildCreateAppointmentPayload();
        using StringContent content = new(payload, Encoding.UTF8, "application/json");
        using HttpResponseMessage createResponse = await _client.PostAsync("/appointments", content, cancellationToken);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        await using System.IO.Stream responseStream = await createResponse.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument jsonDocument = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        string id = jsonDocument.RootElement
                                .GetProperty("resource")
                                .GetProperty("id")
                                .GetString();

        id.Should().NotBeNullOrWhiteSpace();
        return id;
    }

    private static string BuildCreateAppointmentPayload()
    {
        string appointmentId = Guid.NewGuid().ToString();
        string attendeeId = Guid.NewGuid().ToString();
        DateTimeOffset startDate = DateTimeOffset.UtcNow.AddHours(1);
        DateTimeOffset endDate = startDate.AddHours(1);

        return $$"""
                 {
                   "id": "{{appointmentId}}",
                   "subject": "{{s_faker.Lorem.Sentence()}}",
                   "location": "{{s_faker.Address.City()}}",
                   "startDate": "{{startDate:O}}",
                   "endDate": "{{endDate:O}}",
                   "attendees": [
                     {
                       "id": "{{attendeeId}}",
                       "name": "{{s_faker.Name.FullName()}}",
                       "email": "{{s_faker.Internet.Email()}}",
                       "phoneNumber": "{{s_faker.Phone.PhoneNumber()}}"
                     }
                   ]
                 }
                 """;
    }
}
