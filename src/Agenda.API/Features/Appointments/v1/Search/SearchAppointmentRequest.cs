#nullable enable
namespace Agenda.API.Features.Appointments.v1.Search;

/// <summary>
/// Wraps search criteria
/// </summary>
public record SearchAppointmentRequest : AbstractSearchRequest<AppointmentInfo>
{
    /// <summary>
    /// Lower bound of the search criteria
    /// </summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>
    /// Upper bound of the search criterion
    /// </summary>
    public DateTimeOffset? To { get; init; }

    /// <summary>
    /// Criteria on the subject
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Criteria on the location
    /// </summary>
    public string? Location { get; init; }
}