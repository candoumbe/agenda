using System;
using System.Text.Json;
using Agenda.API.Features.Appointments.v1.Create;
using Agenda.API.UnitTests.Helpers;
using Agenda.Ids;
using Bogus;
using FakeItEasy;
using FastEndpoints;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Agenda.API.UnitTests.Features.Appointments.v1.Create;

public class NewAppointmentInfoValidatorShould
{
    private readonly NewAppointmentInfoValidator _sut;
    private readonly IClock _clock;
    private static readonly Faker s_faker = new();
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, AllowTrailingCommas = true };


    public NewAppointmentInfoValidatorShould()
    {
        _clock = A.Fake<IClock>();
        _sut = Factory.CreateValidator<NewAppointmentInfoValidator>(services => services.AddSingleton(_clock));
    }

    public static TheoryData<GenericSerializable<NewAppointmentInfo>, GenericSerializable<ZonedDateTime>, XunitSerializableExpression<ValidationResult>, string> RequestCases
    {
        get
        {
            TheoryData<GenericSerializable<NewAppointmentInfo>, GenericSerializable<ZonedDateTime>, XunitSerializableExpression<ValidationResult>, string> cases = new();

            // Request without attendees
            {
                ZonedDateTime now = s_faker.Noda().Instant.Soon().InUtc();
                NewAppointmentInfo input = new()
                {
                    Id = new AppointmentId(),
                    Subject = s_faker.Lorem.Sentence(),
                    Location = s_faker.Address.FullAddress(),
                    StartDate = s_faker.Noda().ZonedDateTime.Past(reference: now).ToOffsetDateTime(),
                    EndDate = s_faker.Noda().ZonedDateTime.Future(reference: now).ToOffsetDateTime(),
                    Attendees = null
                };

                cases.Add(input,
                          now,
                          new XunitSerializableExpression<ValidationResult>
                          {
                              Value = validationResult => !validationResult.IsValid
                                                          && validationResult.Errors.Count == 1
                                                          && validationResult.Errors[0].PropertyName == nameof(NewAppointmentInfo.Attendees)
                                                          && validationResult.Errors[0].Severity == Severity.Error
                          },
                          "attendees cannot be null");
            }

            // Request with attendees but end date is before start date
            {
                ZonedDateTime now = s_faker.Noda().Instant.Soon().InUtc();
                NewAppointmentInfo input = new()
                {
                    Subject = s_faker.Lorem.Sentence(),
                    Location = s_faker.Lorem.Sentence(),
                    StartDate = s_faker.Noda().ZonedDateTime.Future(reference: now).ToOffsetDateTime(),
                    EndDate = s_faker.Noda().ZonedDateTime.Past(reference: now).ToOffsetDateTime(),
                    Attendees = []
                };
                cases.Add(input,
                          now,
                          new XunitSerializableExpression<ValidationResult>
                          {
                              Value = validationResult => !validationResult.IsValid
                                                          && validationResult.Errors.Count == 1
                                                          && validationResult.Errors[0].PropertyName == nameof(NewAppointmentInfo.EndDate)
                                                          && validationResult.Errors[0].Severity == Severity.Error
                          },
                          "end date cannot be before start date");
            }

            // Request with attendees and end date is after start date
            {
                ZonedDateTime now = s_faker.Noda().Instant.Soon().InUtc();
                NewAppointmentInfo input = new()
                {
                    Subject = s_faker.Lorem.Sentence(),
                    StartDate = s_faker.Noda().ZonedDateTime.Past(reference: now).ToOffsetDateTime(),
                    EndDate = s_faker.Noda().ZonedDateTime.Future(reference: now).ToOffsetDateTime(),
                    Attendees = []
                };
                cases.Add(input,
                          now,
                          new XunitSerializableExpression<ValidationResult>
                          {
                              Value = validationResult => validationResult.IsValid
                          },
                          "attendees and end date are valid");
            }


            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(RequestCases))]
    public void Validate_inputs(GenericSerializable<NewAppointmentInfo> input,
                                GenericSerializable<ZonedDateTime> now,
                                XunitSerializableExpression<ValidationResult> validationResultExpectation,
                                string reason)
    {
        // Arrange
        A.CallTo(() => _clock.GetCurrentInstant()).Returns(now.Value.ToInstant());
        NewAppointmentInfo inputValue = input;

        // Act
        ValidationResult validationResult = _sut.Validate(input);

        // Assert
        validationResult.Should().Match(validationResultExpectation.Value, reason);
    }
}