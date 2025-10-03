
/* Modification non fusionnée à partir du projet 'Agenda.Objects(net8.0)'
Avant :
namespace Agenda.Objects;
Après :
using Agenda.Ids;

using Candoumbe.DataAccess.Abstractions.Entities;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Agenda.Objects;
*/

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Agenda.Ids;
using Candoumbe.DataAccess.Abstractions.Entities;

namespace Agenda.Objects;
/// <summary>
/// Participant of a <see cref="Appointment"/>
/// </summary>
public partial class Attendee : AuditableEntity<AttendeeId, Attendee>
{
    private string _name;
    
    // Regex to validate an email address
    private static readonly System.Text.RegularExpressions.Regex s_emailRegex = ValidateEmailAddressRegex();

    /// <summary>
    /// Name of the participant
    /// </summary>
    public string Name
    {
        get => _name;
        private set => _name = value?.ToTitleCase() ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Phone number of the participant
    /// </summary>
    public string PhoneNumber { get; private set; }

    /// <summary>
    /// Email of the participant
    /// </summary>
    public string Email { get; private set; }

    private readonly IList<Appointment> _appointments;

    [JsonIgnore]
    public IEnumerable<Appointment> Appointments => _appointments;


    /// <summary>
    /// Builds a new <see cref="Attendee"/> instance
    /// </summary>
    /// <param name="id">identifier of the attendee</param>
    /// <param name="name">Name of the participant</param>
    /// <param name="email"></param>
    /// <param name="phoneNumber">Phone number that can be used to contact the participant</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> is <c>Guid.Empty</c></exception>
    public Attendee(AttendeeId id, string name, string email = null, string phoneNumber = null) : base(id == default ? AttendeeId.New() : id)
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        _appointments = new List<Appointment>();
    }

    /// <summary>
    /// Changes attendee's name
    /// </summary>
    /// <param name="newName"></param>
    /// <exception cref="ArgumentNullException">if <paramref name="newName"/> is <see langword="null"/></exception>
    public void ChangeNameTo(string newName) => Name = newName;


    /// <summary>
    /// Changes <see cref="Attendee"/>'s <see cref="Email"/>
    /// </summary>
    /// <param name="newEmail">new email</param>
    /// <exception cref="ArgumentNullException">if <paramref name="newEmail"/> is <see langword="null"/></exception>
    public void ChangeEmail(string newEmail)
    {
        if (Email is null)
        {
            throw new ArgumentNullException(nameof(newEmail));
        }

        if (!s_emailRegex.IsMatch(newEmail))
        {
            throw new InvalidEmailException($"The email '{newEmail}' is not valid");
        }

        Email = newEmail;
    }

    /// <summary>
    /// Reinitializes the <see cref="Email"/> property to <see langword="null"/>
    /// </summary>
    public void ReinitializeEmail() => Email = null;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+(\.[^@\s])?$", RegexOptions.IgnoreCase | RegexOptions.Compiled, matchTimeoutMilliseconds: 1_000)]
    private static partial Regex ValidateEmailAddressRegex();
}