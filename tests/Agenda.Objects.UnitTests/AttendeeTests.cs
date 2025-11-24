using System;
using Agenda.Ids;
using AwesomeAssertions;
using Bogus;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.Objects.UnitTests;
[Feature("Agenda")]
[UnitTest]
public class AttendeeTests
{
    private static readonly Faker s_faker = new();

    [Fact]
    public void Given_an_attendee_When_changing_its_name_to_null_Then_an_ArgumentNullException_should_be_thrown()
    {
        // Arrange
        Attendee attendee = new(AttendeeId.New(), "Bruce");

        // Act
        Action changingNameToNull = () => attendee.ChangeNameTo(null);

        // Assert
        changingNameToNull.Should().Throw<ArgumentNullException>($"Attendee's {nameof(Attendee.Name)} cannot be changed to null");
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

    [Theory]
    [InlineData("b.wayne@wayne-enterprise.com", "bruce.wayne@wayne-enterprise", "bruce.wayne@wayne-enterprise")]
    public void Given_an_attendee_When_updating_its_email_Then_its_email_should_have_expected_value(string initialEmail, string newEmail, string expectedEmail)
    {
        // Arrange
        Attendee attendee = new(AttendeeId.New(), "Bruce", initialEmail);

        // Act
        attendee.ChangeEmail(newEmail);

        // Assert
        attendee.Email.Should().Be(expectedEmail);
    }

    [Theory]
    [InlineData("username", """Email does not have a "domain" part and no "@" sign""" )]
    [InlineData("@domain", """Email does not have a "username" part and no "@" sign""" )]
    public void Given_an_attendee_When_updating_its_email_with_an_invalid_email_Then_an_InvalidEmailException_should_be_thrown(string newEmail, string reason)
    {
        // Arrange
        Attendee attendee = new(AttendeeId.New(), "Bruce", newEmail);

        // Act
        Action updatingEmail = () => attendee.ChangeEmail(newEmail);

        // Assert
        updatingEmail.Should().Throw<InvalidEmailException>(because: reason);
    }

    [Fact]
    public void Given_an_attendee_with_an_existing_email_When_changing_its_email_to_null_Then_an_ArgumentNullException_should_be_thrown()
    {
        // Arrange
        string initialEmail = s_faker.Internet.Email(firstName: "Bruce", lastName: "Wayne");
        Attendee attendee = new(AttendeeId.New(), "Bruce", initialEmail);

        // Act
        Action changingEmailToNull = () => attendee.ChangeEmail(null);

        // Assert
        changingEmailToNull.Should().Throw<ArgumentNullException>($"Attendee's {nameof(Attendee.Email)} cannot be changed to null");
    }

    [Fact]
    public void Given_an_attendee_with_an_existing_email_When_resetting_its_email_Then_its_email_should_be_null()
    {
        // Arrange
        string initialEmail = s_faker.Internet.Email(firstName: "Bruce", lastName: "Wayne");
        Attendee attendee = new(AttendeeId.New(), "Bruce", initialEmail);

        // Act
        attendee.ReinitializeEmail();

        // Assert
        attendee.Email.Should().BeNull();
    }
}