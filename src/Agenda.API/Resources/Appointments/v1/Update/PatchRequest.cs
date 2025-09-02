using System.Collections.Generic;
using SystemTextJsonPatch.Operations;


namespace Agenda.API.Resources.Appointments.v1.Update;

/// <summary>
/// Request for a PATCH operation on a <typeparamref name="TTarget"/>
/// </summary>
/// <typeparam name="TId"></typeparam>
/// <typeparam name="TTarget"></typeparam>
public record PatchRequest<TId, TTarget> where TTarget : class
{
    /// <summary>
    /// Identifier of the resource to be updated.
    /// </summary>
    public required TId Id { get; init; }

    /// <summary>
    /// List of operations to be performed on the resource.
    /// </summary>
    public required List<Operation<TTarget>> Operations { get; init; }

}