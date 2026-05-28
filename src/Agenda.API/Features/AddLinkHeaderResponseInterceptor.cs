using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Candoumbe.Forms;
using FastEndpoints;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Primitives;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Agenda.API.Features;

/// <summary>
/// Adds navigational and pagination headers to successful GET, HEAD, and POST responses.
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
        LogInterceptingResponseWithStatusCode(statusCode, ctx.Request.Method,ctx.Request.Path);

        bool isSupportedMethod = HttpMethods.IsGet(ctx.Request.Method)
                                 || HttpMethods.IsHead(ctx.Request.Method)
                                 || HttpMethods.IsPost(ctx.Request.Method);

        if (isSupportedMethod)
        {
            bool hasSuccessfulStatusCode = statusCode == Status200OK
                                           || statusCode == Status201Created
                                           || statusCode == Status204NoContent;

            if (hasSuccessfulStatusCode && TryGetOkValue(response, out object value))
            {
                bool hasPageInformation = TryGetPageInformation(value, out long total, out long count, out List<Link> pageLinks);
                if (hasPageInformation)
                {
                    SetLinkHeader(ctx, pageLinks);
                    ctx.Response.Headers["total"] = total.ToString(CultureInfo.InvariantCulture);
                    ctx.Response.Headers["count"] = count.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    bool hasBrowsableLinks = TryGetBrowsableLinks(value, out IEnumerable<Link> browsableLinks);
                    if (hasBrowsableLinks)
                    {
                        SetLinkHeader(ctx, browsableLinks);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    private static bool TryGetOkValue(object response, out object value)
    {
        value = null;
        ArgumentNullException.ThrowIfNull(response);

        Type responseType = response.GetType();
        bool isOkResponseType = responseType.IsGenericType
                                && responseType.GetGenericTypeDefinition() == typeof(Ok<>);

        bool hasValue = false;
        if (isOkResponseType)
        {
            PropertyInfo valueProperty = responseType.GetProperty(nameof(Ok<object>.Value));
            if (valueProperty is not null)
            {
                object extractedValue = valueProperty.GetValue(response);
                if (extractedValue is not null)
                {
                    value = extractedValue;
                    hasValue = true;
                }
            }
        }

        return hasValue;
    }

    private static bool TryGetPageInformation(object value, out long total, out long count, out List<Link> links)
    {
        total = 0;
        count = 0;
        links = [];

        Type valueType = value.GetType();
        if (!valueType.IsGenericType || valueType.GetGenericTypeDefinition() != typeof(PageOf<>))
        {
            return false;
        }

        PropertyInfo totalCountProperty = valueType.GetProperty(nameof(PageOf<Browsable<object>>.TotalCount));
        PropertyInfo countProperty = valueType.GetProperty(nameof(PageOf<Browsable<object>>.Count));
        PropertyInfo linksProperty = valueType.GetProperty(nameof(PageOf<Browsable<object>>.Links));

        if (totalCountProperty is null || countProperty is null || linksProperty is null)
        {
            return false;
        }

        object totalCountValue = totalCountProperty.GetValue(value);
        object countValue = countProperty.GetValue(value);
        if (totalCountValue is null || countValue is null)
        {
            return false;
        }

        total = Convert.ToInt64(totalCountValue, CultureInfo.InvariantCulture);
        count = Convert.ToInt64(countValue, CultureInfo.InvariantCulture);

        object rawLinks = linksProperty.GetValue(value);
        if (rawLinks is not PageLinks pageLinks)
        {
            return false;
        }

        List<Link> extractedLinks =
        [
            pageLinks.First,
            pageLinks.Last,
            pageLinks.Next,
            pageLinks.Previous
        ];

        links = [.. extractedLinks.Where(link => link is not null)];

        return true;
    }

    private static bool TryGetBrowsableLinks(object value, out IEnumerable<Link> links)
    {
        links = [];

        Type valueType = value.GetType();
        bool isBrowsableType = valueType.IsGenericType
                               && valueType.GetGenericTypeDefinition() == typeof(Browsable<>);

        bool hasBrowsableLinks = false;
        if (isBrowsableType)
        {
            PropertyInfo linksProperty = valueType.GetProperty(nameof(Browsable<object>.Links));
            if (linksProperty?.GetValue(value) is IEnumerable<Link> browsableLinks)
            {
                links = browsableLinks.Where(link => link is not null);
                hasBrowsableLinks = true;
            }
        }

        return hasBrowsableLinks;
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

        if (linkToRender.Any())
        {
            LogLinkHeader(string.Join(", ", linkToRender));
            context.Response.Headers.Link = new StringValues([.. linkToRender]);
        }
    }

    [ExcludeFromCodeCoverage]
    [LoggerMessage(LogLevel.Trace, "Link header: {LinkHeader}")]
    private partial void LogLinkHeader(string linkHeader);


    [ExcludeFromCodeCoverage]
    [LoggerMessage(LogLevel.Information, "Intercepting response with status code {StatusCode} for request {Method} {Path}")]
    private partial void LogInterceptingResponseWithStatusCode(int statusCode, string method, string path);
}