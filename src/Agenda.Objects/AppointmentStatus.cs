
using System;

namespace Agenda.Objects;

/// <summary>
/// Appointment status.
/// </summary>
[Flags]
public enum AppointmentStatus
{
    /// <summary>
    /// The appointment has not started yet.
    /// </summary>
    NotStarted = 0x0,
    /// <summary>
    /// The appointment is ongoing.
    /// </summary>
    OnGoing = 0x1,

    /// <summary>
    /// The appointment has ended.
    /// </summary>
    Ended = 0x2
}