namespace Agenda.API.Resources.Appointments.v1.Search;

using Agenda.API.Resources.Appointments;
using Agenda.API.Resources.v1.Appointments;

using NodaTime;

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
    public string Subject { get; init; }

    /// <summary>
    /// Criteria on the attendees
    /// </summary>
    public string Attendees { get; init; }
}