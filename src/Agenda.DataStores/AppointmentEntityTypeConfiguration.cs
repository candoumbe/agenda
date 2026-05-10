using Agenda.Ids;
using Agenda.Objects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Agenda.DataStores;
internal class AppointmentEntityTypeConfiguration : IEntityTypeConfiguration<Appointment>
{
    ///<inheritdoc/>
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasConversion<AppointmentId.EfCoreValueConverter>();

        builder.Property(x => x.Location)
            .HasMaxLength(AgendaDataStore.NormalTextLength)
            .HasColumnType("citext");

        builder.Property(x => x.Subject)
            .HasMaxLength(AgendaDataStore.NormalTextLength)
            .IsRequired()
            .HasColumnType("citext");

        builder.Property(x => x.StartDate)
              .IsRequired();

        builder.Property(x => x.EndDate)
              .IsRequired();

        builder.HasMany(x => x.Attendees)
              .WithMany(x => x.Appointments)
              .UsingEntity(j => j.ToTable("AppointmentAttendee"));
    }
}
