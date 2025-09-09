using System.Collections.Generic;
using System.Text.Json;
using Agenda.API.Features.Appointments.v1.Update;
using Agenda.API.UnitTests.Helpers;
using Agenda.Ids;
using FastEndpoints;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using SystemTextJsonPatch.Operations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Agenda.API.UnitTests.Features.Appointments.v1.Patch;

[UnitTest]
public class PatchRequestValidatorShould(ITestOutputHelper outputHelper)
{
    private readonly PatchAppointmentInfoRequestValidator _sut = Factory.CreateValidator<PatchAppointmentInfoRequestValidator>(services => { });
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, AllowTrailingCommas = true };

    public static TheoryData<GenericSerializable<PatchRequest<AppointmentId, PatchAppointmentRequest>>, XunitSerializableExpression<ValidationResult>, string> InvalidRequestCases => new()
    {
        {
            new PatchRequest<AppointmentId, PatchAppointmentRequest>()
            {
                Id = new AppointmentId(),
                Operations = new List<Operation<PatchAppointmentRequest>>()
            },
            new XunitSerializableExpression<ValidationResult>
            {
                Value = validationResult => !validationResult.IsValid
                                            && validationResult.Errors.Count == 1
                                            && validationResult.Errors[0].Severity == Severity.Error
                                            && validationResult.Errors[0].PropertyName == nameof(PatchRequest<AppointmentId, PatchAppointmentRequest>.Operations),
            },
            "Patch document must have one operation at least"
        },
        {
            new PatchRequest<AppointmentId, PatchAppointmentRequest>() { Id = new AppointmentId(), Operations = null },
            new XunitSerializableExpression<ValidationResult>()
            {
                Value =validationResult => !validationResult.IsValid
                                           && validationResult.Errors.Count == 1
                                           && validationResult.Errors[0].Severity == Severity.Error
                                           && validationResult.Errors[0].PropertyName == $"{nameof(PatchRequest<AppointmentId, PatchAppointmentRequest>.Operations)}"
            },
            "Operations is required and cannot be null"
        },
        {
            new PatchRequest<AppointmentId, PatchAppointmentRequest>()
            {
                Id = new AppointmentId(),
                Operations = new List<Operation<PatchAppointmentRequest>>
                {
                    new Operation<PatchAppointmentRequest>(nameof(OperationType.Add),
                                                           $"/{nameof(PatchAppointmentRequest.Subject)}",
                                                           from: null,
                                                           value: "New subject")
                }
            },
            new XunitSerializableExpression<ValidationResult>
            {
                Value = validationResult => !validationResult.IsValid
                                            && validationResult.Errors.Count == 1
                                            && validationResult.Errors[0].Severity == Severity.Warning
                                            && validationResult.Errors[0].PropertyName == $"{nameof(PatchRequest<AppointmentId, PatchAppointmentRequest>.Operations)}"
            },
            "Operations must provide at least one test operation"
        }
    };

    [Theory]
    [MemberData(nameof(InvalidRequestCases))]
    public void Reject_invalid_requests(GenericSerializable<PatchRequest<AppointmentId, PatchAppointmentRequest>> request,
                                        XunitSerializableExpression<ValidationResult> failureExpectation,
                                        string reason)
    {
        // Arrange
        outputHelper.WriteLine(JsonSerializer.Serialize(request, s_jsonSerializerOptions));


        // Act
        ValidationResult validationResult = _sut.Validate(request.Value);

        // Assert
        outputHelper.WriteLine(JsonSerializer.Serialize(validationResult, s_jsonSerializerOptions));
        validationResult.Should().Match(failureExpectation.Value, reason);
    }
}