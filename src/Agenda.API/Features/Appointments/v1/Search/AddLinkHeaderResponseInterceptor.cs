using System.Diagnostics.CodeAnalysis;
using Candoumbe.Forms;
using FastEndpoints;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Primitives;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Agenda.API.Features.Appointments.v1.Search;

/// <summary>
/// Post processor for handling responses from <see cref="SearchAppointmentsEndpoint"/>.
/// </summary>
public partial class AddLinkHeaderResponseInterceptor : IResponseInterceptor
{
    private readonly ILogger<AddLinkHeaderResponseInterceptor> _logger;

    /// <summary>
    /// Builds a new <see cref="AddLinkHeaderResponseInterceptor"/> instance.
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> is <see langword="null"/>.</exception>
    public AddLinkHeaderResponseInterceptor(ILogger<AddLinkHeaderResponseInterceptor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public Task InterceptResponseAsync(object response,
                                       int statusCode,
                                       HttpContext ctx,
                                       IReadOnlyCollection<ValidationFailure> failures,
                                       CancellationToken ct)
    {
        if (response is not Ok<PageOf<Browsable<AppointmentInfo>>> okResponse || statusCode != Status200OK)
        {
            return Task.CompletedTask;
        }

        PageLinks pageLinks = okResponse.Value.Links;
        Link first = pageLinks.First;
        Link last = pageLinks.Last;
        Link next = pageLinks.Next;
        Link previous = pageLinks.Previous;

        Link[] links = [first, last, next, previous];

        List<string> linkToRender = [];
        foreach (Link link in links.Where(link => link is not null))
        {
            linkToRender.AddRange(link.Relations.Select(relation => $"""
                                                                        <{link.Href}>; rel="{relation}"
                                                                        """));
        }

        LogLinkHeader(string.Join(", ", linkToRender));
        ctx.Response.Headers.Link = new StringValues([.. linkToRender]);

        return Task.CompletedTask;

    }

    [ExcludeFromCodeCoverage]
    [LoggerMessage(LogLevel.Trace, "Link header: {LinkHeader}")]
    private partial void LogLinkHeader(string linkHeader);
}