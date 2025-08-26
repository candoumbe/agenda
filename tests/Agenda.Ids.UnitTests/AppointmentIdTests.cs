using System;
using System.Collections.Generic;
using FluentAssertions;
using FsCheck.Xunit;
using Xunit.Categories;

namespace Agenda.Ids.UnitTests;
[UnitTest]
public class AppointmentIdTests
{
    [Property]
    public void Given_value_is_not_empty_Then_Value_should_be_equal_to_value(Guid expected)
    {
        // Act
        AppointmentId appointmentId = AppointmentId.From(expected);

        // Assert
        appointmentId.Value.Should().Be(expected);
    }

    [Property]
    public void Two_ids_built_of_the_same_value_should_be_equal(Guid value)
    {
        // Act
        AppointmentId first = AppointmentId.From(value);
        AppointmentId second = AppointmentId.From(value);

        // Assert
        first.Should().Be(second);
    }

#if !NET8_0_OR_GREATER
    public static IEnumerable<object[]> TryParseCases
    {
        get
        {
            {
                Guid value = Guid.NewGuid();
                yield return new object[] { value.ToString(), true, AppointmentId.From(value) };
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
    public void Given_input_is_a_valid_guid_Then_TryParse_should_parse_correctly(string input, bool expected, AppointmentId expectedId)
    {
        // Act
        bool actual = AppointmentId.TryParse(input, out AppointmentId actualId);

        // Assert
        actual.Should().Be(expected);
        actualId.Should().Be(expectedId);
    }
#endif
}