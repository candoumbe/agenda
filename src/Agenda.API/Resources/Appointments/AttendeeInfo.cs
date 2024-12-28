namespace Agenda.API.Resources.v1.Appointments;

using Agenda.Ids;

/// <summary>
/// A person who participate to an appointment
/// </summary>
public class AttendeeInfo : Resource<AttendeeId>
{
    /// <summary>
    /// Name of the participant
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Phone number
    /// </summary>
    public string PhoneNumber { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string Email { get; set; }
}