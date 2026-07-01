using System;
using Agenda.Objects;
using NodaTime;

namespace Agenda.API.Features;

/// <summary>
/// Represents a resource
/// </summary>
/// <typeparam name="TId">Type of the resource's identifier.</typeparam>
public record AuditableResource<TId> : Resource<TId> where TId : IComparable<TId>
{
    /// <summary>
    /// The date and time when the resource was created.
    /// </summary>
    public Instant CreatedAt { get; init; }

    /// <summary>
    /// The username of the user who created the resource.
    /// </summary>
    public Username CreatedBy { get; init; }

    /// <summary>
    /// The date and time when the resource was last updated.
    /// </summary>
    public Instant? UpdatedAt { get; init; }

    /// <summary>
    /// The username of the user who last updated the resource.
    /// </summary>
    public Username UpdatedBy { get; init; }
}