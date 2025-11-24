using System;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features.Appointments.v1.Update;
using Agenda.API.UnitTests.Fixtures;
using Agenda.API.UnitTests.Helpers;
using Agenda.DataStores;
using Agenda.Ids;
using Agenda.Objects;
using Agenda.UnitTests.Helpers;
using AwesomeAssertions;
using Bogus;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.EFStore;
using FakeItEasy;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using SystemTextJsonPatch.Operations;
using Xunit;

namespace Agenda.API.UnitTests.Features.Appointments.v1.Patch;

public class PatchAppointmentByIdEndpointShould : IClassFixture<PostgresSqlFixture>, IAsyncLifetime
{
    private readonly PatchAppointmentByIdEndpoint _sut;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly ITestOutputHelper _outputHelper;
    private readonly IClock _clock;
    private readonly CurrentRequestMetadataInfoProvider _currentDateTimeProvider;
    private static readonly Faker s_faker = new();

    public PatchAppointmentByIdEndpointShould(ITestOutputHelper outputHelper, PostgresSqlFixture fixture)
    {
        _outputHelper = outputHelper;
        _clock = A.Fake<IClock>();

        DbContextOptionsBuilder<AgendaDataStore> optionsBuilder = new();
        optionsBuilder.UseNpgsql(fixture.ConnectionString, options => options.UseNodaTime()
            .EnableRetryOnFailure(3));

        _unitOfWorkFactory = new EntityFrameworkUnitOfWorkFactory<AgendaDataStore>(optionsBuilder.Options,
            options =>
            {
                AgendaDataStore store = new AgendaDataStore(options, _clock);
                store.Database.EnsureCreated();
                return store;
            },
            new AgendaRepositoryFactory());

        _currentDateTimeProvider = A.Fake<CurrentRequestMetadataInfoProvider>();

        _sut = Factory.Create<PatchAppointmentByIdEndpoint>(_unitOfWorkFactory, _currentDateTimeProvider);
    }


    [Fact]
    public void Have_expected_definition()
    {
        // Assert
        string[] routes = _sut.Definition.Routes;
        routes.Should()
            .HaveCount(1)
            .And
            .ContainSingle("/appointments/{id}");

        string[] methods = _sut.Definition.Verbs;
        methods.Should().HaveCount(1).And.ContainSingle("PATCH");

        _sut.Definition.ValidatorType.Should().Be<PatchAppointmentInfoRequestValidator>();
    }

