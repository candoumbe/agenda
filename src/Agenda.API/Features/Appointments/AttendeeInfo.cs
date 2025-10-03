using Agenda.Ids;

namespace Agenda.API.Features.Appointments;

/// <summary>
/// A person who participate to an appointment
/// </summary>
public record AttendeeInfo : Resource<AttendeeId>
{
    /// <summary>
    /// Name of the participant
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Phone number
    /// </summary>
    public string PhoneNumber { get; init; }

    /// <summary>
    /// Email
    /// </summary>
    public string Email { get; init; }
}