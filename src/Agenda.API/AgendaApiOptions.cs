namespace Agenda.API;

/// <summary>
/// Gives strongly typed access to API's options.
/// </summary>
public sealed record AgendaApiOptions
{
    /// <summary>
    /// Pagination options
    /// </summary>
    public PaginationOptions PaginationOptions { get; set; }

    /// <summary>
    /// Messaging options
    /// </summary>
    public MessagingOptions MessagingOptions { get; set; }
}