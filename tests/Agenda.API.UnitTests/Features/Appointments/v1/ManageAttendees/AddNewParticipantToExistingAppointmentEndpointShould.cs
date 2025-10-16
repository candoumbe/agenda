using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.Appointments.v1.ManageAttendees.AddParticipantToExistingAppointment;
using Agenda.API.Features.Appointments.v1.ManageAttendees.RemoveAttendee;
using Agenda.API.UnitTests.Helpers;
using Agenda.Ids;
using Agenda.Objects;
using Agenda.UnitTests.Helpers;
using Bogus;
using Candoumbe.DataAccess.Abstractions;
using FakeItEasy;
using FastEndpoints;
using FluentAssertions;
using FluentAssertions.Execution;
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
            .BeAssignableTo<Endpoint<AddNewParticipantToExistingAppointmentRequest, Results<NoContent, NotFound>>>();

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


    public static TheoryData<GenericSerializable<IReadOnlyList<Appointment>>, GenericSerializable<AddNewParticipantToExistingAppointmentRequest>, XunitSerializableExpression<Results<NoContent, Conflict, NotFound>>, string> RequestCases
    {
        get
        {
            TheoryData<GenericSerializable<IReadOnlyList<Appointment>>, GenericSerializable<AddNewParticipantToExistingAppointmentRequest>, XunitSerializableExpression<Results<NoContent, Conflict, NotFound>>, string> cases = new()
            {
                // No data in the database
                {
                    Array.Empty<Appointment>(),
                    new AddNewParticipantToExistingAppointmentRequest()
                    {
                        Id = AppointmentId.New(),
                        Participant = new AttendeeInfo
                        {
                            Id = AttendeeId.New(),
                            Name = s_faker.Name.FullName(),
                            Email = s_faker.Internet.Email(),
                            PhoneNumber = s_faker.Phone.PhoneNumber()
                        }
                    },
                    new XunitSerializableExpression<Results<NoContent, Conflict, NotFound>> { Value = result => result.Result is NotFound },
                    "no appointment in the database"
                },

            };

            // Data in the database and request id match an existing appointment, but request attendee id does not match an existing attendee
            {
                Appointment appointment = s_appointmentFaker.Generate();
                appointment.AddAttendee(new Attendee(AttendeeId.New(), s_faker.Name.FullName(), s_faker.Internet.Email(), s_faker.Phone.PhoneNumber()));

                cases.Add(new GenericSerializable<IReadOnlyList<Appointment>> { Value = [appointment] },
                          new AddNewParticipantToExistingAppointmentRequest()
                          {
                              Id = appointment.Id,
                              Participant = new AttendeeInfo
                              {
                                  Id = AttendeeId.New(),
                                  Name = s_faker.Name.FullName(),
                                  Email = s_faker.Internet.Email(),
                                  PhoneNumber = s_faker.Phone.PhoneNumber()
                              }
                          },
                          new XunitSerializableExpression<Results<NoContent, Conflict, NotFound>> { Value = result => result.Result is NoContent },
                          "request id match an existing appointment and  the new participant's id does not match an existing participant in that appointment");
            }

            // Data in the database and request id match an existing appointment but request attendee id does not match an existing attendee
            {
                Appointment appointment = s_appointmentFaker.Generate();
                Attendee attendee = new (AttendeeId.New(), s_faker.Name.FullName(), s_faker.Internet.Email(), s_faker.Phone.PhoneNumber());
                appointment.AddAttendee(attendee);

                cases.Add(new GenericSerializable<IReadOnlyList<Appointment>> { Value = [appointment] },
                    new AddNewParticipantToExistingAppointmentRequest
                    {
                        Id = appointment.Id,
                        Participant = new AttendeeInfo
                        {
                            Id = attendee.Id,
                            Name = s_faker.Name.FullName(),
                            Email = s_faker.Internet.Email(),
                            PhoneNumber = s_faker.Phone.PhoneNumber()
                        }
                    },
                    new XunitSerializableExpression<Results<NoContent, Conflict, NotFound>> { Value = result => result.Result is Conflict },
                    "request id match an existing appointment but new participant's id match an existing attendee in that appointment");
            }


            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(RequestCases))]
    public async Task Return_expected_response_When_datastore_is_in_specified_state(GenericSerializable<IReadOnlyList<Appointment>> appointmentsInStore,
                                                                                    GenericSerializable<AddNewParticipantToExistingAppointmentRequest> request,
                                                                                    XunitSerializableExpression<Results<NoContent, Conflict, NotFound>> responseExpectation,
                                                                                    string reason)
    {
        // Arrange
        IReadOnlyList<Appointment> appointments = appointmentsInStore.Value;

        A.CallTo(() => _fakeRepository.SingleOrDefault(An<IFilterSpecification<Appointment>>._, A<CancellationToken>._))
            .ReturnsLazily((IFilterSpecification<Appointment> predicate, CancellationToken _) => appointments.SingleOrDefault(predicate.Filter.Compile()).SomeNotNull());

        // Act
        Results<NoContent, Conflict, NotFound> response = await _sut.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Should().Match(responseExpectation.Value, reason);

        A.CallTo(() => _fakeRepository.SingleOrDefault(An<IFilterSpecification<Appointment>>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _fakeUnitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .MustHaveHappened(response is { Result: NoContent } ? 1 : 0, Times.Exactly);

    }
}