using System;

namespace Agenda.API.Features;

/// <summary>
/// Represents a resource
/// </summary>
/// <typeparam name="TId">Type of the identifier of the resource</typeparam>
public record Resource<TId> where TId : IComparable<TId>
{
    /// <summary>
    /// Identifier of the resource
    /// </summary>
    public TId Id { get; init; }
}