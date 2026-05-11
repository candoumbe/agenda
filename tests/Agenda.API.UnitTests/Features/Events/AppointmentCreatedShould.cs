using System;
using System.Collections.Generic;
using Agenda.Events;
using Agenda.Ids;
using AwesomeAssertions;
using Bogus;
using NodaTime;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.UnitTests.Features.Events
{
    [UnitTest]
    public class AppointmentCreatedShould
    {
        private static readonly Faker s_faker;

        static AppointmentCreatedShould()
        {
            s_faker = new Faker();
        }

        [Fact]
        public void Build_with_required_data()
        {
            // Arrange
            AppointmentId appointmentId = AppointmentId.New();
            Instant startDate = SystemClock.Instance.GetCurrentInstant();
            Instant endDate = startDate.Plus(Duration.FromHours(1));
            string location = s_faker.Address.FullAddress();
            IReadOnlyList<Attendee> attendees = new[]
            {
                new Attendee
                {
                    Id = Guid.NewGuid(),
                    FirstName = s_faker.Name.FirstName(),
                    LastName = s_faker.Name.LastName()
                }
            };
            string creatorId = "user-123";

            // Act
            AppointmentCreated appointmentCreated = new(appointmentId, startDate, endDate, location, attendees, creatorId);

            // Assert
            appointmentCreated.AppointmentId.Should().Be(appointmentId);
            appointmentCreated.StartDate.Should().Be(startDate);
            appointmentCreated.EndDate.Should().Be(endDate);
            appointmentCreated.Location.Should().Be(location);
            appointmentCreated.Attendees.Should().HaveSameCount(attendees);
            appointmentCreated.CreatorId.Should().Be(creatorId);
        }

        [Fact]
        public void Throw_when_location_is_null()
        {
            // Arrange
            AppointmentId appointmentId = AppointmentId.New();
            Instant startDate = SystemClock.Instance.GetCurrentInstant();
            Instant endDate = startDate.Plus(Duration.FromHours(1));
            IReadOnlyList<Attendee> attendees = [];
            string creatorId = "user-123";

            // Act & Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                new AppointmentCreated(appointmentId, startDate, endDate, null!, attendees, creatorId));

            exception.ParamName.Should().Be("location");
        }

        [Fact]
        public void Throw_when_attendees_is_null()
        {
            // Arrange
            AppointmentId appointmentId = AppointmentId.New();
            Instant startDate = SystemClock.Instance.GetCurrentInstant();
            Instant endDate = startDate.Plus(Duration.FromHours(1));
            string location = s_faker.Address.FullAddress();
            string creatorId = "user-123";

            // Act & Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                new AppointmentCreated(appointmentId, startDate, endDate, location, null!, creatorId));

            exception.ParamName.Should().Be("attendees");
        }

        [Fact]
        public void Throw_when_creatorId_is_null()
        {
            // Arrange
            AppointmentId appointmentId = AppointmentId.New();
            Instant startDate = SystemClock.Instance.GetCurrentInstant();
            Instant endDate = startDate.Plus(Duration.FromHours(1));
            string location = s_faker.Address.FullAddress();
            IReadOnlyList<Attendee> attendees = [];

            // Act & Assert
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                new AppointmentCreated(appointmentId, startDate, endDate, location, attendees, null!));

            exception.ParamName.Should().Be("creatorId");
        }

        [Fact]
        public void Allow_empty_location()
        {
            // Arrange
            AppointmentId appointmentId = AppointmentId.New();
            Instant startDate = SystemClock.Instance.GetCurrentInstant();
            Instant endDate = startDate.Plus(Duration.FromHours(1));
            IReadOnlyList<Attendee> attendees = [];
            string creatorId = "system";

            // Act
            AppointmentCreated appointmentCreated = new(appointmentId, startDate, endDate, string.Empty, attendees, creatorId);

            // Assert
            appointmentCreated.Location.Should().Be(string.Empty);
        }

        [Fact]
        public void Allow_empty_attendees_list()
        {
            // Arrange
            AppointmentId appointmentId = AppointmentId.New();
            Instant startDate = SystemClock.Instance.GetCurrentInstant();
            Instant endDate = startDate.Plus(Duration.FromHours(1));
            string location = s_faker.Address.FullAddress();
            IReadOnlyList<Attendee> attendees = [];
            string creatorId = "system";

            // Act
            AppointmentCreated appointmentCreated = new(appointmentId, startDate, endDate, location, attendees, creatorId);

            // Assert
            appointmentCreated.Attendees.Should().BeEmpty();
        }

        [Fact]
        public void Preserve_start_and_end_dates_in_UTC()
        {
            // Arrange
            AppointmentId appointmentId = AppointmentId.New();
            Instant startDate = Instant.FromUtc(2026, 5, 15, 10, 30, 0);
            Instant endDate = Instant.FromUtc(2026, 5, 15, 12, 0, 0);
            string location = s_faker.Address.FullAddress();
            IReadOnlyList<Attendee> attendees = [];
            string creatorId = "system";

            // Act
            AppointmentCreated appointmentCreated = new(appointmentId, startDate, endDate, location, attendees, creatorId);

            // Assert
            appointmentCreated.StartDate.Should().Be(startDate);
            appointmentCreated.EndDate.Should().Be(endDate);
            appointmentCreated.StartDate.ToDateTimeUtc().Should().Be(new DateTime(2026, 5, 15, 10, 30, 0));
            appointmentCreated.EndDate.ToDateTimeUtc().Should().Be(new DateTime(2026, 5, 15, 12, 0, 0));
        }
    }
}
