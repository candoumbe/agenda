using System;
using System.Collections.Generic;
using System.Linq;
using Agenda.Ids;
using Candoumbe.DataAccess.Abstractions.Entities;
using NodaTime;
using Optional;

namespace Agenda.Objects;
/// <summary>
/// A meeting with a location and a subject
/// </summary>
public class Appointment : AuditableEntity<AppointmentId, Appointment>
{
    private readonly IList<Attendee> _attendees;

    /// <summary>
    /// Location of the appointment
    /// </summary>
    public string Location { get; }

    /// <summary>
    /// Subject of the <see cref="Appointment"/>
    /// </summary>
    public string Subject { get; private set; }

    /// <summary>
    /// Start date of the appointment
    /// </summary>
    public Instant StartDate { get; private set; }

    /// <summary>
    /// End date of the <see cref="Appointment"/>
    /// </summary>
    public Instant EndDate { get; private set; }

    /// <summary>
    /// Participants of the <see cref="Appointment"/>
    /// </summary>
    public IList<Attendee> Attendees => _attendees;

    public AppointmentStatus Status { get; }

    /// <summary>
    /// Builds a new <see cref="Appointment"/> that spans from <paramref name="startDate"/> to <paramref name="endDate"/>.
    /// </summary>
    /// <param name="id">identifier of the appointment to create</param>
    /// <param name="subject"></param>
    /// <param name="location"></param>
    /// <param name="startDate">defines when the appointment starts</param>
    /// <param name="endDate">defines when the appointment ends</param>
    /// <exception cref="ArgumentException">if <paramref name="startDate"/> is after <paramref name="endDate"/></exception>
    public Appointment(AppointmentId id, string subject, string location, Instant startDate, Instant endDate) : base(id ?? AppointmentId.New())
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before end date", nameof(startDate));
        }

        Subject = subject;
        Location = location ?? string.Empty;
        StartDate = startDate;
        EndDate = endDate;
        _attendees = new List<Attendee>();
    }

    /// <summary>
    /// Adds the specified participant to the current instance
    /// </summary>
    /// <param name="attendee">The participant to add</param>
    /// <exception cref="ArgumentNullException">if <paramref name="attendee"/> is <c>null</c></exception>
    public void AddAttendee(Attendee attendee)
    {
        if (attendee is null)
        {
            throw new ArgumentNullException(nameof(attendee));
        }

        if (_attendees.All(att => att.Id != attendee.Id))
        {
            _attendees.Add(attendee);
        }
    }

    /// <summary>
    /// Removes the <see cref="Attendee"/> with the specified <see cref="Entity{TKey,TEntry}.Id"/>
    /// </summary>
    /// <param name="attendeeId">ID of the attendee to remove</param>
    public void RemoveAttendee(AttendeeId attendeeId)
    {
        Option<Attendee> optionalAttendee = _attendees.SingleOrDefault(x => x.Id == attendeeId)
            .SomeNotNull();

        optionalAttendee.MatchSome((attendee) => _attendees.Remove(attendee));
    }

    /// <summary>
    /// Update the <see cref="Subject"/> of the <see cref="Appointment"/>
    /// </summary>
    /// <param name="newSubject">The new subject</param>
    /// <exception cref="ArgumentNullException">if <paramref name="newSubject"/> is <c>null</c></exception>
    public void ChangeSubjectTo(string newSubject) => Subject = newSubject ?? throw new ArgumentNullException(nameof(newSubject));

    /// <summary>
    /// Changes the <see cref="StartDate"/> and <see cref="EndDate"/> of the <see cref="Appointment"/>.
    /// </summary>
    /// <param name="newStartDate">The new start date</param>
    /// <param name="newEndDate">The new end date</param>
    /// <exception cref="ArgumentException">if <paramref name="newStartDate"/> is after <paramref name="newEndDate"/></exception>
    public void Reschedule(ZonedDateTime newStartDate, ZonedDateTime newEndDate)
    {
        Instant start = newStartDate.ToInstant();
        Instant end = newEndDate.ToInstant();

        if (start > end)
        {
            throw new ArgumentException("Start date must be before end date", nameof(newStartDate));
        }

        StartDate = newStartDate.ToInstant();
        EndDate = newEndDate.ToInstant();
    }

    /// <summary>
    /// Gets the status of the <see cref="Appointment"/> at the specified <paramref name="now"/> instant.
    /// </summary>
    /// <param name="now">Represents the instant at which the status should be computed</param>
    /// <returns>The appointment'<see cref="AppointmentStatus">status</see></returns>
    public AppointmentStatus GetStatus(Instant now)
        => (now.CompareTo(StartDate), now.CompareTo(EndDate)) switch
        {
            (< 0, _) => AppointmentStatus.NotStarted,
            (_, > 0) => AppointmentStatus.Ended,
            _        => AppointmentStatus.OnGoing
        };
}