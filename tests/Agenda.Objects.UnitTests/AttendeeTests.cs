using System;
using Agenda.Ids;
using FluentAssertions;
using Xunit;
using Xunit.Categories;

namespace Agenda.Objects.UnitTests;
[Feature("Agenda")]
[UnitTest]
public class AttendeeTests
{
    [Fact]
    public void ChangingAttendee_Name_ToNull_Throws_ArgumentNullException()
    {
        // Arrange
        Attendee attendee = new(AttendeeId.New(), "Bruce");

        // Act
        Action action = () => attendee.ChangeNameTo(null);

        // Assert
        action.Should()
            .Throw<ArgumentNullException>($"Attendee's {nameof(Attendee.Name)} cannot be changed to null");
    }

    [Fact]
    public void CreatingAttendee_With_Null_Name_Throws_ArgumentNullException()
    {
        // Act
        Action action = () => _ = new Attendee(AttendeeId.New(), null);

        // Assert
        action.Should()
            .Throw<ArgumentNullException>($"Attendee's {nameof(Attendee.Name)} cannot be null");
    }

    [Theory]
    [InlineData("bruce Wayne", "Bruce Wayne")]
    [InlineData("Cyrille-alexandre", "Cyrille-Alexandre")]
    public void Ctor_Builds_ValidObject(string name, string expectedName)
    {
        // Arrange
        AttendeeId id = AttendeeId.New();

        // Act
        Attendee attendee = new(id, name);

        // Assert
        attendee.Id.Should()
            .Be(id);
        attendee.Name.Should()
            .Be(expectedName);
        attendee.PhoneNumber.Should()
            .BeNull();
        attendee.Email.Should()
            .BeNull();

        attendee.Appointments.Should()
            .BeEmpty();
    }
}