using System;
using FastEndpoints;
using FluentValidation;
using NodaTime;

namespace Agenda.API.Features.Appointments.v1.Create;

/// <summary>
/// Validates <see cref="NewAppointmentInfo"/> instances.
/// </summary>
public class NewAppointmentInfoValidator : Validator<NewAppointmentInfo>
{
    /// <summary>
    /// Builds a new <see cref="NewAppointmentInfoValidator"/> instance
    /// </summary>
    /// <param name="clock">Service to get <see cref="DateTime"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="clock"/> is null.</exception>
    public NewAppointmentInfoValidator(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.Subject).NotNull();

        When(x => x.StartDate != default && x.EndDate != default,
             () =>
             {
                 RuleFor(x => x.EndDate)
                     .Must((x, endDate) => x.StartDate.ToInstant() <= endDate.ToInstant()) ;

                 RuleFor(x => x.EndDate)
                     .Must((_, endDate) => clock.GetCurrentInstant() <= endDate.ToInstant())
                     .When(input => input.StartDate.ToInstant() < input.EndDate.ToInstant());

             });
    }
}