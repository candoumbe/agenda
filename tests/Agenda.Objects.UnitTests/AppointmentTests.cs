using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Agenda.Ids;
using Bogus;
using FluentAssertions;
using FluentAssertions.Extensions;
using NodaTime;
using NodaTime.Extensions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

namespace Agenda.Objects.UnitTests;

[UnitTest]
public class AppointmentTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void ChangingAppointment_Subject_ToNull_Throws_ArgumentNullException()
    {
        // Arrange
        Appointment attendee = new(id: AppointmentId.New(),
                                   subject: "JLA",
                                   location: "Wayne Manor",
                                   startDate: 12.July(2018).At(12.Hours()).AsUtc().ToInstant(),
                                   endDate: 12.July(2018).At(12.Hours().And(30.Minutes())).AsUtc().ToInstant());

        // Act
        Action action = () => attendee.ChangeSubjectTo(null);

        // Assert
        action.Should()
            .Throw<ArgumentNullException>($"{nameof(Appointment)}'s {nameof(Appointment.Subject)} cannot be changed to null");
    }

    public static TheoryData<Appointment, Instant, AppointmentStatus> ComputeStatusCases
        => new()
        {
            {
                new Appointment(id: AppointmentId.New(),
                                subject: "Daily meeting",
                                location: "My office",
                                startDate: 12.April(2017).At(14.Hours()).AsUtc().ToInstant(),
                                endDate: 12.April(2017).At(17.Hours()).AsUtc().ToInstant()),
                12.April(2017).At(12.Hours()).AsUtc().ToInstant(), AppointmentStatus.NotStarted
            },
            {
                new Appointment(id: AppointmentId.New(),
                                subject: "Daily meeting",
                                location: "My office",
                                startDate: 12.April(2017).At(14.Hours()).AsUtc().ToInstant(),
                                endDate: 12.April(2017).At(17.Hours()).AsUtc().ToInstant()),
                12.April(2017).At(14.Hours()).AsUtc().ToInstant(), AppointmentStatus.OnGoing
            },
            {
                new Appointment(id: AppointmentId.New(),
                                subject: "Daily meeting",
                                location: "My office",
                                startDate: 12.April(2017).At(14.Hours()).AsUtc().ToInstant(),
                                endDate: 12.April(2017).At(17.Hours()).AsUtc().ToInstant()),
                12.April(2017).At(18.Hours()).AsUtc().ToInstant(), AppointmentStatus.Ended
            }
        };

    [Theory]
    [MemberData(nameof(ComputeStatusCases))]
    public void ComputeStatus(Appointment appointment, Instant now, AppointmentStatus expected)
    {
        outputHelper.WriteLine($"Appointment starts at {appointment.StartDate}");
        outputHelper.WriteLine($"Appointment ends at {appointment.EndDate}");

        outputHelper.WriteLine($"Date : {now}");

        // Act
        AppointmentStatus actual = appointment.GetStatus(now);

        // Assert
        actual.Should()
            .Be(expected);
    }

    public static TheoryData<Appointment, Attendee, Expression<Func<Appointment, bool>>> AddingAttendeesToAppointmentCases
    {
        get
        {
            TheoryData<Appointment, Attendee, Expression<Func<Appointment, bool>>> cases = new();

            // Add an attendee with explicit id
            {
                Attendee newAttendee = new Attendee(AttendeeId.New(), "John", "", "0123456789");
                cases.Add(new Appointment(AppointmentId.New(),
                                          "Daily meeting",
                                          "My office",
                                          12.April(2017).At(14.Hours()).AsUtc().ToInstant(),
                                          12.April(2017).At(17.Hours()).AsUtc().ToInstant()),
                          newAttendee,
                          app => app.Attendees.Count == 1
                                 && app.Attendees[0].Id == newAttendee.Id
                                 && app.Attendees[0].Name == "John"
                                 && app.Attendees[0].Email == ""
                                 && app.Attendees[0].PhoneNumber == "0123456789");
            }

            // Adding an attendee to an existing appointment with an explicit id that's already used by another attendee
            {
                AttendeeId attendeeId = AttendeeId.New();
                Attendee existingAttendee = new Attendee(attendeeId, "John", "", "0123456789");
                Appointment appointment = new Appointment(AppointmentId.New(),
                                                          "Daily meeting",
                                                          "My office",
                                                          12.April(2017).At(14.Hours()).AsUtc().ToInstant(),
                                                          12.April(2017).At(17.Hours()).AsUtc().ToInstant());

                appointment.AddAttendee(existingAttendee);

                cases.Add(appointment,
                          existingAttendee,
                          app => app.Attendees.Count == 1
                                 && app.Attendees[0].Id == existingAttendee.Id);
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(AddingAttendeesToAppointmentCases))]
    public void Given_an_appointment_with_no_attendees_When_adding_an_attendee_Then_the_appointment_has_expected_attendees(Appointment appointment,
                                                                                                                           Attendee attendeeToAdd,
                                                                                                                           Expression<Func<Appointment, bool>> appointmentExpectation)
    {
        // Act
        appointment.AddAttendee(attendeeToAdd);

        // Assert
        appointment.Should()
            .Match(appointmentExpectation);
    }

    [Fact]
    public void Given_an_appointment_When_adding_an_attendee_that_is_null_Then_an_ArgumentNullException_is_thrown()
    {
        // Arrange
        Appointment appointment = new(AppointmentId.New(),
                                      "Daily meeting",
                                      "My office",
                                      12.April(2017).At(14.Hours()).AsUtc().ToInstant(),
                                      12.April(2017).At(17.Hours()).AsUtc().ToInstant());

        // Act
        Action addingAnAttendeeThatIsNull = () => appointment.AddAttendee(null);

        // Assert
        addingAnAttendeeThatIsNull.Should()
            .Throw<ArgumentNullException>();
    }
}