using Candoumbe.Forms;

namespace Agenda.API.Resources;

/// <summary>
/// Wraps a resource and its <see cref="Links"/>.
/// </summary>
/// <typeparam name="TResource">Type of the resource</typeparam>
public class Browsable<TResource>
{
    /// <summary>
    /// The resource being rendered
    /// </summary>
    public TResource Resource { get; set; }

    /// <summary>
    /// Links to resources related to <see cref="Resource"/>.
    /// </summary>
    public IEnumerable<Link> Links { get; set; }
}
