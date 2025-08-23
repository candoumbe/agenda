using FsCheck.Xunit;

namespace Agenda.Ids.UnitTests;

using FluentAssertions;

using System;
using System.Collections.Generic;

using Xunit.Categories;

[UnitTest]
public class AppointmentIdTests
{
    [Property]
    public void Given_value_is_not_empty_Then_Value_should_be_equal_to_value(Guid expected)
    {
        // Act
        AppointmentId appointmentId = new AppointmentId(expected);

        // Assert
        appointmentId.Value.Should().Be(expected);
    }

    [Property]
    public void Two_ids_built_of_the_same_value_should_be_equal(Guid value)
    {
        // Act
        AppointmentId first = new AppointmentId(value);
        AppointmentId second = new AppointmentId(value);

        // Assert
        first.Should().Be(second);
    }

    public static IEnumerable<object[]> TryParseCases
    {
        get
        {
            {
                Guid value = Guid.NewGuid();
                yield return new object[] { value.ToString(), true, new AppointmentId(value) };
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
}