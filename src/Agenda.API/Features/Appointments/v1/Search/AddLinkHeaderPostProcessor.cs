using System.Diagnostics.CodeAnalysis;
using Candoumbe.Forms;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Primitives;

namespace Agenda.API.Features.Appointments.v1.Search;

/// <summary>
/// Post processor for handling responses from <see cref="SearchAppointmentsEndpoint"/>.
/// </summary>
public partial class AddLinkHeaderPostProcessor : IPostProcessor<SearchAppointmentRequest, Ok<PageOf<Browsable<AppointmentInfo>>>>
{
    private readonly ILogger<AddLinkHeaderPostProcessor> _logger;

    /// <summary>
    /// Builds a new <see cref="AddLinkHeaderPostProcessor"/> instance.
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> is <see langword="null"/>.</exception>
    public AddLinkHeaderPostProcessor(ILogger<AddLinkHeaderPostProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public Task PostProcessAsync(IPostProcessorContext<SearchAppointmentRequest, Ok<PageOf<Browsable<AppointmentInfo>>>> context,
                                 CancellationToken ct)
    {
        PageLinks pageLinks = context.Response.Value.Links;
        Link first = pageLinks.First;
        Link last = pageLinks.Last;
        Link next = pageLinks.Next;
        Link previous = pageLinks.Previous;

        Link[] links = [first, last, next, previous];

        context.HttpContext.Response.OnStarting(() =>
                                                {
                                                    List<string> linkToRender = [];
                                                    foreach (Link link in links.Where(link => link is not null))
                                                    {
                                                        linkToRender.AddRange(link.Relations.Select(relation => $"""
                                                                                                                 <{link.Href}>; rel="{relation}"
                                                                                                                 """));
                                                    }

                                                    LogLinkHeader(string.Join(", ", linkToRender));
                                                    context.HttpContext.Response.Headers.Link = new StringValues([..linkToRender]);

                                                    return Task.CompletedTask;
                                                });
        return Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    [LoggerMessage(LogLevel.Trace, "Link header: {LinkHeader}")]
    private partial void LogLinkHeader(string linkHeader);
}