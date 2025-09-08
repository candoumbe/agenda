using System;
using System.Collections.Generic;
using System.Text.Json;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.Appointments.v1.Create;
using Agenda.API.Features.v1.Appointments;
using Agenda.API.UnitTests.Helpers;
using Bogus;
using FastEndpoints;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NodaTime.Testing;
using Xunit;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Agenda.API.UnitTests.Features.Appointments.v1.Create
{
    public class CreateAppointmentRequestValidatorShould
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly NewAppointmentInfoValidator _sut;
        private static readonly Faker s_faker = new();
        private static readonly Faker<AttendeeInfo> s_attendeeFaker = new();
        private static readonly Instant s_instantReference = s_faker.Noda().Instant.Recent();
        private static readonly JsonSerializerOptions s_jsonSerializerOptions;


        public CreateAppointmentRequestValidatorShould(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _sut = Factory.CreateValidator<NewAppointmentInfoValidator>(services => { services.AddSingleton<IClock>(new FakeClock(s_instantReference)); });
        }

        static CreateAppointmentRequestValidatorShould()
        {
            s_jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, AllowTrailingCommas = true };
            s_jsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        public static TheoryData<GenericSerializable<NewAppointmentInfo>, XunitSerializableExpression<ValidationResult>, string> CreateAppointmentRequestCases
        {
            get
            {
                TheoryData<GenericSerializable<NewAppointmentInfo>, XunitSerializableExpression<ValidationResult>, string> cases = new();
                {
                    OffsetDateTime start = s_faker.Noda().ZonedDateTime.Past().ToOffsetDateTime();
                    OffsetDateTime end = s_faker.Noda().ZonedDateTime.Future().ToOffsetDateTime();

                    cases.Add(new NewAppointmentInfo()
                              {
                                  StartDate = start,
                                  EndDate = end,
                                  Subject = s_faker.Lorem.Sentence(),
                                  Location = null,
                                  Attendees = null,
                             },
                              new XunitSerializableExpression<ValidationResult>
                              {
                                  Value = validationResult => !validationResult.IsValid
                                                              && validationResult.Errors.Count == 1
                                                              && validationResult.Errors[0].PropertyName == nameof(NewAppointmentInfo.Attendees)
                                                              && validationResult.Errors[0].Severity == Severity.Error
                              },
                              "attendees cannot be null");
                }
                {
                    OffsetDateTime start = s_faker.Noda().ZonedDateTime.Past().ToOffsetDateTime();
                    OffsetDateTime end = s_faker.Noda().ZonedDateTime.Future().ToOffsetDateTime();

                    cases.Add(new NewAppointmentInfo()
                              {
                                  StartDate = start,
                                  EndDate = end,
                                  Subject = s_faker.Lorem.Sentence(),
                                  Location = null,
                                  Attendees = [],
                              },
                              new XunitSerializableExpression<ValidationResult>
                              {
                                  Value = validationResult => validationResult.IsValid
                              },
                              $"""
                               "{nameof(NewAppointmentInfo.Attendees)}" can be empty.
                               """);
                }

                {
                    OffsetDateTime start = s_faker.Noda().Instant.Past(reference: s_instantReference).InUtc().ToOffsetDateTime();
                    OffsetDateTime end = s_faker.Noda().Instant.Past(reference: start.ToInstant()).InUtc().ToOffsetDateTime();
                    cases.Add(new NewAppointmentInfo()
                              {
                                  StartDate = start,
                                  EndDate = end,
                                  Subject = s_faker.Lorem.Sentence(),
                                  Location = null,
                                  Attendees = new List<AttendeeInfo>
                                    {
                                      new AttendeeInfo
                                      {
                                          Email = s_faker.Internet.Email(),
                                          Name = s_faker.Name.FullName(),
                                          PhoneNumber = s_faker.Phone.PhoneNumber()
                                      }
                                  },
                              },
                              new XunitSerializableExpression<ValidationResult>
                              {
                                  Value = validationResult => !validationResult.IsValid
                                                              && validationResult.Errors.Count == 1
                                                              && validationResult.Errors[0].PropertyName == nameof(NewAppointmentInfo.EndDate)
                                                              && validationResult.Errors[0].Severity == Severity.Error,
                              },
                              $"""
                               "{nameof(NewAppointmentInfo.EndDate)}" must be after "{nameof(NewAppointmentInfo.StartDate)}".
                               """);
                }

                {
                    OffsetDateTime end = s_faker.Noda().Instant.Past(reference: s_instantReference).InUtc().ToOffsetDateTime();
                    OffsetDateTime start = s_faker.Noda().Instant.Past(reference: end.ToInstant()).InUtc().ToOffsetDateTime();
                    cases.Add(new NewAppointmentInfo()
                              {
                                  StartDate = start,
                                  EndDate = end,
                                  Subject = s_faker.Lorem.Sentence(),
                                  Location = null,
                                  Attendees =
                                  [
                                      new AttendeeInfo
                                      {
                                          Email = s_faker.Internet.Email(),
                                          Name = s_faker.Name.FullName(),
                                          PhoneNumber = s_faker.Phone.PhoneNumber()
                                      }
                                  ],
                              },
                              new XunitSerializableExpression<ValidationResult>
                              {
                                  Value = validationResult => !validationResult.IsValid
                                                              && validationResult.Errors.Count == 1
                                                              && validationResult.Errors[0].PropertyName == nameof(NewAppointmentInfo.EndDate)
                                                              && validationResult.Errors[0].Severity == Severity.Error
                              },
                              "newEndDateTime cannot be in the past");
                }

                return cases;
            }
        }

        [Theory]
        [MemberData(nameof(CreateAppointmentRequestCases))]
        public void Given_a_request_When_validating_Then_validationResult_should_match_expectations(GenericSerializable<NewAppointmentInfo> request,
                                                                                                    XunitSerializableExpression<ValidationResult> validationResultExpectation,
                                                                                                    string reason)
        {
            // Arrange

            _outputHelper.WriteLine($"Current date : {s_instantReference.InUtc().ToOffsetDateTime()}");
            _outputHelper.WriteLine($"Request : {request.Value.Jsonify(s_jsonSerializerOptions)}");

            // Act
            ValidationResult validationResult = _sut.Validate(request.Value);

            _outputHelper.WriteLine($"ValidationResult : {validationResult.Jsonify(s_jsonSerializerOptions)}");

            // Assert
            validationResult.Should().Match(validationResultExpectation.Value, reason);
        }
    }
}