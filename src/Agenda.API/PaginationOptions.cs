using Candoumbe.Types.Numerics;

namespace Agenda.API;

/// <summary>
/// Pagination options
/// </summary>
public record struct PaginationOptions
{
    /// <summary>
    /// Number of items to return when requiring a page of result no hint was provided by the client.
    /// </summary>
    public PositiveInteger DefaultPageSize { get; set; }
    /// <summary>
    /// Number of items the API can return in a single call at most
    /// </summary>
    public PositiveInteger MaxPageSize { get; set; }
}