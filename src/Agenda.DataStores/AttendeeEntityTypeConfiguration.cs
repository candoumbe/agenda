
using Agenda.Ids;
using Agenda.Objects;


using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Agenda.DataStores;
///<inheritdoc/>
public class AttendeeEntityTypeConfiguration : IEntityTypeConfiguration<Attendee>
{
    ///<inheritdoc/>
    public void Configure(EntityTypeBuilder<Attendee> builder)
    {
        builder.Property(x => x.Id).HasConversion<AttendeeId.EfCoreValueConverter>();

        builder.Property(x => x.Name)
            .HasMaxLength(AgendaDataStore.NormalTextLength)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(AgendaDataStore.NormalTextLength)
            .IsRequired()
            .HasDefaultValue(string.Empty);


    }
}