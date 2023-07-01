﻿namespace Agenda.DataStores;

using Agenda.Objects;

using Fluxera.StronglyTypedId.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

///<inheritdoc/>
public class AttendeeEntityTypeConfiguration : IEntityTypeConfiguration<Attendee>
{
    ///<inheritdoc/>
    public void Configure(EntityTypeBuilder<Attendee> builder)
    {

        builder.UseStronglyTypedId();
        builder.Property(x => x.Name)
            .HasMaxLength(AgendaDataStore.NormalTextLength)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(AgendaDataStore.NormalTextLength)
            .IsRequired()
            .HasDefaultValue(string.Empty);


    }
}

