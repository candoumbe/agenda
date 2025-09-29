using System;
using System.Threading;
using System.Threading.Tasks;
using Agenda.API.Features.Appointments.v1.Update;
using Agenda.API.UnitTests.Fixtures;
using Agenda.DataStores;
using Agenda.Ids;
using Agenda.Objects;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.EFStore;
using FakeItEasy;
using FastEndpoints;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using SystemTextJsonPatch.Operations;
using Xunit;

namespace Agenda.API.UnitTests.Features.Appointments.v1.Patch;

public class PatchAppointmentByIdEndpointShould: IClassFixture<PostgresSqlFixture>, IAsyncLifetime
{
    private readonly PatchAppointmentByIdEndpoint _sut;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly ITestOutputHelper _outputHelper;
    private readonly IClock _clock;

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

        _sut = Factory.Create<PatchAppointmentByIdEndpoint>(_unitOfWorkFactory);
    }


    [Fact]
    public void Have_expected_route()
    {
        // Assert
        string[] routes = _sut.Definition.Routes;
        routes.Should()
            .HaveCount(1)
            .And
            .ContainSingle("/appointments/{id}");

        string[] methods = _sut.Definition.Verbs;
        methods.Should().HaveCount(1).And.ContainSingle("PATCH");
    }

    [Fact]
    public async Task Return_NoContent_when_existing_appointment_was_successfully_updated()
    {
        Appointment appointment = new(
            AppointmentId.New(),
            "Initial subject",
            "Location",
            Instant.FromUtc(2024, 1, 1, 12, 0),
            Instant.FromUtc(2024, 1, 1, 13, 0));

        CancellationToken ct = TestContext.Current.CancellationToken;

        using IUnitOfWork unitOfWork = _unitOfWorkFactory.NewUnitOfWork();
        await unitOfWork.Repository<Appointment>().Create(appointment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        PatchRequest<AppointmentId, PatchAppointmentRequest> request = new()
        {
            Id = appointment.Id,
            Operations =
            [
                new Operation<PatchAppointmentRequest>(nameof(OperationType.Replace), $"/{nameof(Appointment.Subject)}",  "New subject"),
                new Operation<PatchAppointmentRequest>(nameof(OperationType.Test), $"/{nameof(Appointment.Id)}",  appointment.Id.ToString())
            ]
        };

        // Act
        Results<NoContent, NotFound, ProblemDetails> response= await _sut.ExecuteAsync(request, CancellationToken.None);

        // Assert
        response.Result.Should().BeOfType<NoContent>();

        using IUnitOfWork verifyUnitOfWork = _unitOfWorkFactory.NewUnitOfWork();
        Appointment updatedAppointment = await verifyUnitOfWork.Repository<Appointment>().Single(new FilterSpecification<Appointment>(a => a.Id == appointment.Id), ct);
        updatedAppointment.Subject.Should().Be("New subject");
    }

    [Fact]
    public async Task Return_NotFound_When_appointment_does_not_exist()
    {
        Instant newStartDate = Instant.FromUtc(2024, 1, 1, 14, 0);
        PatchRequest<AppointmentId, PatchAppointmentRequest> request = new()
        {
            Id = AppointmentId.New(),
            Operations =
            [
                new Operation<PatchAppointmentRequest>(nameof(OperationType.Replace), $"/{nameof(Appointment.StartDate)}",  newStartDate.ToString())
            ]
        };

        // Act
        Results<NoContent, NotFound, ProblemDetails> response = await _sut.ExecuteAsync(request, CancellationToken.None);

        // Assert
        response.Result.Should().BeOfType<NotFound>();
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