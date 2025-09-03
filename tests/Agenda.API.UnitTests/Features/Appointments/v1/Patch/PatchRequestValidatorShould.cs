using System;
using System.Linq.Expressions;
using System.Text.Json;
using Agenda.API.Features.Appointments.v1.Update;
using Agenda.Ids;
using FastEndpoints;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using SystemTextJsonPatch.Operations;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.UnitTests.Features.Appointments.v1.Patch;

[UnitTest]
public class PatchRequestValidatorShould(ITestOutputHelper outputHelper)
{
    private readonly PatchAppointmentInfoRequestValidator _sut = Factory.CreateValidator<PatchAppointmentInfoRequestValidator>(services => { });
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, AllowTrailingCommas = true };

    public static TheoryData<PatchRequest<AppointmentId, PatchAppointmentRequest>, Expression<Func<ValidationResult, bool>>, string> InvalidRequestCases => new()
    {
        {
            new PatchRequest<AppointmentId, PatchAppointmentRequest>()
            {
                Id = AppointmentId.New(), Operations = []
            },
            validationResult => !validationResult.IsValid
                                && validationResult.Errors.Count == 1
                                && validationResult.Errors[0].Severity == Severity.Error
                                && validationResult.Errors[0].PropertyName == nameof(PatchRequest<AppointmentId, PatchAppointmentRequest>.Operations),
            "Patch document must have one operation at least"
        },
        {
            new PatchRequest<AppointmentId, PatchAppointmentRequest>()
            {
                Id = AppointmentId.New(),
                Operations = null
            },
            validationResult => !validationResult.IsValid
                                && validationResult.Errors.Count == 1
                                && validationResult.Errors[0].Severity == Severity.Error
                                && validationResult.Errors[0].PropertyName == $"{nameof(PatchRequest<AppointmentId, PatchAppointmentRequest>.Operations)}",
            "Operations is required and cannot be null"
        },
        { new PatchRequest<AppointmentId, PatchAppointmentRequest>()
        {
            Id = AppointmentId.New(),
            Operations =
            [
                new Operation<PatchAppointmentRequest>(nameof(OperationType.Add),
                                                       $"/{nameof(PatchAppointmentRequest.Subject)}",
                                                       from: null,
                                                       value: "New subject")
            ]
        }, validationResult => !validationResult.IsValid
                               && validationResult.Errors.Count == 1
                               && validationResult.Errors[0].Severity == Severity.Warning
                               && validationResult.Errors[0].PropertyName == $"{nameof(PatchRequest<AppointmentId, PatchAppointmentRequest>.Operations)}",
            "Operations must provide at least one test operation" }
    };

    [Theory]
    [MemberData(nameof(InvalidRequestCases))]
    public void Reject_invalid_requests(PatchRequest<AppointmentId, PatchAppointmentRequest> request,
                                        Expression<Func<ValidationResult, bool>> failureExpectation,
                                        string reason)
    {
        // Arrange
        outputHelper.WriteLine(JsonSerializer.Serialize(request, s_jsonSerializerOptions));


        // Act
        ValidationResult validationResult = _sut.Validate(request);

        // Assert
        outputHelper.WriteLine(JsonSerializer.Serialize(validationResult, s_jsonSerializerOptions));
        validationResult.Should().Match(failureExpectation, reason);
    }
}