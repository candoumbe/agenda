using System;
using Xunit;

namespace Agenda.API.IntegrationTests.Fixtures;

public sealed class AgendaApplicationTestingBuilderShould
{
    [Theory]
    [InlineData(null, null, 120)]
    [InlineData("false", null, 120)]
    [InlineData(null, "false", 120)]
    [InlineData("true", null, 300)]
    [InlineData(null, "true", 300)]
    [InlineData("TRUE", null, 300)]
    [InlineData(null, "TRUE", 300)]
    public void Resolve_start_stop_timeout_based_on_ci_signals(string ci,
                                                                string githubActions,
                                                                int expectedSeconds)
    {
        // Arrange
        TimeSpan expected = TimeSpan.FromSeconds(expectedSeconds);

        // Act
        TimeSpan actual = AgendaApplicationTestingBuilder.ResolveStartStopTimeout(ci, githubActions);

        // Assert
        Assert.Equal(expected, actual);
    }
}
