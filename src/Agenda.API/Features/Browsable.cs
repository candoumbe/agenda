using System.Collections.Generic;
using Candoumbe.Forms;

namespace Agenda.API.Features;

/// <summary>
/// Wraps a resource and its <see cref="Links"/>.
/// </summary>
/// <typeparam name="TResource">Type of the resource</typeparam>
public class Browsable<TResource> : IFormattable
{
    /// <summary>
    /// The resource being rendered
    /// </summary>
    public TResource Resource { get; set; }

    /// <summary>
    /// Links to resources related to <see cref="Resource"/>.
    /// </summary>
    public IEnumerable<Link> Links { get; set; }

    /// <inheritdoc />
    public string ToString(string format, IFormatProvider formatProvider)
    {
        FormattableString formattable = $"{nameof(Resource)}: {Resource}, {nameof(Links)}: {Links}";
        return formattable.ToString(formatProvider);
    }

    /// <inheritdoc />
    public override string ToString() => $"{nameof(Resource)}: {Resource}, {nameof(Links)}: {Links}";
}