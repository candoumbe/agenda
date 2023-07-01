﻿namespace Agenda.API.Resources.Appointments.v1.Create;

using Agenda.API.Resources.v1.Appointments;
using Agenda.Ids;

using NodaTime;

/// <summary>
/// Contains data to create a new <see cref="AppointmentInfo"/> beetween two or more person
/// </summary>
public record NewAppointmentInfo : IEquatable<NewAppointmentInfo>
{
    /// <summary>
    /// Identifier of the appointment
    /// </summary>
    public AppointmentId Id { get; init; }

    /// <summary>
    /// Location of the appointment
    /// </summary>
    public string Location { get; init; }

    /// <summary>
    /// Subject of the appointment
    /// </summary>
    public string Subject { get; init; }

    /// <summary>
    /// Start date of the appointment
    /// </summary>
    public OffsetDateTime StartDate { get; init; }

    /// <summary>
    /// End date of the appointment
    /// </summary>
    public OffsetDateTime EndDate { get; init; }

    /// <summary>
    /// Participants of the appointment
    /// </summary>
    public IEnumerable<AttendeeInfo> Attendees { get; init; }

    /// <summary>
    /// Builds a new <see cref="NewAppointmentInfo"/> instance.
    /// </summary>
    public NewAppointmentInfo()
    {
        Attendees = Enumerable.Empty<AttendeeInfo>();
    }
}
