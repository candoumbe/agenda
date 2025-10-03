using Candoumbe.Forms;

namespace Agenda.API.Features;

/// <summary>
/// Navigation links through pages of result
/// </summary>
/// <param name="First">Link to the first page</param>
/// <param name="Previous">Link to the previous page</param>
/// <param name="Next">Link to the next page of result</param>
/// <param name="Last">Link to the last page of result</param>
public record PageLinks(Link First, Link Last, Link Previous = null, Link Next = null);