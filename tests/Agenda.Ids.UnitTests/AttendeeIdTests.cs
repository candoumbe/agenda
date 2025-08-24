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

    public static IEnumerable<object[]> TryParseCases
    {
        get
        {
            {
                Guid value = Guid.NewGuid();
                yield return new object[] { value.ToString(), true, new AttendeeId(value) };
            }
            {
                string value = string.Empty;
                yield return new object[] { value, false, null };
            }

            {
                string value = null;
                yield return new object[] { value, false, null };
            }
        }
    }

    [Theory]
    [MemberData(nameof(TryParseCases))]
    public void Given_input_is_a_valid_guid_Then_TryParse_should_parse_correctly(string input, bool expected, AttendeeId expectedId)
    {
        // Act
        bool actual = AttendeeId.TryParse(input, out AttendeeId actualId);

        // Assert
        actual.Should().Be(expected);
        actualId.Should().Be(expectedId);
    }
}