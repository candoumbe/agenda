using FluentValidation;
using NodaTime;

namespace Agenda.API.Resources.Appointments.v1.Create;

/// <summary>
/// Validates <see cref="NewAppointmentInfo"/> instances.
/// </summary>
public class NewAppointmentModelValidator : AbstractValidator<NewAppointmentInfo>
{
    /// <summary>
    /// Builds a new <see cref="NewAppointmentModelValidator"/> instance
    /// </summary>
    /// <param name="clock">Service to get <see cref="DateTime"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="clock"/> is null.</exception>
    public NewAppointmentModelValidator(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.Location).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.Subject).NotNull();

        RuleFor(x => x.Attendees).NotEmpty();

        When(
            x => x.StartDate != default && x.EndDate != default,
            () =>
            {
                RuleFor(x => x.EndDate)
                    .Must((x, endDate) => endDate.ToInstant() >= x.StartDate.ToInstant());
            }
        );
    }
}