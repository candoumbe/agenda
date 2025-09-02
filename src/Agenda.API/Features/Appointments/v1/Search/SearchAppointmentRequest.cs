using JetBrains.Annotations;
using NodaTime;

namespace Agenda.API.Features.Appointments.v1.Search;

/// <summary>
/// Wraps search criteria
/// </summary>
public record SearchAppointmentRequest : AbstractSearchRequest<AppointmentInfo>
{
    /// <summary>
    /// Lower bound of the search criteria
    /// </summary>
    public OffsetDateTime? From { get; init; }

    /// <summary>
    /// Upper bound of the search criterion
    /// </summary>
    public OffsetDateTime? To { get; init; }

    /// <summary>
    /// Criteria on the subject
    /// </summary>
    [CanBeNull]
    public string Subject { get; init; }

    /// <summary>
    /// Criteria on the attendees
    /// </summary>
    [CanBeNull]
    public string Attendees { get; init; }
}