using System;
using Agenda.API.UnitTests.Helpers;
using Agenda.Ids;
using Agenda.UnitTests.Helpers;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using Bogus;
using FsCheck.Xunit;
using NodaTime;
using NodaTime.Extensions;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.Objects.UnitTests;

[UnitTest]
public class AppointmentTests(ITestOutputHelper outputHelper)
{
    private static readonly Faker s_faker = new();

    [Property]
    public void Given_start_date_is_after_end_date_Then_an_ArgumentException_is_thrown(Guid id, string subject, string location)
    {
        // Arrange
        DateTimeOffset startDate = 12.July(2018).At(12.Hours().And(30.Minutes())).AsUtc();
        DateTimeOffset endDate = 12.July(2018).At(12.Hours()).AsUtc();
        AppointmentId appointmentId = AppointmentId.From(id);

        // Act
        Action buildingAnInstanceWithStartDateAfterEndDate = () => _ = new Appointment(appointmentId, subject, location, startDate.ToInstant(), endDate.ToInstant());

        // Assert
        buildingAnInstanceWithStartDateAfterEndDate.Should()
            .Throw<ArgumentException>("Start date cannot be after end date");
    }

    [Property]
    public void Given_an_appointment_When_rescheduling_with_a_start_date_after_the_end_date_Then_an_ArgumentException_is_thrown(Guid id, string subject, string location)
    {
        // Arrange
        AppointmentId appointmentId = AppointmentId.From(id);
        Instant startDate = 12.July(2018).At(12.Hours()).AsUtc().ToInstant();
        Instant endDate = 12.July(2018).At(12.Hours().And(30.Minutes())).AsUtc().ToInstant();
        Appointment appointment = new(appointmentId, subject, location, startDate, endDate);

        ZonedDateTime newStartDate = endDate.InUtc();
        ZonedDateTime newEndDate = startDate.InUtc();

        // Act
        Action reschedulingWithNullStartDate = () => appointment.Reschedule(newStartDate, newEndDate);

        // Assert
        reschedulingWithNullStartDate.Should()
            .Throw<ArgumentException>("Start date cannot be after end date");
    }

    [Fact]
    public void Given_an_appointment_When_changing_its_subject_to_null_Then_an_ArgumentNullException_is_thrown()
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

    public static TheoryData<GenericSerializable<Appointment>, GenericSerializable<Instant>, AppointmentStatus> ComputeStatusCases
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
    public void ComputeStatus(GenericSerializable<Appointment> sut, GenericSerializable<Instant> nowGenerator, AppointmentStatus expected)
    {
        // Arrange
        Appointment appointment = sut;
        Instant now = nowGenerator;

        outputHelper.WriteLine($"Appointment starts at {appointment.StartDate}");
        outputHelper.WriteLine($"Appointment ends at {appointment.EndDate}");

        outputHelper.WriteLine($"Date : {now}");

        // Act
        AppointmentStatus actual = appointment.GetStatus(now);

        // Assert
        actual.Should()
            .Be(expected);
    }

