using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.Appointments.v1.ManageAttendees.AddParticipantToExistingAppointment;
using Agenda.Ids;
using Agenda.Objects;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Bogus;
using Candoumbe.DataAccess.Abstractions;
using FakeItEasy;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using NodaTime;
using Optional;
using Xunit;

namespace Agenda.API.UnitTests.Features.Appointments.v1.ManageAttendees;

public class AddNewParticipantToExistingAppointmentEndpointShould
{
    private static readonly Faker s_faker;
    private static readonly Faker<AttendeeInfo> s_attendeeFaker;
    private static readonly Faker<Appointment> s_appointmentFaker;
    private readonly AddNewParticipantToExistingAppointmentEndpoint _sut;
    private readonly IRepository<Appointment> _fakeRepository;
    private readonly IUnitOfWork _fakeUnitOfWork;

    static AddNewParticipantToExistingAppointmentEndpointShould()
    {
        s_faker = new Faker();
        s_appointmentFaker = new Faker<Appointment>();
        s_attendeeFaker = new Faker<AttendeeInfo>();
        s_attendeeFaker.RuleFor(attendee => attendee.Id, new AttendeeId())
            .RuleFor(attendee => attendee.Name, s_faker.Name.FullName())
            .RuleFor(attendee => attendee.Email, s_faker.Internet.Email())
            .RuleFor(attendee => attendee.PhoneNumber, s_faker.Phone.PhoneNumber())
            ;

        s_appointmentFaker.CustomInstantiator(f =>
        {
            Instant startDate = f.Noda().Instant.Soon();
            return new Appointment(AppointmentId.New(),
                f.Lorem.Sentence(),
                f.Address.FullAddress(),
                startDate,
                f.Noda().Instant.Future(reference: startDate)
            );
        });
    }

    public AddNewParticipantToExistingAppointmentEndpointShould()
    {
        IUnitOfWorkFactory fakeUnitOfWorkFactory = A.Fake<IUnitOfWorkFactory>(x => x.Strict().Named("unitOfWorkFactory"));
        _fakeUnitOfWork = A.Fake<IUnitOfWork>(x => x.Strict().Named("unitOfWork"));
        _fakeRepository = A.Fake<IRepository<Appointment>>(x => x.Strict().Named("repository"));

        A.CallTo(() => fakeUnitOfWorkFactory.NewUnitOfWork()).Returns(_fakeUnitOfWork);
        A.CallTo(() => _fakeUnitOfWork.Repository<Appointment>()).Returns(_fakeRepository);
        A.CallTo(() => _fakeUnitOfWork.SaveChangesAsync(A<CancellationToken>._)).DoesNothing();
        A.CallTo(() => _fakeUnitOfWork.Dispose()).DoesNothing();

        _sut = Factory.Create<AddNewParticipantToExistingAppointmentEndpoint>(fakeUnitOfWorkFactory);
    }

    [Fact]
    public void Have_expected_definition()
    {
        // Assert
        using AssertionScope _ = new ();

        typeof(AddNewParticipantToExistingAppointmentEndpoint).Should()
            .BeAssignableTo<Endpoint<AddNewParticipantToExistingAppointmentRequest, Results<NoContent, Conflict, NotFound>>>();

        EndpointDefinition definition = _sut.Definition;

        definition.Routes.Should()
            .HaveCount(1)
            .And
            .ContainSingle("/appointments/{id}/attendees");

        definition.Verbs.Should()
            .HaveCount(1)
            .And
            .ContainSingle("POST");
    }

