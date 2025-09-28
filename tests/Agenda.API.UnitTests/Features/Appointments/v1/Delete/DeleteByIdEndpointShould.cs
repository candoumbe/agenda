using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features.Appointments;
using Agenda.API.Features.Appointments.v1.Delete;
using Agenda.API.UnitTests.Helpers;
using Agenda.Ids;
using Agenda.Objects;
using Bogus;
using Candoumbe.DataAccess.Abstractions;
using FakeItEasy;
using FastEndpoints;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Xunit;

namespace Agenda.API.UnitTests.Features.Appointments.v1.Delete;

public class DeleteByIdEndpointShould
{
    private readonly DeleteEndpoint _sut;
    private readonly IRepository<Appointment> _fakeRepository;
    private static readonly Faker<Appointment> s_appointmentFaker;
    private static readonly Faker s_faker = new();

    static DeleteByIdEndpointShould()
    {
        s_appointmentFaker = new Faker<Appointment>();
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

    public DeleteByIdEndpointShould()
    {
        IUnitOfWorkFactory fakeUnitOfWorkFactory = A.Fake<IUnitOfWorkFactory>(x => x.Strict().Named("unitOfWorkFactory"));
        IUnitOfWork fakeUnitOfWork = A.Fake<IUnitOfWork>(x => x.Strict().Named("unitOfWork"));
        _fakeRepository = A.Fake<IRepository<Appointment>>(x => x.Strict().Named("repository"));

        A.CallTo(() => fakeUnitOfWorkFactory.NewUnitOfWork()).Returns(fakeUnitOfWork);
        A.CallTo(() => fakeUnitOfWork.Repository<Appointment>()).Returns(_fakeRepository);
        A.CallTo(() => fakeUnitOfWork.SaveChangesAsync(A<CancellationToken>._)).DoesNothing();
        A.CallTo(() => fakeUnitOfWork.Dispose()).DoesNothing();

        _sut = Factory.Create<DeleteEndpoint>(fakeUnitOfWorkFactory);
    }

    [Fact]
    public void Have_expected_definition()
    {
        EndpointDefinition endpointDefinition = _sut.Definition;

        // Assert
        string[] routes = endpointDefinition.Routes;
        routes.Should()
            .HaveCount(1)
            .And
            .ContainSingle("/appointments/{id}");

        string[] methods = endpointDefinition.Verbs;
        methods.Should().HaveCount(1)
            .And.ContainSingle("DELETE");

        endpointDefinition.ValidatorType.Should().BeNull();
        endpointDefinition.PostProcessorsList.Should().BeEmpty();
    }

    public static TheoryData<GenericSerializable<IReadOnlyList<Appointment>>, GenericSerializable<DeleteByIdRequest>, XunitSerializableExpression<Results<NoContent, NotFound>>, string> RequestCases
    {
        get
        {
            TheoryData<GenericSerializable<IReadOnlyList<Appointment>>, GenericSerializable<DeleteByIdRequest>, XunitSerializableExpression<Results<NoContent, NotFound>>, string> cases = new()
            {
                // No data in the database
                {
                    Array.Empty<Appointment>(),
                    new DeleteByIdRequest(AppointmentId.New()),
                    new XunitSerializableExpression<Results<NoContent, NotFound>> { Value = result => result.Result is NotFound },
                    "no data in the database"
                }
            };

            // Data in the database and request id match an existing appointment
            {
                List<Appointment> appointments = s_appointmentFaker.Generate(10);
                AppointmentId id = s_faker.PickRandom(appointments).Id;
                DeleteByIdRequest request = new(id);

                cases.Add(appointments,
                          request,
                          new XunitSerializableExpression<Results<NoContent, NotFound>> { Value = result => result.Result is NoContent },
                          "data in the database and request id match an existing appointment");
            }

            // Data in the database and request id does not match an existing appointment
            {
                List<Appointment> appointments = s_appointmentFaker.Generate(10);
                AppointmentId id = AppointmentId.New();
                DeleteByIdRequest request = new(id);

                cases.Add(appointments,
                          request,
                          new XunitSerializableExpression<Results<NoContent, NotFound>> { Value = result => result.Result is NotFound },
                          "data in the database but request id does not match an existing appointment");
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(RequestCases))]
    public async Task Given_datastore_state_When_deleting_Then_return_expected_response(GenericSerializable<IReadOnlyList<Appointment>> appointmentsInStore,
                                                                                        GenericSerializable<DeleteByIdRequest> request,
                                                                                        XunitSerializableExpression<Results<NoContent, NotFound>> responseExpectation,
                                                                                        string reason)
    {
        // Arrange
        IReadOnlyList<Appointment> appointments = appointmentsInStore.Value;

        A.CallTo(() => _fakeRepository.Any(An<IFilterSpecification<Appointment>>._, A<CancellationToken>._))
            .ReturnsLazily((IFilterSpecification<Appointment> predicate, CancellationToken _) => appointments.Any(predicate.Filter.Compile()));

        A.CallTo(() => _fakeRepository.Delete(An<IFilterSpecification<Appointment>>._, A<CancellationToken>._))
            .DoesNothing();

        // Act
        Results<NoContent, NotFound> response = await _sut.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Should().Match(responseExpectation.Value, reason);
    }
}