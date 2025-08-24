
using Agenda.Objects;

using Fluxera.StronglyTypedId.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Agenda.DataStores;
public class AppointmentEntityTypeConfiguration : IEntityTypeConfiguration<Appointment>
{
    ///<inheritdoc/>
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.UseStronglyTypedId();
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Location)
            .HasMaxLength(AgendaDataStore.NormalTextLength);

        builder.Property(x => x.Subject)
            .HasMaxLength(AgendaDataStore.NormalTextLength)
            .IsRequired();

        builder.Property(x => x.StartDate)
              .IsRequired();

        builder.Property(x => x.EndDate)
              .IsRequired();

        builder.HasMany(x => x.Attendees)
              .WithMany(x => x.Appointments)
              .UsingEntity(j => j.ToTable("AppointmentAttendee"));


    }
}