    public static TheoryData<GenericSerializable<Appointment>, GenericSerializable<PatchRequest<AppointmentId, PatchAppointmentRequest>>, XunitSerializableExpression<Appointment>> RequestShouldUpdateAppointmentCases
    {
        get
        {
            TheoryData<GenericSerializable<Appointment>, GenericSerializable<PatchRequest<AppointmentId, PatchAppointmentRequest>>, XunitSerializableExpression<Appointment>> cases = new();
            {
                Appointment appointment = new(AppointmentId.New(),
                    s_faker.Lorem.Sentence(),
                    s_faker.Address.FullAddress(),
                    Instant.FromUtc(2024, 1, 1, 12, 0),
                    Instant.FromUtc(2024, 1, 1, 13, 0));

                PatchRequest<AppointmentId, PatchAppointmentRequest> request = new()
                {
                    Id = appointment.Id,
                    Operations =
                    [
                        new Operation<PatchAppointmentRequest>(nameof(OperationType.Replace), $"/{nameof(Appointment.Subject)}", "New subject"),
                        new Operation<PatchAppointmentRequest>(nameof(OperationType.Test), $"/{nameof(Appointment.Id)}", appointment.Id.ToString())
                    ]
                };

                cases.Add(appointment, request, new XunitSerializableExpression<Appointment> { Value = updatedAppointment => updatedAppointment.Subject == "New subject" });
            }
            {
                Appointment appointment = new(
                    AppointmentId.New(),
                    s_faker.Lorem.Sentence(),
                    s_faker.Address.FullAddress(),
                    Instant.FromUtc(2024, 1, 1, 12, 0),
                    Instant.FromUtc(2024, 1, 1, 13, 0));

                string newLocation = s_faker.Address.FullAddress();

                PatchRequest<AppointmentId, PatchAppointmentRequest> request = new()
                {
                    Id = appointment.Id,
                    Operations =
                    [
                        new Operation<PatchAppointmentRequest>(nameof(OperationType.Replace), $"/{nameof(Appointment.Location)}", newLocation),
                        new Operation<PatchAppointmentRequest>(nameof(OperationType.Test), $"/{nameof(Appointment.Id)}", appointment.Id.ToString())
                    ]
                };


                cases.Add(appointment, request, new XunitSerializableExpression<Appointment> { Value = updatedAppointment => updatedAppointment.Location == newLocation });
            }
            {
                Appointment appointment = new(
                    AppointmentId.New(),
                    s_faker.Lorem.Sentence(),
                    s_faker.Address.FullAddress(),
                    Instant.FromUtc(2024, 1, 1, 12, 0),
                    Instant.FromUtc(2024, 1, 1, 13, 0));

                Instant newStartDate = appointment.StartDate.Minus(Duration.FromMinutes(5.0));
                Instant newEndDate = appointment.EndDate.Plus(Duration.FromMinutes(5.0));
                PatchRequest<AppointmentId, PatchAppointmentRequest> request = new()
                {
                    Id = appointment.Id,
                    Operations =
                    [
                        new Operation<PatchAppointmentRequest>(nameof(OperationType.Replace), $"/{nameof(Appointment.StartDate)}", newStartDate.ToString()),
                        new Operation<PatchAppointmentRequest>(nameof(OperationType.Replace), $"/{nameof(Appointment.EndDate)}", newEndDate.ToString()),
                        new Operation<PatchAppointmentRequest>(nameof(OperationType.Test), $"/{nameof(Appointment.Id)}", appointment.Id.ToString())
                    ]
                };


                cases.Add(appointment,
                    request,
                    new XunitSerializableExpression<Appointment>
                    {
                        Value = updatedAppointment => updatedAppointment.StartDate == newStartDate &&
                                                      updatedAppointment.EndDate == newEndDate
                    });
            }
            {
                Appointment appointment = new(
                    AppointmentId.New(),
                    s_faker.Lorem.Sentence(),
                    s_faker.Address.FullAddress(),
                    Instant.FromUtc(2024, 1, 1, 12, 0),
                    Instant.FromUtc(2024, 1, 1, 13, 0));

                Instant newStartDate = appointment.StartDate.Minus(Duration.FromMinutes(5.0));
                PatchRequest<AppointmentId, PatchAppointmentRequest> request = new()
                {
                    Id = appointment.Id,
                    Operations =
                    [
                        new Operation<PatchAppointmentRequest>(nameof(OperationType.Replace), $"/{nameof(Appointment.StartDate)}", newStartDate.ToString()),
                        new Operation<PatchAppointmentRequest>(nameof(OperationType.Test), $"/{nameof(Appointment.Id)}", appointment.Id.ToString())
                    ]
                };


                cases.Add(appointment,
                    request,
                    new XunitSerializableExpression<Appointment>
                    {
                        Value = updatedAppointment => updatedAppointment.StartDate == newStartDate &&
                                                      updatedAppointment.EndDate == appointment.EndDate
                    });
            }
            {
                Appointment appointment = new(
                    AppointmentId.New(),
                    s_faker.Lorem.Sentence(),
                    s_faker.Address.FullAddress(),
                    Instant.FromUtc(2024, 1, 1, 12, 0),
                    Instant.FromUtc(2024, 1, 1, 13, 0));

                Instant newEndDate = appointment.EndDate.Plus(Duration.FromMinutes(5.0));
                PatchRequest<AppointmentId, PatchAppointmentRequest> request = new()
                {
                    Id = appointment.Id,
                    Operations =
                    [
                        new Operation<PatchAppointmentRequest>(nameof(OperationType.Replace), $"/{nameof(Appointment.EndDate)}", newEndDate.ToString()),
                        new Operation<PatchAppointmentRequest>(nameof(OperationType.Test), $"/{nameof(Appointment.Id)}", appointment.Id.ToString())
                    ]
                };

                cases.Add(appointment,
                    request,
                    new XunitSerializableExpression<Appointment>
                    {
                        Value = updatedAppointment => updatedAppointment.StartDate == appointment.StartDate &&
                                                      updatedAppointment.EndDate == newEndDate
                    });
            }
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(RequestShouldUpdateAppointmentCases))]
    public async Task Return_NoContent_when_existing_appointment_was_successfully_patched(GenericSerializable<Appointment> appointmentInput,
                                                                                          GenericSerializable<PatchRequest<AppointmentId, PatchAppointmentRequest>> requestInput,
                                                                                          XunitSerializableExpression<Appointment> updateExpectation)
    {
        Appointment appointment = appointmentInput;
        PatchRequest<AppointmentId, PatchAppointmentRequest> request = requestInput;

        CancellationToken ct = TestContext.Current.CancellationToken;

        using IUnitOfWork unitOfWork = _unitOfWorkFactory.NewUnitOfWork();
        await unitOfWork.Repository<Appointment>().Create(appointment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Act
        Results<NoContent, NotFound, ProblemDetails> response = await _sut.ExecuteAsync(request, CancellationToken.None);

        // Assert
        response.Result.Should().BeOfType<NoContent>();

        using IUnitOfWork verifyUnitOfWork = _unitOfWorkFactory.NewUnitOfWork();
        Appointment updatedAppointment = await verifyUnitOfWork.Repository<Appointment>().Single(new FilterSpecification<Appointment>(a => a.Id == appointment.Id), ct);
        updatedAppointment.Should().Match(updateExpectation.Value);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        using IUnitOfWork unitOfWork = _unitOfWorkFactory.NewUnitOfWork();
        await unitOfWork.Repository<Appointment>().Clear();
        await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <inheritdoc />
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;
}