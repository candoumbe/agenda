using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit.Categories;

namespace Agenda.Ids.UnitTests;
[UnitTest]
public class AttendeeIdTests
{
    [Fact]
    public void Given_value_is_not_empty_Then_Value_should_be_equal_to_value()
    {
        // Arrange
        Guid expected = Guid.NewGuid();

        // Act
        AttendeeId appointmentId = new AttendeeId(expected);

        // Assert
        appointmentId.Value.Should().Be(expected);
    }

    [Fact]
    public void Two_ids_built_of_the_same_value_should_be_equal()
    {
        // Arrange
        Guid guid = Guid.NewGuid();

        // Act
        AttendeeId first = new AttendeeId(guid);
        AttendeeId second = new AttendeeId(guid);

        // Assert
        first.Should().Be(second);
    }
}