    public static TheoryData<GenericSerializable<Appointment>, GenericSerializable<Attendee>, XunitSerializableExpression<Appointment>> AddingAttendeesToAppointmentCases
    {
        get
        {
            TheoryData<GenericSerializable<Appointment>, GenericSerializable<Attendee>, XunitSerializableExpression<Appointment>> cases = new();

            // Add an attendee with explicit id
            {
                Attendee newAttendee = new Attendee(AttendeeId.New(), "John", "", "0123456789");
                cases.Add(new Appointment(AppointmentId.New(),
                                          "Daily meeting",
                                          "My office",
                                          12.April(2017).At(14.Hours()).AsUtc().ToInstant(),
                                          12.April(2017).At(17.Hours()).AsUtc().ToInstant()),
                          newAttendee,
                          new XunitSerializableExpression<Appointment> { Value = app => app.Attendees.Count == 1
                                                                               && app.Attendees[0].Id == newAttendee.Id
                                                                               && app.Attendees[0].Name == "John"
                                                                               && app.Attendees[0].Email == ""
                                                                               && app.Attendees[0].PhoneNumber == "0123456789" });
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
                          new XunitSerializableExpression<Appointment>(){ Value = app => app.Attendees.Count == 1
                                                                               && app.Attendees[0].Id == existingAttendee.Id });
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(AddingAttendeesToAppointmentCases))]
    public void Given_an_appointment_with_no_attendees_When_adding_an_attendee_Then_the_appointment_has_expected_attendees(GenericSerializable<Appointment> appointmentGenerator,
                                                                                                                           GenericSerializable<Attendee> attendeeToAddGenerator,
                                                                                                                           XunitSerializableExpression<Appointment> appointmentExpectation)
    {
        // Arrange
        Appointment appointment = appointmentGenerator;
        Attendee attendeeToAdd = attendeeToAddGenerator;

        // Act
        appointment.AddAttendee(attendeeToAdd);

        // Assert
        appointment.Should()
            .Match(appointmentExpectation.Value);
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

    public static TheoryData<GenericSerializable<Appointment>, AttendeeId, XunitSerializableExpression<Appointment>> RemoveAttendeeFromAppointmentCases
    {
        get
        {
            TheoryData<GenericSerializable<Appointment>, AttendeeId, XunitSerializableExpression<Appointment>> cases = new();

            {
                Appointment appointment = new Appointment(AppointmentId.New(),
                                                          s_faker.Lorem.Sentence(),
                                                          s_faker.Address.FullAddress(),
                                                          s_faker.Noda().Instant.Past(),
                                                          s_faker.Noda().Instant.Future());

                AttendeeId attendeeId = AttendeeId.New();
                Attendee attendee = new Attendee(attendeeId, s_faker.Name.FullName(), s_faker.Internet.Email(), s_faker.Phone.PhoneNumber());

                appointment.AddAttendee(attendee);

                cases.Add(appointment,
                          attendeeId,
                          new XunitSerializableExpression<Appointment>() { Value = app => app.Attendees.Count == 0});
            }

            {
                Appointment appointment = new Appointment(AppointmentId.New(),
                                                          s_faker.Lorem.Sentence(),
                                                          s_faker.Address.FullAddress(),
                                                          s_faker.Noda().Instant.Past(),
                                                          s_faker.Noda().Instant.Future());

                AttendeeId attendeeId = AttendeeId.New();
                Attendee attendee = new Attendee(attendeeId, s_faker.Name.FullName(), s_faker.Internet.Email(), s_faker.Phone.PhoneNumber());
                appointment.AddAttendee(attendee);

                cases.Add(appointment,
                          AttendeeId.New(),
                          new XunitSerializableExpression<Appointment>(){ Value = app => app.Attendees.Count == 1
                                                                               && app.Attendees[0].Id == attendeeId});
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(RemoveAttendeeFromAppointmentCases))]
    public void Given_an_appointment_with_attendees_When_removing_an_attendee_Then_the_appointment_has_expected_attendees(GenericSerializable<Appointment> appointmentGenerator,
                                                                                                                          AttendeeId attendeeId,
                                                                                                                          XunitSerializableExpression<Appointment> appointmentExpectation)
    {
        // Arrange
        Appointment appointment = appointmentGenerator;

        // Act
        appointment.RemoveAttendee(attendeeId);

        // Assert
        appointment.Should().Match(appointmentExpectation.Value);
    }


    [Property]
    public void Given_an_appointment_When_relocating_to_a_new_location_Then_the_appointment_location_is_updated(string newLocation)
    {
        // Arrange
        Appointment appointment = new(AppointmentId.New(),
                                      "Daily meeting",
                                      "My office",
                                      12.April(2017).At(14.Hours()).AsUtc().ToInstant(),
                                      12.April(2017).At(17.Hours()).AsUtc().ToInstant());

        // Act
        appointment.RelocateTo(newLocation);

        // Assert
        appointment.Location.Should().Be(newLocation);

    }
}