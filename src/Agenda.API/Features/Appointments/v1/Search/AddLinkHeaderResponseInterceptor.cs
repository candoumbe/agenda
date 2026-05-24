using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
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
        if (statusCode != Status200OK || !TryGetOkValue(response, out object value))
        {
            return Task.CompletedTask;
        }

        if (TryGetPageInformation(value, out long total, out long totalCount, out long count, out List<Link> pageLinks))
        {
            SetLinkHeader(ctx, pageLinks);
            ctx.Response.Headers["total"] = total.ToString(CultureInfo.InvariantCulture);
            ctx.Response.Headers["totalCount"] = totalCount.ToString(CultureInfo.InvariantCulture);
            ctx.Response.Headers["count"] = count.ToString(CultureInfo.InvariantCulture);
            return Task.CompletedTask;
        }

        if (TryGetBrowsableLinks(value, out IEnumerable<Link> browsableLinks))
        {
            SetLinkHeader(ctx, browsableLinks);
        }

        return Task.CompletedTask;
    }

    private static bool TryGetOkValue(object response, out object value)
    {
        value = null;
        ArgumentNullException.ThrowIfNull(response);

        Type responseType = response.GetType();
        if (!responseType.IsGenericType || responseType.GetGenericTypeDefinition() != typeof(Ok<>))
        {
            return false;
        }

        PropertyInfo valueProperty = responseType.GetProperty(nameof(Ok<object>.Value));
        if (valueProperty is null)
        {
            return false;
        }

        value = valueProperty.GetValue(response);
        return value is not null;
    }

    private static bool TryGetPageInformation(object value, out long total, out long totalCount, out long count, out List<Link> links)
    {
        total = 0;
        totalCount = 0;
        count = 0;
        links = [];

        Type valueType = value.GetType();
        if (!valueType.IsGenericType || valueType.GetGenericTypeDefinition() != typeof(PageOf<>))
        {
            return false;
        }

        PropertyInfo totalProperty = valueType.GetProperty(nameof(PageOf<Browsable<AppointmentInfo>>.Total));
        PropertyInfo totalCountProperty = valueType.GetProperty(nameof(PageOf<Browsable<AppointmentInfo>>.TotalCount));
        PropertyInfo countProperty = valueType.GetProperty(nameof(PageOf<Browsable<AppointmentInfo>>.Count));
        PropertyInfo linksProperty = valueType.GetProperty(nameof(PageOf<Browsable<AppointmentInfo>>.Links));

        if (totalProperty is null || totalCountProperty is null || countProperty is null || linksProperty is null)
        {
            return false;
        }

        object totalValue = totalProperty.GetValue(value);
        object totalCountValue = totalCountProperty.GetValue(value);
        object countValue = countProperty.GetValue(value);
        if (totalValue is null || totalCountValue is null || countValue is null)
        {
            return false;
        }

        total = Convert.ToInt64(totalValue, CultureInfo.InvariantCulture);
        totalCount = Convert.ToInt64(totalCountValue, CultureInfo.InvariantCulture);
        count = Convert.ToInt64(countValue, CultureInfo.InvariantCulture);

        object rawLinks = linksProperty.GetValue(value);
        if (rawLinks is not PageLinks pageLinks)
        {
            return false;
        }

        List<Link> extractedLinks = new()
        {
            pageLinks.First,
            pageLinks.Last,
            pageLinks.Next,
            pageLinks.Previous
        };

        links = extractedLinks.Where(link => link is not null)
                              .ToList();

        return true;
    }

    private static bool TryGetBrowsableLinks(object value, out IEnumerable<Link> links)
    {
        links = [];

        Type valueType = value.GetType();
        if (!valueType.IsGenericType || valueType.GetGenericTypeDefinition() != typeof(Browsable<>))
        {
            return false;
        }

        PropertyInfo linksProperty = valueType.GetProperty(nameof(Browsable<AppointmentInfo>.Links));
        if (linksProperty?.GetValue(value) is not IEnumerable<Link> browsableLinks)
        {
            return false;
        }

        links = browsableLinks.Where(link => link is not null);
        return true;
    }

    private void SetLinkHeader(HttpContext context, IEnumerable<Link> links)
    {
        List<string> linkToRender = [];
        foreach (Link link in links)
        {
            if (link?.Relations is null)
            {
                continue;
            }

            linkToRender.AddRange(link.Relations
                                     .Where(relation => !string.IsNullOrWhiteSpace(relation))
                                     .Select(relation => $"<{link.Href}>; rel=\"{relation}\""));
        }

        if (!linkToRender.Any())
        {
            return;
        }

        LogLinkHeader(string.Join(", ", linkToRender));
        context.Response.Headers.Link = new StringValues([.. linkToRender]);
    }

    [ExcludeFromCodeCoverage]
    [LoggerMessage(LogLevel.Trace, "Link header: {LinkHeader}")]
    private partial void LogLinkHeader(string linkHeader);
}