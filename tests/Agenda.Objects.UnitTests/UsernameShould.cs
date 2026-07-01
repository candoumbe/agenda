using FsCheck;
using FsCheck.Xunit;
using AwesomeAssertions;
using Xunit.OpenCategories.V3;
using Xunit;
using System;

namespace Agenda.Objects.UnitTests;

 [UnitTest]
public class UsernameShould
{
    [Property]
    public void Create_username_when_input_is_valid(NonWhiteSpaceString validUsernameGenerator)
    {
        // Arrange
        string validUsername = validUsernameGenerator.Get;

        // Act
        Username username = Username.FromString(validUsername);

        // Assert
        username.Value.Should().Be(validUsername);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Throw_ArgumentException_when_input_is_invalid(string invalidUsername)
    {

        // Act 
        Action buildingUsernameFromInvalidInput = () => _ = Username.FromString(invalidUsername);

        // Assert
        buildingUsernameFromInvalidInput.Should().Throw<ArgumentException>()
            .WithMessage("Username cannot be null or whitespace*")
            .And.ParamName.Should().Be("username");
    }
}