    [Fact]
    public async ValueTask Return_NotFound_When_no_appointment_in_the_database()
    {
        // Arrange
        IReadOnlyList<Appointment> appointments = Array.Empty<Appointment>();
        AddNewParticipantToExistingAppointmentRequest request = new()
        {
            Id = AppointmentId.New(),
            Participant = new AttendeeInfo
            {
                Id = AttendeeId.New(),
                Name = s_faker.Name.FullName(),
                Email = s_faker.Internet.Email(),
                PhoneNumber = s_faker.Phone.PhoneNumber()
            }
        };

        A.CallTo(() => _fakeRepository.SingleOrDefault(An<IFilterSpecification<Appointment>>._, A<CancellationToken>._))
            .ReturnsLazily((IFilterSpecification<Appointment> predicate, CancellationToken _) => appointments.SingleOrDefault(predicate.Filter.Compile()).SomeNotNull());

        // Act
        Results<NoContent, Conflict, NotFound> response = await _sut.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Result.Should().BeOfType<NotFound>();

        A.CallTo(() => _fakeRepository.SingleOrDefault(An<IFilterSpecification<Appointment>>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _fakeUnitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async ValueTask Return_NoContent_When_request_id_match_an_existing_appointment_and_the_new_participant_id_does_not_match_an_existing_participant()
    {
        // Arrange
        Appointment appointment = s_appointmentFaker.Generate();
        appointment.AddAttendee(new Attendee(AttendeeId.New(), s_faker.Name.FullName(), s_faker.Internet.Email(), s_faker.Phone.PhoneNumber()));

        IReadOnlyList<Appointment> appointments = [appointment];
        AddNewParticipantToExistingAppointmentRequest request = new()
        {
            Id = appointment.Id,
            Participant = new AttendeeInfo
            {
                Id = AttendeeId.New(),
                Name = s_faker.Name.FullName(),
                Email = s_faker.Internet.Email(),
                PhoneNumber = s_faker.Phone.PhoneNumber()
            }
        };

        A.CallTo(() => _fakeRepository.SingleOrDefault(An<IFilterSpecification<Appointment>>._, A<CancellationToken>._))
            .ReturnsLazily((IFilterSpecification<Appointment> predicate, CancellationToken _) => appointments.SingleOrDefault(predicate.Filter.Compile()).SomeNotNull());

        // Act
        Results<NoContent, Conflict, NotFound> response = await _sut.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Result.Should().BeOfType<NoContent>();

        A.CallTo(() => _fakeRepository.SingleOrDefault(An<IFilterSpecification<Appointment>>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _fakeUnitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        appointment.Attendees.Should()
                   .HaveCount(2);
    }

    [Fact]
    public async ValueTask Return_NoContent_When_request_id_match_an_existing_appointment_and_there_was_no_participant()
    {
        // Arrange
        Appointment appointment = s_appointmentFaker.Generate();

        IReadOnlyList<Appointment> appointments = [appointment];
        AddNewParticipantToExistingAppointmentRequest request = new()
        {
            Id = appointment.Id,
            Participant = new AttendeeInfo
            {
                Id = AttendeeId.New(),
                Name = s_faker.Name.FullName(),
                Email = s_faker.Internet.Email(),
                PhoneNumber = s_faker.Phone.PhoneNumber()
            }
        };

        A.CallTo(() => _fakeRepository.SingleOrDefault(An<IFilterSpecification<Appointment>>._, A<CancellationToken>._))
            .ReturnsLazily((IFilterSpecification<Appointment> predicate, CancellationToken _) => appointments.SingleOrDefault(predicate.Filter.Compile()).SomeNotNull());

        // Act
        Results<NoContent, Conflict, NotFound> response = await _sut.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Result.Should().BeOfType<NoContent>();

        A.CallTo(() => _fakeRepository.SingleOrDefault(An<IFilterSpecification<Appointment>>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _fakeUnitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        appointment.Attendees.Should()
            .HaveCount(1)
            .And.ContainSingle(attendee => attendee.Id == request.Participant.Id);
    }

    [Fact]
    public async ValueTask Return_Conflict_When_request_id_match_an_existing_appointment_but_new_participant_id_match_an_existing_attendee()
    {
        // Arrange
        Appointment appointment = s_appointmentFaker.Generate();
        Attendee attendee = new(AttendeeId.New(), s_faker.Name.FullName(), s_faker.Internet.Email(), s_faker.Phone.PhoneNumber());
        appointment.AddAttendee(attendee);

        IReadOnlyList<Appointment> appointments = [appointment];
        AddNewParticipantToExistingAppointmentRequest request = new()
        {
            Id = appointment.Id,
            Participant = new AttendeeInfo
            {
                Id = attendee.Id,
                Name = s_faker.Name.FullName(),
                Email = s_faker.Internet.Email(),
                PhoneNumber = s_faker.Phone.PhoneNumber()
            }
        };

        A.CallTo(() => _fakeRepository.SingleOrDefault(An<IFilterSpecification<Appointment>>._, A<CancellationToken>._))
            .ReturnsLazily((IFilterSpecification<Appointment> predicate, CancellationToken _) => appointments.SingleOrDefault(predicate.Filter.Compile()).SomeNotNull());

        // Act
        Results<NoContent, Conflict, NotFound> response = await _sut.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Result.Should().BeOfType<Conflict>();

        A.CallTo(() => _fakeRepository.SingleOrDefault(An<IFilterSpecification<Appointment>>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _fakeUnitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}