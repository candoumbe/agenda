using System.Collections.Generic;
using Agenda.Ids;
using FastEndpoints;
using FluentValidation;
using SystemTextJsonPatch.Operations;

namespace Agenda.API.Resources.Appointments.v1.Update;

/// <summary>
/// Validator for the PATCH request over the appointment resource.
/// </summary>
public class PatchAppointmentInfoRequestValidator : Validator<PatchRequest<AppointmentId, PatchAppointmentRequest>>
{
    /// <summary>
    /// Builds a new <see cref="PatchAppointmentInfoRequestValidator"/> instance.
    /// </summary>
    public PatchAppointmentInfoRequestValidator()
    {
        RuleFor(app => app.Id)
            .NotNull();
        RuleFor(patch => patch.Operations)
            .NotNull();
        When(input => input.Operations != null,
             () =>
             {
                 When(input => input.Operations != null,
                      () => RuleFor(input => input.Operations)
                          .Must(operations => operations.AtLeastOnce(op => op.Op == nameof(OperationType.Test)))
                          .WithSeverity(input => input.Operations switch
                          {
                              {Count: 0} => Severity.Error,
                              _          => Severity.Warning
                          } )
                     );
             }
            );
    }
}