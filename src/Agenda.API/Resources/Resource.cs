namespace Agenda.API.Resources;

/// <summary>
/// Represents a resource
/// </summary>
/// <typeparam name="TId">Type of the identifier of the resource</typeparam>
public class Resource<TId> where TId : IComparable<TId>
{
    /// <summary>
    /// Identifier of the resource
    /// </summary>
    public TId Id { get; init; }
}