
using Agenda.Objects;

using Candoumbe.DataAccess.Abstractions;

using Microsoft.EntityFrameworkCore;

using NodaTime;

namespace Agenda.DataStores;
/// <summary>
/// Interacts with the underlying repostories.
/// </summary>
public class AgendaDataStore : DataStore<AgendaDataStore>
{
    /// <summary>
    /// Collection of <see cref="Attendee"/>s
    /// </summary>
    public DbSet<Attendee> Participants { get; set; }

    /// <summary>
    /// Collection of <see cref="Appointment"/>
    /// </summary>
    public DbSet<Appointment> Appointments { get; set; }

    /// <summary>
    /// Builds a new <see cref="AgendaDataStore"/> instance.
    /// </summary>
    /// <param name="options">options of the MeasuresContext</param>
    public AgendaDataStore(DbContextOptions<AgendaDataStore> options, IClock clock) : base(options, clock)
    {
    }

    /// <summary>
    /// <see cref="DbContext.OnModelCreating(ModelBuilder)"/>
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppointmentEntityTypeConfiguration).Assembly);
    }
}