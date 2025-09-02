using System;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features.Appointments.v1.Update;
using Agenda.Ids;
using FastEndpoints;
using FluentAssertions;
using Xunit;

namespace Agenda.API.UnitTests.Features.v1.Patch;

public class PatchAppointmentByIdEndpointShould
{
    private readonly PatchAppointmentByIdEndpoint _sut = Factory.Create<PatchAppointmentByIdEndpoint>();

    [Fact]
    public void Have_expected_route()
    {
        // Assert
        string[] routes = _sut.Definition.Routes;
        routes.Should()
            .HaveCount(1)
            .And
            .ContainSingle("/appointments/{id}");

        string[] methods = _sut.Definition.Verbs;
        methods.Should().HaveCount(1).And.ContainSingle("PATCH");
    }

    [Fact]
    public async Task Return_NoContent()
    {
        PatchRequest<AppointmentId, PatchAppointmentRequest> request = new()
        {
            Id = AppointmentId.New(),
            Operations = []
        };

        // Act
        Func<Task> patch = async () => _ = await _sut.ExecuteAsync(request, CancellationToken.None);

        // Assert
        await patch.Should().NotThrowAsync();

    }
}